using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Protos.Services.TradingSystem;
using TerrariaLauncher.Services.TradingSystem.Database.Commands.Handlers;
using TerrariaLauncher.Services.TradingSystem.Database.Queries.Handlers;

namespace TerrariaLauncher.Services.TradingSystem.GrpcServices
{
    public class RegisteredInstanceUserService : TerrariaLauncher.Protos.Services.TradingSystem.RegisteredInstanceUserService.RegisteredInstanceUserServiceBase
    {
        private RegisteredInstanceUserQueryHandler registeredInstanceUserQueryHandler;
        private RegisteredInstanceUserCommandHandler registeredInstanceUserCommandHandler;
        private TerrariaLauncher.Protos.Services.InstanceGateway.InstanceUserManagement.InstanceUserManagementClient tShockUserManagementClient;

        public RegisteredInstanceUserService(
            RegisteredInstanceUserQueryHandler registeredInstanceUserQueryHandler,
            RegisteredInstanceUserCommandHandler registeredInstanceUserCommandHandler,
            TerrariaLauncher.Protos.Services.InstanceGateway.InstanceUserManagement.InstanceUserManagementClient tShockUserManagementClient)
        {
            this.registeredInstanceUserQueryHandler = registeredInstanceUserQueryHandler;
            this.registeredInstanceUserCommandHandler = registeredInstanceUserCommandHandler;
            this.tShockUserManagementClient = tShockUserManagementClient;
        }

        public override async Task<GetRegisteredInstanceUsersResponse> GetRegisteredInstanceUsers(GetRegisteredInstanceUsersRequest request, ServerCallContext context)
        {
            var response = new GetRegisteredInstanceUsersResponse();
            var query = new Database.Queries.GetRegisteredInstanceUsersQueryAsync()
            {
                InstanceId = request.InstanceId,
                UserId = request.UserId
            };
            var characters = registeredInstanceUserQueryHandler.Handle(query, context.CancellationToken);

            await foreach (var character in characters.ConfigureAwait(false))
            {
                using (var getUserCall = tShockUserManagementClient.GetUserAsync(new Protos.Services.InstanceGateway.GetUserRequest()
                {
                    InstanceId = character.InstanceId,
                    Payload = new Protos.InstancePlugins.InstanceManagement.GetUserRequest()
                    {
                        Id = character.InstanceUserId
                    }
                }, cancellationToken: context.CancellationToken))
                {
                    var getUserResponse = await getUserCall.ResponseAsync.ConfigureAwait(false);
                    response.RegisteredInstanceUsers.Add(new RegisteredInstanceUser()
                    {
                        InstanceId = character.InstanceId,
                        InstanceUserId = character.InstanceUserId,
                        InstanceUserName = getUserResponse.Name,
                        UserId = character.UserId
                    });
                }
            }
            return response;
        }

        public override async Task<CreateInstanceUserResponse> CreateInstanceUser(CreateInstanceUserRequest request, ServerCallContext context)
        {
            var createUserResponse = await this.tShockUserManagementClient.CreateUserAsync(new Protos.Services.InstanceGateway.CreateUserRequest()
            {
                InstanceId = request.Instance,
                Payload = new Protos.InstancePlugins.InstanceManagement.CreateUserRequest()
                {
                    Name = request.InstanceUserName,
                    Password = request.InstanceUserPassword
                }
            }, cancellationToken: context.CancellationToken);

            return new CreateInstanceUserResponse()
            {
                InstanceUserId = createUserResponse.Id,
                InstanceUserName = createUserResponse.Name,
                InstanceUserGroup = createUserResponse.Group
            };
        }

        public override async Task<RegisterInstanceUserResponse> RegisterInstanceUser(RegisterInstanceUserRequest request, ServerCallContext context)
        {
            var getTShockUserResponse = await this.tShockUserManagementClient.GetUserAsync(new Protos.Services.InstanceGateway.GetUserRequest()
            {
                InstanceId = request.InstanceId,
                Payload = new Protos.InstancePlugins.InstanceManagement.GetUserRequest()
                {
                    Name = request.InstanceUserName
                }
            }, cancellationToken: context.CancellationToken);
            var verifyPasswordResponse = await this.tShockUserManagementClient.VerifyUserPasswordAsync(new Protos.Services.InstanceGateway.VerifyUserPasswordRequest()
            {
                InstanceId = request.InstanceId,
                Payload = new Protos.InstancePlugins.InstanceManagement.VerifyUserPasswordRequest()
                {
                    Name = request.InstanceUserName,
                    Password = request.InstanceUserPassword
                }
            }, cancellationToken: context.CancellationToken);

            if (!verifyPasswordResponse.IsPasswordValid)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "TShock user password is not valid."));
            }

            await this.registeredInstanceUserCommandHandler.Handle(new Database.Commands.AttachCharacterCommandAsync()
            {
                InstanceId = request.InstanceId,
                InstanceUserId = getTShockUserResponse.Id,
                UserId = request.UserId
            }, context.CancellationToken);

            return new RegisterInstanceUserResponse()
            {
                
            };
        }

        public override async Task<DeregisterInstanceUserResponse> DeregisterInstanceUser(DeregisterInstanceUserRequest request, ServerCallContext context)
        {
            var isDeleted = await this.registeredInstanceUserCommandHandler.Handle(new Database.Commands.DettachCharacterCommandAsync()
            {
                InstanceId = request.InstanceId,
                InstanceUserId = request.InstanceUserId,
                UserId = request.UserId
            });
            
            if (!isDeleted)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Could not find the character in the user characters."));
            }

            return new DeregisterInstanceUserResponse()
            {
                
            };
        }
    }
}
