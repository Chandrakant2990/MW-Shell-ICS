// -----------------------------------------------------------------------
// <copyright file="IndexRequest.cs" company="Microsoft Corporation">
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
    public class IndexRequest
    {
        /// <summary>
        /// Gets or sets the subscription.
        /// </summary>
        /// <value>
        /// The subscription.
        /// </value>
        public SubscriptionEntity Subscription { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [bypass spo notification].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [bypass spo notification]; otherwise, <c>false</c>.
        /// </value>
        public bool BypassSpoNotification { get; set; }

        /// <summary>
        /// Gets or sets the queue names.
        /// </summary>
        /// <value>
        /// The queue names.
        /// </value>
        public string QueueNames { get; set; }
    }
}
