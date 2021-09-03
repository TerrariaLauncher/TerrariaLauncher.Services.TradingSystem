using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Command;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Protos.Services.TradingSystem;
using TerrariaLauncher.Services.TradingSystem.Database.Commands;
using TerrariaLauncher.Services.TradingSystem.Database.Queries;

namespace TerrariaLauncher.Services.TradingSystem.GrpcServices
{
    public class RegisteredGameUserService : TerrariaLauncher.Protos.Services.TradingSystem.RegisteredGameUserService.RegisteredGameUserServiceBase
    {
        IQueryDispatcher _queryDispatcher;
        ICommandDispatcher _commandDispatcher;
        TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Users.UsersClient _gameUserService;

        public RegisteredGameUserService(
            IQueryDispatcher queryDispatcher,
            ICommandDispatcher commandDispatcher,
            TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Users.UsersClient gameUserService)
        {
            this._queryDispatcher = queryDispatcher;
            this._commandDispatcher = commandDispatcher;
            this._gameUserService = gameUserService;
        }

        public override async Task<CheckIfGameUserIsRegisteredResponse> CheckIfGameUserIsRegistered(CheckIfGameUserIsRegisteredRequest request, ServerCallContext context)
        {
            TerrariaLauncher.Protos.Services.GameCoordinator.Hub.GetUserResponse getUserResult;
            switch (request.IdentityCase)
            {
                case CheckIfGameUserIsRegisteredRequest.IdentityOneofCase.GameUserId:
                    getUserResult = await this._gameUserService.GetUserAsync(new Protos.Services.GameCoordinator.Hub.GetUserRequest() { Id = request.GameUserId }, cancellationToken: context.CancellationToken);
                    break;
                case CheckIfGameUserIsRegisteredRequest.IdentityOneofCase.GameUserName:
                    getUserResult = await this._gameUserService.GetUserAsync(new Protos.Services.GameCoordinator.Hub.GetUserRequest() { Name = request.GameUserName }, cancellationToken: context.CancellationToken);
                    break;
                default:
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Instance user identity is not provided."));
            }

            var getRegisteredGameUserResult = await this._queryDispatcher.DispatchAsync<GetRegisteredGameUserQuery, GetRegisteredGameUserResult>(new GetRegisteredGameUserQuery()
            {
                Id = getUserResult.Id,
                ByPassDeleted = true
            }, context.CancellationToken);
            if (getRegisteredGameUserResult is null)
            {
                return new CheckIfGameUserIsRegisteredResponse()
                {
                    IsRegistered = false
                };
            }

            return new CheckIfGameUserIsRegisteredResponse()
            {
                IsRegistered = true,
                UserId = getRegisteredGameUserResult.UserId,
                GameUser = new GameUser()
                {
                    Id = getUserResult.Id,
                    Name = getUserResult.Name,
                    Group = getUserResult.Group
                }
            };
        }

        public override async Task<GetRegisteredGameUserResponse> GetRegisteredGameUser(GetRegisteredGameUserRequest request, ServerCallContext context)
        {
            var getRegisteredGameUserResult = await this._queryDispatcher.DispatchAsync<GetRegisteredGameUserQuery, GetRegisteredGameUserResult>(new GetRegisteredGameUserQuery()
            {
                Id = request.GameUserId
            }, context.CancellationToken);
            if (getRegisteredGameUserResult is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Could not find registered instance user."));
            }

            var getUserResult = await this._gameUserService.GetUserAsync(new Protos.Services.GameCoordinator.Hub.GetUserRequest()
            {
                Id = request.GameUserId
            }, cancellationToken: context.CancellationToken);
            return new GetRegisteredGameUserResponse()
            {
                UserId = getRegisteredGameUserResult.UserId,
                GameUser = new GameUser()
                {
                    Id = getUserResult.Id,
                    Name = getUserResult.Name,
                    Group = getUserResult.Group
                }
            };
        }

        public override async Task<GetRegisteredGameUsersResponse> GetRegisteredGameUsers(GetRegisteredGameUsersRequest request, ServerCallContext context)
        {
            var getRegisteredGameUsersResult = await this._queryDispatcher.DispatchAsync<GetRegisteredGameUsersQuery, GetRegisteredGameUsersResult>(
                new GetRegisteredGameUsersQuery()
                {
                    UserId = request.UserId
                }, context.CancellationToken);

            var response = new GetRegisteredGameUsersResponse()
            {
                UserId = getRegisteredGameUsersResult.UserId
            };
            foreach (var gameUser in getRegisteredGameUsersResult.RegisteredGameUsers)
            {
                var getGameUserResult = await this._gameUserService.GetUserAsync(new Protos.Services.GameCoordinator.Hub.GetUserRequest() { Id = gameUser.GameUserId }, cancellationToken: context.CancellationToken);

                response.GameUsers.Add(new GameUser()
                {
                    Id = getGameUserResult.Id,
                    Name = getGameUserResult.Name,
                    Group = getGameUserResult.Name
                });
            }
            return response;
        }

        public override async Task<RegisterNewGameUserResponse> RegisterNewGameUser(RegisterNewGameUserRequest request, ServerCallContext context)
        {
            var registerResult = await this._gameUserService.RegisterAsync(new Protos.Services.GameCoordinator.Hub.RegisterRequest()
            {
                Name = request.GameUserName,
                Password = request.GameUserPassword
            }, cancellationToken: context.CancellationToken);

            await this._commandDispatcher.DispatchAsync<RegisterGameUserCommand, RegisterGameUserResult>(new RegisterGameUserCommand()
            {
                UserId = request.UserId,
                GameUserId = registerResult.Id
            }, context.CancellationToken);

            return new RegisterNewGameUserResponse()
            {
                UserId = request.UserId,
                GameUser = new GameUser()
                {
                    Id = registerResult.Id,
                    Name = registerResult.Name,
                    Group = registerResult.Group
                }
            };
        }

        public override async Task<RegisterExistingGameUserResponse> RegisterExistingGameUser(RegisterExistingGameUserRequest request, ServerCallContext context)
        {
            var getGameUserResult = await this._gameUserService.GetUserAsync(new Protos.Services.GameCoordinator.Hub.GetUserRequest()
            {
                Name = request.GameUserName
            }, cancellationToken: context.CancellationToken);
            var verifyPasswordResult = await this._gameUserService.VerifyPasswordAsync(new Protos.Services.GameCoordinator.Hub.VerifyPasswordRequest()
            {
                Name = request.GameUserName,
                Password = request.GameUserPassword
            }, cancellationToken: context.CancellationToken);

            if (!verifyPasswordResult.IsValid)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "TShock user password is not valid."));
            }

            await this._commandDispatcher.DispatchAsync<RegisterGameUserCommand, RegisterGameUserResult>(new RegisterGameUserCommand()
            {
                UserId = request.UserId,
                GameUserId = getGameUserResult.Id
            }, context.CancellationToken);

            return new RegisterExistingGameUserResponse()
            {
                UserId = request.UserId,
                GameUser = new GameUser()
                {
                    Id = getGameUserResult.Id,
                    Name = getGameUserResult.Name,
                    Group = getGameUserResult.Group
                }
            };
        }

        public override async Task<DeregisterGameUserResponse> DeregisterGameUser(DeregisterGameUserRequest request, ServerCallContext context)
        {
            await this._commandDispatcher.DispatchAsync<DeregisterGameUserCommand, DeregisterGameUserResult>(new DeregisterGameUserCommand()
            {
                UserId = request.UserId,
                GameUserId = request.GameUserId
            }, context.CancellationToken);

            return new DeregisterGameUserResponse() { };
        }
    }
}
