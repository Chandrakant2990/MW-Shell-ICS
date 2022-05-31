// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.RefreshDeltaToken.WebJob
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Shell.SPOCPI.Common;

    /// <summary>
    /// The program class which contains the main method of the web job.
    /// </summary>
    public sealed class Program
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
            var environment = ConfigHelper.StringReader(Common.Constants.NetCoreEnvironmentVariableName, default(string));

            var builder = new HostBuilder()
                .UseEnvironment(Constants.EnvironmentName)
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
                    b.AddJsonFile(Constants.JSONFileName, optional: false, reloadOnChange: true);
                    b.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
                    b.AddEnvironmentVariables().Build();
                })
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddSingleton<ILoggerComponent, LoggerComponent>();
                    serviceCollection.AddSingleton<Common.IConfiguration, SPOCPIConfiguration>();
                    serviceCollection.AddTransient<Functions>();
                })
                .UseConsoleLifetime();

            return builder;
        }
    }
}
