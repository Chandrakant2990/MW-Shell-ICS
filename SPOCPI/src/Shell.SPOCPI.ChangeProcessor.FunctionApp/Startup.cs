// <copyright file="Startup.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Shell.SPOCPI.ChangeProcessor.FunctionApp.Startup))]

namespace Shell.SPOCPI.ChangeProcessor.FunctionApp
{
    using Shell.SPOCPI.Common;

    /// <summary>
    /// The function start up class.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// Configures the specified builder.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (builder != null)
            {
                builder.Services.AddSingleton<IConfiguration, SPOCPIConfiguration>();
                builder.Services.AddSingleton<ILoggerComponent, LoggerComponent>();
            }
        }
    }
}
