// -----------------------------------------------------------------------
// <copyright file="Configuration.cs" company="Microsoft Corporation">
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
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The Configuration.
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Gets or sets the Azure Active Directory Application client identifier.
        /// </summary>
        /// <value>
        /// The Azure Active Directory Application client identifier.
        /// </value>
        public string AADAppClientId { get; set; }

        /// <summary>
        /// Gets or sets the Azure Active Directory Application client secret.
        /// </summary>
        /// <value>
        /// The Azure Active Directory Application client secret.
        /// </value>
        public string AADAppClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the application insights instrumentation key.
        /// </summary>
        /// <value>
        /// The application insights instrumentation key.
        /// </value>
        public string AppInsightsInstrumentationKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is logging enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is logging enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsLoggingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        /// <value>
        /// The tenant identifier.
        /// </value>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the name of the tenant.
        /// </summary>
        /// <value>
        /// The name of the tenant.
        /// </value>
        public string TenantName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is redis cache enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is redis cache enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsRedisCacheEnabled { get; set; }

        /// <summary>
        /// Gets or sets the redis connection string.
        /// </summary>
        /// <value>
        /// The redis connection string.
        /// </value>
        public string RedisConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the web hooks store connection string.
        /// </summary>
        /// <value>
        /// The web hooks store connection string.
        /// </value>
        public string WebHooksStoreConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the name of the subscription table.
        /// </summary>
        /// <value>
        /// The name of the subscription table.
        /// </value>
        public string SubscriptionTableName { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Reviewed.")]

        /// <summary>
        /// Gets or sets the web hook notification URL.
        /// </summary>
        /// <value>
        /// The web hook notification URL.
        /// </value>
        public string WebhookNotificationUrl { get; set; }

        /// <summary>
        /// Gets or sets the name of the search service.
        /// </summary>
        /// <value>
        /// The name of the search service.
        /// </value>
        public string SearchServiceName { get; set; }

        /// <summary>
        /// Gets or sets the search query key.
        /// </summary>
        /// <value>
        /// The search query key.
        /// </value>
        public string SearchQueryKey { get; set; }

        /// <summary>
        /// Gets or sets the search update key.
        /// </summary>
        public string SearchUpdateKey { get; set; }

        /// <summary>
        /// Gets or sets the name of the subscription index.
        /// </summary>
        /// <value>
        /// The name of the subscription index.
        /// </value>
        public string SubscriptionIndexName { get; set; }

        /// <summary>
        /// Gets or sets the name of the tracking index.
        /// </summary>
        /// <value>
        /// The name of the tracking index.
        /// </value>
        public string TrackingIndexName { get; set; }

        /// <summary>Gets or sets the Change Processor drive delta store connection string.</summary>
        /// <value>The Change Processor drive delta store connection string.</value>
        public string CPDriveDeltaStoreConnString { get; set; }

        /// <summary>Gets or sets the name of the Change Processor drive delta table.</summary>
        /// <value>The name of the Change Processor drive delta table.</value>
        public string CPDriveDeltaTableName { get; set; }

        /// <summary>
        /// Gets or sets the Change Processor service bus connection string.
        /// </summary>
        /// <value>
        /// The Change Processor service bus connection string.
        /// </value>
        public string CPServiceBusConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the name of the Change Processor input queue.
        /// </summary>
        /// <value>
        /// The name of the Change Processor input queue.
        /// </value>
        public string CPInputQueueName { get; set; }

        /// <summary>
        /// Gets or sets the name of the Change Processor output queue.
        /// </summary>
        /// <value>
        /// The name of the Change Processor output queue.
        /// </value>
        public string CPOutputQueueName { get; set; }

        /// <summary>Gets or sets the name of the DocumentTracking table name.</summary>
        /// <value>The name of the DocumentTracking table.</value>
        public string DocumentTrackingTableName { get; set; }

        /// <summary>
        /// Gets or sets the Change Processor query fields.
        /// </summary>
        /// <value>
        /// The Change Processor query fields.
        /// </value>
        public string CPQueryFields { get; set; }

        /// <summary>
        /// Gets or sets the Change Processor notification fetch count.
        /// </summary>
        /// <value>
        /// The number of items to fetch from Notification storage.
        /// </value>
        public int CPNotificationFetchCount { get; set; }

        /// <summary>
        /// Gets or sets the Change Processor maximum retry attempts.
        /// </summary>
        /// <value>
        /// The Change Processor maximum retry attempts.
        /// </value>
        public int CPMaxRetryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the output queue names.
        /// </summary>
        /// <value>
        /// The output queue names.
        /// </value>
        public string OutputQueueNames { get; set; }
    }
}
