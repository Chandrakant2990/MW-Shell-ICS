// <copyright file="Functions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.WebHooksManager.WebJob
{
    using System;
    using System.Globalization;
    using System.Net.Http;
    using Microsoft.Azure.WebJobs;
    using Shell.SPOCPI.Common;
    using WebHooksManager.WebJob.Filters;

    /// <summary>
    /// Functions will be the entry point for the Azure Web Job functionality.
    /// </summary>
#pragma warning disable CS0612 // Type or member is obsolete
    [ErrorHandler]
#pragma warning restore CS0612 // Type or member is obsolete

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
        /// The client factory
        /// </summary>
        private readonly IHttpClientFactory clientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Functions"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="logger">The logger.</param>
        public Functions(IConfiguration config, ILoggerComponent logger, IHttpClientFactory clientFactory)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

            this.logger.LogInformation(Properties.Resources.FunctionInitializationMessage, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
        }

        /// <summary>
        /// The program start from ProcessQueueMessage method.
        /// </summary>
        /// <param name="timerInfo">The timerInfo object.</param>
        public void ProcessQueueMessage([TimerTrigger(typeof(CustomMinuteSchedule))] TimerInfo timerInfo)
        {
            timerInfo = timerInfo ?? throw new ArgumentNullException(nameof(timerInfo));

            try
            {
                this.logger.LogInformation(Resource.ProcessQueueTrigerred + DateTime.Now.ToString(CultureInfo.InvariantCulture), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                Operations operations = new Operations(this.logger, this.config, this.clientFactory);

                Action subscriptionHandler = operations.CreateSubscription;
                subscriptionHandler += operations.DeleteSubscription;
                subscriptionHandler += operations.DisableSubscription;
                subscriptionHandler += operations.RenewSubscription;

                operations.StartOperations(subscriptionHandler);

                this.logger.LogInformation(Resource.ProcessQueueCompleted + DateTime.Now.ToString(CultureInfo.InvariantCulture), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }
        }
    }
}
