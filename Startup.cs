using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Consul;
using TerrariaLauncher.Commons.Consul.Extensions;
using TerrariaLauncher.Commons.ConsulHelpers;
using TerrariaLauncher.Commons.Database;
using TerrariaLauncher.Commons.Database.CQS.Extensions;

namespace TerrariaLauncher.Services.TradingSystem
{
    public class Startup
    {
        IConfiguration configuration;
        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddConsulService(config =>
            {
                this.configuration.GetSection("Consul").Bind(config);
            });
            services.AddGrpc();
            services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var connectionString = configuration.GetConnectionString("Database");
                return new UnitOfWorkFactory(connectionString);
            });
            services.AddDatabaseCQS().AddHandlers(Assembly.GetExecutingAssembly());
            var consulHost = this.configuration.GetSection("Consul").Get<ConsulHostConfiguration>();
            using (var consulSync = new ConsulSync(consulHost))
            {
                var hubEndPoint = consulSync.GetServiceEndPoint(
                    this.configuration["Services:TerrariaLauncher.Services.GameCoordinator.Hub:ConsulServiceId"]
                );
                var hubUrl = $"http://{hubEndPoint.Address}:{hubEndPoint.Port}";

                services.AddGrpcClient<TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Users.UsersClient>(options =>
                {
                    options.Address = new Uri(hubUrl);
                });
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<GrpcServices.RegisteredGameUserService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}
