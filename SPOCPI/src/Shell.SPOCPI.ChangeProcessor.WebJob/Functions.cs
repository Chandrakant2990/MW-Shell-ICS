// <copyright file="Functions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.ChangeProcessor.WebJob
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using ChangeProcessor.WebJob.Filters;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Shell.SPOCPI.Common;

    /// <summary>
    /// Web job Functions.
    /// </summary>
    [ErrorHandler]
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
        /// The queue helper.
        /// </summary>
        private readonly QueueHelper queueHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="Functions"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="loggerComponent">The logger.</param>
        /// <param name="queueHelperInstance">The queue helper.</param>
        public Functions(IConfiguration configuration, ILoggerComponent loggerComponent, QueueHelper queueHelperInstance)
        {
            this.config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.logger = loggerComponent ?? throw new ArgumentNullException(nameof(loggerComponent));
            this.queueHelper = queueHelperInstance ?? throw new ArgumentNullException(nameof(queueHelperInstance));

            this.logger.LogInformation(Resource.FunctionsInitializationSuccess, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]

        /// <summary>Processes the job.</summary>
        /// <param name="timerInfo">The timer information.</param>
        /// <param name="loggerComponent">The logger.</param>
        public void ProcessJob([TimerTrigger(typeof(CustomMinuteSchedule))]TimerInfo timerInfo, ILogger loggerComponent)
        {
            try
            {
                this.logger.LogInformation(Resource.ProcessQueueTrigerred + DateTime.Now.ToString(CultureInfo.InvariantCulture), Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));

                CPHelper helper = new CPHelper(this.logger, this.config, this.queueHelper);
                helper.ProcessQueueMessages().Wait();

                this.logger.LogInformation(Resource.ProcessQueueCompleted + DateTime.Now.ToString(CultureInfo.InvariantCulture), Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            }
        }
    }
}