using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database;
using TerrariaLauncher.Commons.Database.CQS.Command;
using TerrariaLauncher.Commons.Database.Extensions;

namespace TerrariaLauncher.Services.TradingSystem.Database.Commands
{
    public class DeregisterGameUserHandler : CommandHandler<DeregisterGameUserCommand, DeregisterGameUserResult>
    {
        IUnitOfWorkFactory unitOfWorkFactory;
        public DeregisterGameUserHandler(
            IUnitOfWorkFactory unitOfWorkFactory,
            ILogger<DeregisterGameUserHandler> logger) : base(logger)
        {
            this.unitOfWorkFactory = unitOfWorkFactory;
        }

        protected override async Task<DeregisterGameUserResult> ImplementationAsync(DeregisterGameUserCommand command, CancellationToken cancellationToken = default)
        {
            var uow = await this.unitOfWorkFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
            await using (uow.ConfigureAwait(false))
            {
                var sqlCommand = uow.Connection.CreateCommand();
                await using (sqlCommand.ConfigureAwait(false))
                {
                    sqlCommand.Transaction = uow.Transaction;
                    sqlCommand.CommandText = "UPDATE registeredGameUsers SET isDeleted = @isDeleted WHERE userId = @userId AND gameUserId = @gameUserId";
                    sqlCommand.AddParameterWithValue("userId", command.UserId);
                    sqlCommand.AddParameterWithValue("gameUserId", command.GameUserId);
                    sqlCommand.AddParameterWithValue("isDeleted", 1);

                    var numChanges = await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            return new DeregisterGameUserResult() { };
        }
    }
}
