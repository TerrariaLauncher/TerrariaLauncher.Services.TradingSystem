using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database;
using TerrariaLauncher.Commons.Database.CQS.Command;
using TerrariaLauncher.Commons.Database.Extensions;


namespace TerrariaLauncher.Services.TradingSystem.Database.Commands
{
    public class RegisterGameUserHandler: CommandHandler<RegisterGameUserCommand, RegisterGameUserResult>
    {
        private IUnitOfWorkFactory unitOfWorkFactory;

        public RegisterGameUserHandler(
            IUnitOfWorkFactory unitOfWorkFactory,
            ILogger<RegisterGameUserHandler> logger) : base(logger)
        {
            this.unitOfWorkFactory = unitOfWorkFactory;
        }

        protected override async Task<RegisterGameUserResult> ImplementationAsync(RegisterGameUserCommand command, CancellationToken cancellationToken = default)
        {
            var uow = await this.unitOfWorkFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
            await using (uow.ConfigureAwait(false))
            {
                var sqlCommand = uow.Connection.CreateCommand();
                await using (sqlCommand.ConfigureAwait(false))
                {
                    sqlCommand.Transaction = uow.Transaction;
                    sqlCommand.CommandText = "INSERT INTO registeredGameUsers (userId, gameUserId) VALUES (@userId, @gameUserId)";
                    sqlCommand.AddParameterWithValue("userId", command.UserId);
                    sqlCommand.AddParameterWithValue("gameUserId", command.GameUserId);
                    await sqlCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            return new RegisterGameUserResult();
        }
    }
}
