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
        private TerrariaLauncher.Protos.Services.InstanceGateway.InstanceUserManagement.InstanceUserManagementClient instanceUserManagementClient;

        public RegisteredInstanceUserService(
            RegisteredInstanceUserQueryHandler registeredInstanceUserQueryHandler,
            RegisteredInstanceUserCommandHandler registeredInstanceUserCommandHandler,
            TerrariaLauncher.Protos.Services.InstanceGateway.InstanceUserManagement.InstanceUserManagementClient tShockUserManagementClient)
        {
            this.registeredInstanceUserQueryHandler = registeredInstanceUserQueryHandler;
            this.registeredInstanceUserCommandHandler = registeredInstanceUserCommandHandler;
            this.instanceUserManagementClient = tShockUserManagementClient;
        }

        public override async Task<CheckIfInstanceUserIsRegisteredResponse> CheckIfInstanceUserIsRegistered(CheckIfInstanceUserIsRegisteredRequest request, ServerCallContext context)
        {
            var getInstanceUserRequest = new Protos.Services.InstanceGateway.GetUserRequest()
            {
                InstanceId = request.InstanceId,
                Payload = new Protos.InstancePlugins.InstanceManagement.GetUserRequest()
            };
            switch (request.IdentityCase)
            {
                case CheckIfInstanceUserIsRegisteredRequest.IdentityOneofCase.InstanceUserId:
                    getInstanceUserRequest.Payload.Id = request.InstanceUserId;
                    break;
                case CheckIfInstanceUserIsRegisteredRequest.IdentityOneofCase.InstanceUserName:
                    getInstanceUserRequest.Payload.Name = request.InstanceUserName;
                    break;
                default:
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Instance user identity is not provided."));
            }
            var getInstanceUserResponse = await this.instanceUserManagementClient.GetUserAsync(getInstanceUserRequest, cancellationToken: context.CancellationToken);
            var registration = await this.registeredInstanceUserQueryHandler.Handle(new Database.Queries.GetRegisteredInstanceUserQueryAsync()
            {
                InstanceId = request.InstanceId,
                InstanceUserId = getInstanceUserResponse.Id
            }, cancellationToken: context.CancellationToken);

            var response = new CheckIfInstanceUserIsRegisteredResponse()
            {
                InstanceUser = new InstanceUser()
                {
                    Id = getInstanceUserResponse.Id,
                    Name = getInstanceUserResponse.Name,
                    Group = getInstanceUserResponse.Group
                }
            };
            if (registration is null)
            {
                response.IsRegistered = false;
                response.InstanceId = request.InstanceId;
                return response;
            }

            response.IsRegistered = true;
            response.UserId = registration.UserId;
            response.InstanceId = registration.InstanceId;
            return response;
        }

        public override async Task<GetRegisteredInstanceUserResponse> GetRegisteredInstanceUser(GetRegisteredInstanceUserRequest request, ServerCallContext context)
        {
            var registration = await this.registeredInstanceUserQueryHandler.Handle(new Database.Queries.GetRegisteredInstanceUserQueryAsync()
            {
                InstanceId = request.InstanceId,
                InstanceUserId = request.InstanceUserId
            }, context.CancellationToken);
            if (registration is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Could not find registered instance user."));
            }


            var getUserCall = this.instanceUserManagementClient.GetUserAsync(new Protos.Services.InstanceGateway.GetUserRequest()
            {
                InstanceId = registration.InstanceId,
                Payload = new Protos.InstancePlugins.InstanceManagement.GetUserRequest()
                {
                    Id = registration.InstanceUserId
                }
            }, cancellationToken: context.CancellationToken);
            using (getUserCall)
            {
                var getUserResponse = await getUserCall.ResponseAsync.ConfigureAwait(false);
                return new GetRegisteredInstanceUserResponse()
                {
                    UserId = registration.UserId,
                    InstanceId = registration.InstanceId,
                    InstanceUser = new InstanceUser()
                    {
                        Id = getUserResponse.Id,
                        Name = getUserResponse.Name,
                        Group = getUserResponse.Group
                    }
                };
            }
        }

        public override async Task<GetRegisteredInstanceUsersResponse> GetRegisteredInstanceUsers(GetRegisteredInstanceUsersRequest request, ServerCallContext context)
        {
            var response = new GetRegisteredInstanceUsersResponse()
            {
                UserId = request.UserId,
                InstanceId = request.InstanceId
            };

            var query = new Database.Queries.GetRegisteredInstanceUsersQueryAsync()
            {
                InstanceId = request.InstanceId,
                UserId = request.UserId
            };
            var registrations = registeredInstanceUserQueryHandler.Handle(query, context.CancellationToken);

            await foreach (var registration in registrations.ConfigureAwait(false))
            {
                var getUserCall = instanceUserManagementClient.GetUserAsync(new Protos.Services.InstanceGateway.GetUserRequest()
                {
                    InstanceId = registration.InstanceId,
                    Payload = new Protos.InstancePlugins.InstanceManagement.GetUserRequest()
                    {
                        Id = registration.InstanceUserId
                    }
                }, cancellationToken: context.CancellationToken);
                using (getUserCall)
                {
                    var getUserResponse = await getUserCall.ResponseAsync.ConfigureAwait(false);
                    response.InstanceUsers.Add(new InstanceUser()
                    {
                        Id = getUserResponse.Id,
                        Name = getUserResponse.Name,
                        Group = getUserResponse.Group
                    });
                }
            }
            return response;
        }

        public override async Task<RegisterNewInstanceUserResponse> RegisterNewInstanceUser(RegisterNewInstanceUserRequest request, ServerCallContext context)
        {
            var createUserResponse = await this.instanceUserManagementClient.CreateUserAsync(new Protos.Services.InstanceGateway.CreateUserRequest()
            {
                InstanceId = request.InstanceId,
                Payload = new Protos.InstancePlugins.InstanceManagement.CreateUserRequest()
                {
                    Name = request.InstanceUserName,
                    Password = request.InstanceUserPassword
                }
            }, cancellationToken: context.CancellationToken);

            await this.registeredInstanceUserCommandHandler.Handle(new Database.Commands.RegisterInstanceUserCommandAsync()
            {
                UserId = request.UserId,
                InstanceId = request.InstanceId,
                InstanceUserId = createUserResponse.Id
            }, context.CancellationToken);

            return new RegisterNewInstanceUserResponse()
            {
                UserId = request.UserId,
                InstanceId = request.InstanceId,
                InstanceUser = new InstanceUser()
                {
                    Id = createUserResponse.Id,
                    Name = createUserResponse.Name,
                    Group = createUserResponse.Group
                }
            };
        }

        public override async Task<RegisterExistingInstanceUserResponse> RegisterExistingInstanceUser(RegisterExistingInstanceUserRequest request, ServerCallContext context)
        {
            var getTShockUserResponse = await this.instanceUserManagementClient.GetUserAsync(new Protos.Services.InstanceGateway.GetUserRequest()
            {
                InstanceId = request.InstanceId,
                Payload = new Protos.InstancePlugins.InstanceManagement.GetUserRequest()
                {
                    Name = request.InstanceUserName
                }
            }, cancellationToken: context.CancellationToken);
            var verifyPasswordResponse = await this.instanceUserManagementClient.VerifyUserPasswordAsync(new Protos.Services.InstanceGateway.VerifyUserPasswordRequest()
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

            await this.registeredInstanceUserCommandHandler.Handle(new Database.Commands.RegisterInstanceUserCommandAsync()
            {
                InstanceId = request.InstanceId,
                InstanceUserId = getTShockUserResponse.Id,
                UserId = request.UserId
            }, context.CancellationToken);

            return new RegisterExistingInstanceUserResponse()
            {

            };
        }

        public override async Task<DeregisterInstanceUserResponse> DeregisterInstanceUser(DeregisterInstanceUserRequest request, ServerCallContext context)
        {
            var isDeleted = await this.registeredInstanceUserCommandHandler.Handle(new Database.Commands.DeregisterInstanceUserCommandAsync()
            {
                InstanceId = request.InstanceId,
                InstanceUserId = request.InstanceUserId
            });

            if (!isDeleted)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User had not registered the instance user."));
            }

            return new DeregisterInstanceUserResponse()
            {

            };
        }
    }
}
