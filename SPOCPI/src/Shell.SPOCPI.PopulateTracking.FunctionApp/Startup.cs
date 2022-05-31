// -----------------------------------------------------------------------
// <copyright file="Startup.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Shell.SPOCPI.Common;

[assembly: FunctionsStartup(typeof(Shell.SPOCPI.PopulateTracking.FunctionApp.Startup))]

namespace Shell.SPOCPI.PopulateTracking.FunctionApp
{
    /// <summary>
    /// Startup function to inject dependencies.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// Builder instance to build dependencies.
        /// </summary>
        /// <param name="builder"><see cref="IFunctionsHostBuilder"/>.</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentException(Resource.BuilderNull, nameof(builder));
            }

            builder.Services.AddSingleton<IConfiguration, SPOCPIConfiguration>();
            builder.Services.AddSingleton<ILoggerComponent, LoggerComponent>();
        }
    }
}
