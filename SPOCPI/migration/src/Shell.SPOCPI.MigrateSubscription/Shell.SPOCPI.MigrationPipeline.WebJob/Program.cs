// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.MigrationPipeline.WebJob
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Shell.Common.Core;
    using ShellCommon = Shell.Common.Core;

    /// <summary>
    /// The Program class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main method.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The task status.</returns>
        public static async Task Main(string[] args)
        {
            var builder = ConfigureServices();
            var host = builder.Build();

            using (host)
            {
                await host.RunAsync().ConfigureAwait(true);
            }
        }

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <returns>The host builder.</returns>
        private static IHostBuilder ConfigureServices()
        {
            var environment = ConfigHelper.StringReader("ASPNETCORE_ENVIRONMENT", default(string));

            var builder = new HostBuilder()
                .UseEnvironment("Development")
                .ConfigureWebJobs(b =>
                {
                    b.AddTimers();
                    b.AddAzureStorageCoreServices();
                    b.AddExecutionContextBinding();
                })
                .ConfigureLogging((context, b) =>
                {
                    b.AddConsole();
                    b.SetMinimumLevel(LogLevel.Debug);
                }).ConfigureHostConfiguration((b) =>
                {
                    b.SetBasePath(System.IO.Directory.GetCurrentDirectory());
                    b.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    b.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
                    b.AddEnvironmentVariables().Build();
                })
                .ConfigureServices(serviceCollection =>
                {
                    var blobConnString = ConfigHelper.StringReader(Constants.MigrationStorageConnectionString, default(string));
                    serviceCollection.AddSingleton<ShellCommon.IConfiguration, Configuration>();
                    serviceCollection.AddSingleton<ILoggerComponent, LoggerComponent>(serviceProvider =>
                    {
                        return new LoggerComponent(serviceProvider.GetRequiredService<ShellCommon.IConfiguration>());
                    });
                    serviceCollection.AddSingleton(s => new BlobService(blobConnString));
                })
                .UseConsoleLifetime();

            return builder;
        }
    }
}
