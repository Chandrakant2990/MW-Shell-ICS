// -----------------------------------------------------------------------
// <copyright file="Request.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------

namespace Shell.SPOCPI.MigrateSubscription
{
    /// <summary>
    /// Migration request.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// The subscription table partition key.
        /// </summary>
        public string SubscriptionPartitionKey { get; set; }

        /// <summary>
        /// The subscription table row key.
        /// </summary>
        public string SubscriptionRowKey { get; set; }

        /// <summary>
        /// The spo subscription id.
        /// </summary>
        public string SPOSubscriptionId { get; set; }
    }
}
