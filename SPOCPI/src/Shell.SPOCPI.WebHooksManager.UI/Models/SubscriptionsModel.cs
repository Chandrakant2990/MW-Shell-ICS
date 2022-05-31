// -----------------------------------------------------------------------
// <copyright file="SubscriptionsModel.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------
namespace Shell.SPOCPI.WebHooksManager.UI.Models
{
    using System;

    /// <summary>
    /// Subscriptions Model.
    /// </summary>
    public class SubscriptionsModel
    {
        /// <summary>
        /// Gets or sets the drive identifier.
        /// </summary>
        /// <value>
        /// The drive identifier.
        /// </value>
        public string DriveId { get; set; }

        /// <summary>
        /// Gets or sets the expiration date time.
        /// </summary>
        /// <value>
        /// The expiration date time.
        /// </value>
        public DateTime? ExpirationDateTime { get; set; }

        /// <summary>
        /// Gets or sets the site identifier.
        /// </summary>
        /// <value>
        /// The site identifier.
        /// </value>
        public string SiteId { get; set; }

        /// <summary>
        /// Gets or sets the site URL.
        /// </summary>
        /// <value>
        /// The site URL.
        /// </value>
        public Uri SiteUrl { get; set; }

        /// <summary>
        /// Gets or sets the library URL.
        /// </summary>
        /// <value>
        /// The library URL.
        /// </value>
        public Uri LibraryUrl { get; set; }

        /// <summary>
        /// Gets or sets the subscription identifier.
        /// </summary>
        /// <value>
        /// The subscription identifier.
        /// </value>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the activity type.
        /// </summary>
        public string OutputQueue { get; set; }

        /// <summary>
        /// Gets or sets the Folder Relative Path.
        /// </summary>
        public string IncludeFolderRelativePath { get; set; }
    }
}
