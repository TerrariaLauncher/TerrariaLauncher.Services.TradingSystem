using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Consul.API.Agent.Services.Commands;
using TerrariaLauncher.Commons.Consul.API.Commons;
using TerrariaLauncher.Commons.Consul.ConfigurationProvider;

namespace TerrariaLauncher.Services.TradingSystem
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using (var host = CreateHostBuilder(args).Build())
            {
                var consulCommandDispatcher = host.Services.GetRequiredService<IConsulCommandDispatcher>();
                var configuration = host.Services.GetRequiredService<IConfiguration>();

                var hostUrl = configuration["urls"].Trim().Split(';', 1, StringSplitOptions.TrimEntries)[0];
                var hostUri = new Uri(hostUrl);

                var registration = configuration.GetSection("ConsulServiceRegister").Get<Commons.Consul.API.DTOs.Registration>();
                var command = new RegisterServiceCommand()
                {
                    ReplaceExistingChecks = true,
                    Registration = registration
                };
                command.Registration.Address = hostUri.Host;
                command.Registration.Port = hostUri.Port;
                command.Registration.Check.TCP = hostUri.Authority;

                await consulCommandDispatcher.Dispatch<RegisterServiceCommand, RegisterServiceCommandResult>(command);
                await host.RunAsync();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
                {
                    IConfigurationBuilder tempConfigurationBuilder = new ConfigurationBuilder();
                    var env = hostBuilderContext.HostingEnvironment;
                    tempConfigurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                    tempConfigurationBuilder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                    var tempConfigurationRoot = tempConfigurationBuilder.Build();

                    var consulHost = new Commons.Consul.ConsulHostConfiguration();
                    tempConfigurationRoot.GetSection("Consul").Bind(consulHost);
                    var consulConfigurationKey = tempConfigurationRoot["ConsulConfigurationProvider:Key"];

                    configurationBuilder.UseConsulConfiguration(consulHost, consulConfigurationKey);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
