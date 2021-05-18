using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.TradingSystem
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpcClient<Protos.Services.InstanceGateway.InstanceUserManagement.InstanceUserManagementClient>(options =>
            {
                options.Address = new Uri("http://localhost:3102");
            })
            .ConfigureChannel(channelOptions =>
            {
                channelOptions.Credentials = Grpc.Core.ChannelCredentials.Insecure;
            });
            services.AddGrpc();
            services.AddSingleton<Commons.Database.IUnitOfWorkFactory, Commons.Database.UnitOfWorkFactory>((serviceProvider) =>
            {
                return new Commons.Database.UnitOfWorkFactory("server=127.0.0.1;port=3306;uid=launcher;password=Th3B3stP@ssw0rd3v3r;database=terraria_launcher_trading_system");
            });
            services.AddSingleton<Database.Queries.Handlers.RegisteredInstanceUserQueryHandler>();
            services.AddSingleton<Database.Commands.Handlers.RegisteredInstanceUserCommandHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<GrpcServices.RegisteredInstanceUserService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}
