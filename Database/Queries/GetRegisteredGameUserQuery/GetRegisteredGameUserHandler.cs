using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database;
using TerrariaLauncher.Commons.Database.CQS.Command;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Commons.Database.Extensions;

namespace TerrariaLauncher.Services.TradingSystem.Database.Queries
{
    public class GetRegisteredGameUserHandler : QueryHandler<GetRegisteredGameUserQuery, GetRegisteredGameUserResult>
    {
        private IUnitOfWorkFactory unitOfWorkFactory;

        public GetRegisteredGameUserHandler(
            IUnitOfWorkFactory unitOfWorkFactory,
            ILogger<GetRegisteredGameUserHandler> logger) : base(logger)
        {
            this.unitOfWorkFactory = unitOfWorkFactory;
        }

        protected override async Task<GetRegisteredGameUserResult> ImplementationAsync(GetRegisteredGameUserQuery query, CancellationToken cancellationToken = default)
        {
            var uow = await this.unitOfWorkFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
            await using (uow.ConfigureAwait(false))
            {
                var command = uow.Connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.Transaction = uow.Transaction;
                    command.CommandText = "SELECT * FROM registeredGameUsers WHERE gameUserId = @gameUserId";
                    command.AddParameterWithValue("gameUserId", query.Id);
                    if (!query.ByPassDeleted)
                    {
                        command.CommandText += " AND isDeleted = @isDeleted";
                        command.AddParameterWithValue("isDeleted", 0);
                    }

                    var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
                    await using (reader.ConfigureAwait(false))
                    {
                        if (!await reader.ReadAsync(cancellationToken))
                        {
                            return null;
                        }

                        return new GetRegisteredGameUserResult()
                        {
                            UserId = await reader.GetFieldValueAsync<int>("userId", cancellationToken),
                            GameUserId = await reader.GetFieldValueAsync<int>("gameUserId", cancellationToken),
                            IsDeleted = await reader.GetFieldValueAsync<bool>("isDeleted", cancellationToken)
                        };
                    }
                }
            }
        }
    }
}
