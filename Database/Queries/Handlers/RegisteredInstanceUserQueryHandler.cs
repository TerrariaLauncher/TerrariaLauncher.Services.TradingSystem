using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database;
using TerrariaLauncher.Commons.Database.Extensions;
using TerrariaLauncher.Services.TradingSystem.Database.QueryDTOs;

namespace TerrariaLauncher.Services.TradingSystem.Database.Queries.Handlers
{
    public class RegisteredInstanceUserQueryHandler
    {
        private IUnitOfWorkFactory unitOfWorkFactory;

        public RegisteredInstanceUserQueryHandler(IUnitOfWorkFactory unitOfWorkFactory)
        {
            this.unitOfWorkFactory = unitOfWorkFactory;
        }

        public async IAsyncEnumerable<RegisteredInstanceUser> Handle(GetRegisteredInstanceUsersQueryAsync query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var unitOfWork = await this.unitOfWorkFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
            await using (unitOfWork.ConfigureAwait(false))
            {
                var items = unitOfWork.RunQueryHandler(new GetCharactersQueryAsyncHandler(query), cancellationToken);
                await foreach (var item in items.ConfigureAwait(false))
                {
                    yield return item;
                }
            }
        }

        #region HandlerImplementations
        private class GetCharactersQueryAsyncHandler : IQueryHandlerAsyncEnumerable<RegisteredInstanceUser>
        {
            private GetRegisteredInstanceUsersQueryAsync query;

            public GetCharactersQueryAsyncHandler(GetRegisteredInstanceUsersQueryAsync query)
            {
                this.query = query;
            }

            public async IAsyncEnumerable<RegisteredInstanceUser> HandleAsync(DbConnection connection, DbTransaction transaction, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                var command = connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.Transaction = transaction;
                    command.CommandText = "SELECT * FROM registeredInstanceUsers WHERE userId = @userId AND instanceId = @instanceId";
                    command.AddParameterWithValue("userId", this.query.UserId);
                    command.AddParameterWithValue("instanceId", this.query.InstanceId);

                    var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    await using (reader.ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            var character = new RegisteredInstanceUser()
                            {
                                InstanceId = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("instanceId"), cancellationToken).ConfigureAwait(false),
                                InstanceUserId = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("instanceUserId"), cancellationToken).ConfigureAwait(false),
                                UserId = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("userId"), cancellationToken).ConfigureAwait(false)
                            };
                            yield return character;
                        }
                    }
                }
            }
        }
        #endregion
    }
}
