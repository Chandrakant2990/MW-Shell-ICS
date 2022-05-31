// -----------------------------------------------------------------------
// <copyright file="MigrationEntity.cs" company="Microsoft Corporation">
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
    using System;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// The Subscription entity.
    /// </summary>
    /// <seealso cref="Microsoft.WindowsAzure.Storage.Table.TableEntity" />
    public class MigrationEntity : TableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationEntity"/> class.
        /// </summary>
        public MigrationEntity()
        {
        }

        /// <summary>
        /// Gets or sets the drive identifier.
        /// </summary>
        /// <value>
        /// The drive identifier.
        /// </value>
        public string DriveId { get; set; }

        /// <summary>
        /// Gets or sets the base64 drive identifier.
        /// </summary>
        /// <value>
        /// The drive identifier.
        /// </value>
        public string Base64DriveId { get; set; }

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
        /// Gets or sets the migration status. 
        /// NotStarted, Pending, InProgress, Success, Failed
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the Migration Pipeline Run Id
        /// </summary>
        public string PipelineRunId { get; set; }
    }
}
