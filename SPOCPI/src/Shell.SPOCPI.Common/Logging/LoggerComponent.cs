// -----------------------------------------------------------------------
// <copyright file="LoggerComponent.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------

namespace Shell.SPOCPI.Common
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// The Logger Component.
    /// </summary>
    /// <seealso cref="Common.ILoggerComponent" />
    public class LoggerComponent : ILoggerComponent
    {
        /// <summary>
        /// New static TelemetryClient instance.
        /// </summary>
        private static TelemetryClient telemetryClient = null;

        /// <summary>
        /// The configuration instance.
        /// </summary>
        private readonly IConfiguration configurationInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerComponent"/> class.
        /// </summary>
        /// <param name="configurationInstance">The configuration instance.</param>
        /// <exception cref="ArgumentNullException">configuration Instance.</exception>
        public LoggerComponent(IConfiguration configurationInstance)
        {
            this.configurationInstance = configurationInstance ?? throw new ArgumentNullException(nameof(configurationInstance));

            if (telemetryClient == null)
            {
                string appInsightsKey = configurationInstance.GetConfigValue(SPOCPI.Common.Resource.ConfigAppInsightsInstrumentationKey);
                var config = TelemetryConfiguration.CreateDefault();
                telemetryClient = new TelemetryClient(config)
                {
                    InstrumentationKey = appInsightsKey,
                };
            }
        }

        /// <summary>
        /// Flushes the telemetry client.
        /// </summary>
        public static void FlushTelemetryClient()
        {
            telemetryClient.Flush();
        }

        /// <summary>
        /// Debug only method which is useful for tracing.
        /// </summary>
        /// <param name="message">&gt;Logs information message.</param>
        public void LogDebug(string message)
        {
            telemetryClient.TrackTrace(message, SeverityLevel.Verbose);
            FlushTelemetryClient();
        }

        /// <summary>
        /// Logs warning messages.
        /// </summary>
        /// <param name="message">Logs warning message.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        public void LogWarning(string message, string applicationName, string moduleName)
        {
            Dictionary<string, string> telemetricProps = new Dictionary<string, string>
            {
                [Constants.AppInsightsCategory] = $"{applicationName}-{moduleName}",
                [Constants.ApplicationName] = applicationName,
                [Constants.ModuleName] = moduleName,
            };

            telemetryClient.TrackTrace(message, SeverityLevel.Warning, telemetricProps);
            FlushTelemetryClient();
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="ex">Logs error with exception.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        public void LogError(Exception ex, string applicationName, string moduleName)
        {
            // Data to push into AI which can be searched on
            Dictionary<string, string> telemetricProps = new Dictionary<string, string>()
            {
                [Constants.AppInsightsCategory] = $"{applicationName}-{moduleName}",
                [Constants.ApplicationName] = applicationName,
                [Constants.ModuleName] = moduleName,
            };

            telemetryClient.TrackException(ex, telemetricProps);
            telemetryClient.TrackTrace(ex?.Message, SeverityLevel.Error, telemetricProps);
            FlushTelemetryClient();
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="ex">Logs error with exception.</param>
        /// <param name="message">Logs error with Message.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        public void LogError(Exception ex, string message, string applicationName, string moduleName)
        {
            Dictionary<string, string> telemetricProps = new Dictionary<string, string>()
            {
                [Constants.AppInsightsMessage] = message,
                [Constants.AppInsightsCategory] = $"{applicationName}-{moduleName}",
                [Constants.ApplicationName] = applicationName,
                [Constants.ModuleName] = moduleName,
            };

            telemetryClient.TrackException(ex, telemetricProps);
            telemetryClient.TrackTrace(message, SeverityLevel.Error, telemetricProps);
        }

        /// <summary>
        /// Logs the information.
        /// </summary>
        /// <param name="message">Logs information message.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        public void LogInformation(string message, string applicationName, string moduleName)
        {
            if (this.ShouldLog())
            {
                Dictionary<string, string> telemetricProps = new Dictionary<string, string>()
                {
                    [Constants.AppInsightsCategory] = $"{applicationName}-{moduleName}",
                    [Constants.ApplicationName] = applicationName,
                    [Constants.ModuleName] = moduleName,
                };

                telemetryClient.TrackTrace(message, SeverityLevel.Information, telemetricProps);
                FlushTelemetryClient();
            }
        }

        /// <summary>
        /// Returns whether logging is enabled.
        /// </summary>
        /// <returns>
        /// returns true or false.
        /// </returns>
        public bool ShouldLog()
        {
            return Convert.ToBoolean(this.configurationInstance.GetConfigValue(Resource.ConfigIsLoggingEnabled));
        }
    }
}
