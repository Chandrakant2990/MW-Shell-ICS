// <copyright file="Functions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.RefreshDeltaToken.WebJob
{
    using System;
    using System.Globalization;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Shell.SPOCPI.Common;

    /// <summary>
    /// Web job Functions.
    /// </summary>
    public class Functions
    {
        /// <summary>
        /// The configuration.
        /// </summary>
        private readonly IConfiguration config;

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILoggerComponent logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Functions"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="loggerComponent">The logger.</param>
        /// <param name="queueHelperInstance">The queue helper.</param>
        public Functions(IConfiguration configuration, ILoggerComponent loggerComponent)
        {
            this.config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.logger = loggerComponent ?? throw new ArgumentNullException(nameof(loggerComponent));
            this.logger.LogInformation(Resources.FunctionsInitializationSuccess, Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));
        }

        /// <summary>Processes the job.</summary>
        /// <param name="timerInfo">The timer information.</param>
        /// <param name="loggerComponent">The logger.</param>
        public void ProcessJob([TimerTrigger(typeof(CustomMinuteSchedule))] TimerInfo timerInfo, ILogger loggerComponent)
        {
            try
            {
                this.logger.LogInformation(Resources.ProcessRefreshTrigerred + DateTime.Now.ToString(CultureInfo.InvariantCulture), Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));

                Helper helper = new Helper(this.logger, this.config);
                helper.ProcessDeltaTokens().Wait();

                this.logger.LogInformation(Resources.ProcessRefreshCompleted + DateTime.Now.ToString(CultureInfo.InvariantCulture), Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));
            }
        }
    }
}
