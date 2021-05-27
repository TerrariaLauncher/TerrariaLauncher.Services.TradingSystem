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
                var items = unitOfWork.RunQueryHandler(new GetInstanceUsersQueryAsyncHandler(query), cancellationToken);
                await foreach (var item in items.ConfigureAwait(false))
                {
                    yield return item;
                }
            }
        }

        public async Task<RegisteredInstanceUser> Handle(GetRegisteredInstanceUserQueryAsync query, CancellationToken cancellationToken = default)
        {
            var unitOfWork = await this.unitOfWorkFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
            await using (unitOfWork.ConfigureAwait(false))
            {
                return await unitOfWork.RunQueryHandler(new GetInstanceUserAsyncHandler(query), cancellationToken);
            }
        }

        #region HandlerImplementations
        private class GetInstanceUserAsyncHandler : IQuerySingleHandlerAsync<RegisteredInstanceUser>
        {
            GetRegisteredInstanceUserQueryAsync query;
            public GetInstanceUserAsyncHandler(GetRegisteredInstanceUserQueryAsync query)
            {
                this.query = query;
            }

            public async Task<RegisteredInstanceUser> HandleAsync(DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            {
                var command = connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.Transaction = transaction;
                    command.CommandText = "SELECT * FROM registeredInstanceUsers WHERE instanceId = @instanceId AND instanceUserId = @instanceUserId AND isDeleted = @isDeleted";
                    command.AddParameterWithValue("instanceId", this.query.InstanceId);
                    command.AddParameterWithValue("instanceUserId", this.query.InstanceUserId);
                    command.AddParameterWithValue("isDeleted", 0);

                    var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
                    await using (reader.ConfigureAwait(false))
                    {
                        if (!await reader.ReadAsync(cancellationToken))
                        {
                            return null;
                        }

                        return new RegisteredInstanceUser()
                        {
                            UserId = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("userId"), cancellationToken).ConfigureAwait(false),
                            InstanceId = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("instanceId"), cancellationToken).ConfigureAwait(false),
                            InstanceUserId = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("instanceUserId"), cancellationToken).ConfigureAwait(false)
                        };
                    }
                }
            }
        }

        private class GetInstanceUsersQueryAsyncHandler : IQueryHandlerAsyncEnumerable<RegisteredInstanceUser>
        {
            private GetRegisteredInstanceUsersQueryAsync query;

            public GetInstanceUsersQueryAsyncHandler(GetRegisteredInstanceUsersQueryAsync query)
            {
                this.query = query;
            }

            public async IAsyncEnumerable<RegisteredInstanceUser> HandleAsync(DbConnection connection, DbTransaction transaction, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                var command = connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.Transaction = transaction;
                    command.CommandText = "SELECT * FROM registeredInstanceUsers WHERE userId = @userId AND instanceId = @instanceId AND isDeleted = @isDeleted";
                    command.AddParameterWithValue("userId", this.query.UserId);
                    command.AddParameterWithValue("instanceId", this.query.InstanceId);
                    command.AddParameterWithValue("isDeleted", 0);

                    var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    await using (reader.ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            var character = new RegisteredInstanceUser()
                            {
                                UserId = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("userId"), cancellationToken).ConfigureAwait(false),
                                InstanceId = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("instanceId"), cancellationToken).ConfigureAwait(false),
                                InstanceUserId = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("instanceUserId"), cancellationToken).ConfigureAwait(false)
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
