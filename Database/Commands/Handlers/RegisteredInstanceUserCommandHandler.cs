using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database;
using TerrariaLauncher.Commons.Database.Extensions;


namespace TerrariaLauncher.Services.TradingSystem.Database.Commands.Handlers
{
    public class RegisteredInstanceUserCommandHandler
    {
        private IUnitOfWorkFactory unitOfWorkFactory;

        public RegisteredInstanceUserCommandHandler(IUnitOfWorkFactory unitOfWorkFactory)
        {
            this.unitOfWorkFactory = unitOfWorkFactory;
        }

        public async Task Handle(AttachCharacterCommandAsync command, CancellationToken cancellationToken = default)
        {
            var unitOfWork = await this.unitOfWorkFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
            await using (unitOfWork.ConfigureAwait(false))
            {
                await unitOfWork.RunCommandHandler(new AttachCharacterCommandHandler(command), cancellationToken);
            }
        }

        public async Task<bool> Handle(DettachCharacterCommandAsync command, CancellationToken cancellationToken = default)
        {
            var unitOfWork = await this.unitOfWorkFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
            await using (unitOfWork.ConfigureAwait(false))
            {
                return await unitOfWork.RunCommandHandler(new DettachCharacterCommandHandler(command), cancellationToken);
            }
        }

        private class AttachCharacterCommandHandler : ICommandHandlerAsync
        {
            public bool RequiredTransaction => false;

            private AttachCharacterCommandAsync command;

            public AttachCharacterCommandHandler(AttachCharacterCommandAsync command)
            {
                this.command = command;
            }

            public async Task Handle(DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            {
                var command = connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.Transaction = transaction;
                    command.CommandText = "INSERT INTO characters (instanceId, instanceUserId, userId) VALUES (@instanceId, @instanceUserId, @userId)";
                    command.AddParameterWithValue("instanceId", this.command.InstanceId);
                    command.AddParameterWithValue("instanceUserId", this.command.InstanceUserId);
                    command.AddParameterWithValue("userId", this.command.UserId);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
            }
        }

        private class DettachCharacterCommandHandler : ICommandHandlerAsync<bool>
        {
            public bool RequiredTransaction => false;

            private DettachCharacterCommandAsync command;

            public DettachCharacterCommandHandler(DettachCharacterCommandAsync command)
            {
                this.command = command;
            }

            public async Task<bool> Handle(DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            {
                var command = connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.Transaction = transaction;
                    command.CommandText = "DELETE FROM characters WHERE instanceId = @instanceId AND instanceUserId = @instanceUserId AND userId = @userId";
                    command.AddParameterWithValue("instanceId", this.command.InstanceId);
                    command.AddParameterWithValue("instanceUserId", this.command.InstanceUserId);

                    var numChanges = await command.ExecuteNonQueryAsync(cancellationToken);
                    return numChanges > 0;
                }
            }
        }
    }
}
