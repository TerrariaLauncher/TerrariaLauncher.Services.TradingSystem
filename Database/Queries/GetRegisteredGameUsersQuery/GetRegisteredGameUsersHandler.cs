using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Commons.Database.Extensions;

namespace TerrariaLauncher.Services.TradingSystem.Database.Queries
{
    public class GetRegisteredGameUsersHandler : QueryHandler<GetRegisteredGameUsersQuery, GetRegisteredGameUsersResult>
    {
        IUnitOfWorkFactory unitOfWorkFactory;
        public GetRegisteredGameUsersHandler(
            IUnitOfWorkFactory unitOfWorkFactory,
            ILogger<GetRegisteredGameUsersHandler> logger) : base(logger)
        {
            this.unitOfWorkFactory = unitOfWorkFactory;
        }

        protected override async Task<GetRegisteredGameUsersResult> ImplementationAsync(GetRegisteredGameUsersQuery query, CancellationToken cancellationToken = default)
        {
            var uow = await this.unitOfWorkFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
            await using (uow.ConfigureAwait(false))
            {
                var command = uow.Connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.Transaction = uow.Transaction;
                    command.CommandText = "SELECT * FROM registeredGameUsers WHERE userId = @userId";
                    command.AddParameterWithValue("userId", query.UserId);
                    if (!query.ByPassDeleted)
                    {
                        command.CommandText += " AND isDeleted = @isDeleted";
                        command.AddParameterWithValue("isDeleted", 0);
                    }

                    var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
                    await using (reader.ConfigureAwait(false))
                    {
                        var result = new GetRegisteredGameUsersResult()
                        {
                            RegisteredGameUsers = new List<GetRegisteredGameUsersResult.RegisteredGameUser>()
                        };
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            result.RegisteredGameUsers.Add(new GetRegisteredGameUsersResult.RegisteredGameUser()
                            {
                                GameUserId = await reader.GetFieldValueAsync<int>("gameUserId", cancellationToken),
                                Deleted = await reader.GetFieldValueAsync<bool>("isDeleted", cancellationToken)
                            });
                        }
                        result.UserId = query.UserId;
                        return result;
                    }
                }
            }
        }
    }
}
