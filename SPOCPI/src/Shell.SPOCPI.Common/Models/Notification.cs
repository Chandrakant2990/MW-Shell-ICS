// -----------------------------------------------------------------------
// <copyright file="Notification.cs" company="Microsoft Corporation">
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
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Notification Model.
    /// </summary>
    [DataContract]
    public class Notification
    {
        /// <summary>
        /// Gets or sets the subscription identifier.
        /// </summary>
        /// <value>
        /// The subscription identifier.
        /// </value>
        [DataMember]
        [JsonProperty(PropertyName = "subscriptionId")]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the state of the client.
        /// </summary>
        /// <value>
        /// The state of the client.
        /// </value>
        [DataMember]
        [JsonProperty(PropertyName = "clientState")]
        public string ClientState { get; set; }

        /// <summary>
        /// Gets or sets the subscription expiration date time.
        /// </summary>
        /// <value>
        /// The subscription expiration date time.
        /// </value>
        [DataMember]
        [JsonProperty(PropertyName = "subscriptionExpirationDateTime")]
        public string SubscriptionExpirationDateTime { get; set; }

        /// <summary>
        /// Gets or sets the resource.
        /// </summary>
        /// <value>
        /// The resource.
        /// </value>
        [DataMember]
        [JsonProperty(PropertyName = "resource")]
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets the resource data.
        /// </summary>
        /// <value>
        /// The resource data.
        /// </value>
        [DataMember]
        [JsonProperty(PropertyName = "resourceData")]
        public string ResourceData { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        /// <value>
        /// The tenant identifier.
        /// </value>
        [DataMember]
        [JsonProperty(PropertyName = "tenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the type of the change.
        /// </summary>
        /// <value>
        /// The type of the change.
        /// </value>
        [DataMember]
        [JsonProperty(PropertyName = "changeType")]
        public string ChangeType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [bypass spo notification].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [bypass spo notification]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        [JsonProperty(PropertyName = "bypassSpoNotification")]
        public bool BypassSpoNotification { get; set; }

        /// <summary>
        /// Gets or sets the queue names.
        /// </summary>
        /// <value>
        /// The queue names.
        /// </value>
        [DataMember]
        [JsonProperty(PropertyName = "queueNames")]
        public string QueueNames { get; set; }
    }
}
