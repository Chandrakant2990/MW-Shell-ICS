// -----------------------------------------------------------------------
// <copyright file="SubscriptionEntity.cs" company="Microsoft Corporation">
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
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// The Subscription entity.
    /// </summary>
    /// <seealso cref="Microsoft.WindowsAzure.Storage.Table.TableEntity" />
    public class SubscriptionEntity : TableEntity
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
        /// Gets or sets the subscription creation date time.
        /// </summary>
        /// <value>
        /// The creation date time.
        /// </value>
        public DateTime? CreationDateTime { get; set; }

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
        public string SiteUrl { get; set; }

        /// <summary>
        /// Gets or sets the library URL.
        /// </summary>
        /// <value>
        /// The library URL.
        /// </value>
        public string LibraryUrl { get; set; }

        /// <summary>
        /// Gets the subscription identifier.
        /// </summary>
        /// <value>
        /// The subscription identifier.
        /// </value>
        public string SubscriptionId
        {
            get { return this.RowKey; }
        }

        /// <summary>
        /// Gets or sets the SPO subscription Id.
        /// </summary>
        public string SPOSubscriptionId { get; set; }

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
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the Output queue(s) name. Multiple queue names will be stored in semicolon format.
        /// </summary>
        public string OutputQueue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [automatic index].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [automatic index]; otherwise, <c>false</c>.
        /// </value>
        public bool AutoIndex { get; set; }

        /// <summary>
        /// Gets or sets the FolderRelativePath.
        /// </summary>
        public string IncludeFolderRelativePath { get; set; }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public string Parameters { get; set; }

        /// <summary>
        /// Gets or sets the index queues.
        /// </summary>
        /// <value>
        /// The index queue.
        /// </value>
        public string IndexQueues { get; set; }

        /// <summary>
        /// Gets or sets the list identifier.
        /// </summary>
        /// <value>
        /// The list identifier.
        /// </value>
        public string ListId { get; set; }
    }
}
