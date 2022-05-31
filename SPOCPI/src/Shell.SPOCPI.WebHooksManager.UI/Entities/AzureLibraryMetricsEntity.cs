// -----------------------------------------------------------------------
// <copyright file="AzureLibraryMetricsEntity.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------
namespace Shell.SPOCPI.WebHooksManager.UI.Entities
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// The Azure library metrics entity class.
    /// </summary>
    public class AzureLibraryMetricsEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the site url.
        /// </summary>
        /// <value>The site url.</value>
        public string SiteUrl { get; set; }

        /// <summary>
        /// Gets or sets the library url.
        /// </summary>
        /// <value>The library url.</value>
        public string LibraryUrl { get; set; }

        /// <summary>
        /// Gets or sets the file extension.
        /// </summary>
        /// <value>The file extension.</value>
        public string Extension { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the item is a folder or not.
        /// </summary>
        /// <value>The value indicating if the item is a folder or not.</value>
        public string IsFolder { get; set; }

        /// <summary>
        /// Gets or sets the total row count.
        /// </summary>
        /// <value>The total row count.</value>
        public long TotalRowCount { get; set; }
    }
}
