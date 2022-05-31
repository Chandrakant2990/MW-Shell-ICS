// -----------------------------------------------------------------------
// <copyright file="StorageTableContext.cs" company="Microsoft Corporation">
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
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// The Storage Table Context.
    /// </summary>
    public class StorageTableContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageTableContext"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="tableName">Name of the table.</param>
        public StorageTableContext(string connectionString, string tableName)
        {
            this.StorageAccount = CloudStorageAccount.Parse(connectionString);
            this.Table = this.StorageAccount.CreateCloudTableClient().GetTableReference(tableName);
        }

        /// <summary>
        /// Gets or sets the storage account.
        /// </summary>
        /// <value>
        /// The storage account.
        /// </value>
        public CloudStorageAccount StorageAccount { get; set; }

        /// <summary>
        /// Gets or sets the subscription table.
        /// </summary>
        /// <value>
        /// The subscription table.
        /// </value>
        public CloudTable Table { get; set; }
    }
}