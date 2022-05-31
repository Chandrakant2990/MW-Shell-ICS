// -----------------------------------------------------------------------
// <copyright file="Operations.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------

namespace Shell.SPOCPI.WebHooksManager.WebJob
{
    using System;
    using System.Net.Http;
    using Shell.SPOCPI.Common;

    /// <summary>
    /// The operations class.
    /// </summary>
    public class Operations
    {
        /// <summary>
        /// The logger component instance.
        /// </summary>
        private readonly ILoggerComponent logger;

        /// <summary>
        /// The configuration instance.
        /// </summary>
        private readonly IConfiguration config;

        /// <summary>
        /// The subscriptionHelper instance.
        /// </summary>
        private readonly SubscriptionHelper subscriptionHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="Operations"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="config">The configuration.</param>
        public Operations(ILoggerComponent logger, IConfiguration config, IHttpClientFactory clientFactory)
        {
            this.logger = logger;
            this.config = config;
            this.subscriptionHelper = new SubscriptionHelper(logger, config, clientFactory);
        }

        /// <summary>
        /// StartOperations is the starting point for conducting the various operations related to WebJob like creating , updating and deleting subscriptions.
        /// </summary>
        /// <param name="subscriptionHandler">The subscriptionHandler delegate object.</param>
        public void StartOperations(Action subscriptionHandler)
        {
            try
            {
                subscriptionHandler();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }
        }

        /// <summary>
        /// CreateSubscription : This method is used to create graph subscription on a SharePoint library.
        /// </summary>
        public void CreateSubscription()
        {
            try
            {
                this.subscriptionHelper.CreateSubscription().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, nameof(this.CreateSubscription), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }
        }

        /// <summary>
        /// UpdateSubscription find all the items where the ExpirationDateTime is going to expire in the next 7 days and update the subscription's ExpirationDateTime to next 21 days or based on the Config entry.
        /// </summary>
        public void RenewSubscription()
        {
            try
            {
                this.subscriptionHelper.RenewSubscription();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, nameof(this.RenewSubscription), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }
        }

        /// <summary>
        /// DeleteSubscription finds all the items where the status is "Delete". It first delete the subscription associated with the item and after that delete the item
        /// also.
        /// </summary>
        public void DeleteSubscription()
        {
            try
            {
                this.subscriptionHelper.DeleteSubscription();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, nameof(this.DeleteSubscription), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }
        }

        /// <summary>
        /// DisableSubscription finds all the items where the status is "Disabled" and delete the subscription associated with each item.
        /// </summary>
        public void DisableSubscription()
        {
            try
            {
                this.subscriptionHelper.DisableSubscription();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, nameof(this.DisableSubscription), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }
        }
    }
}
