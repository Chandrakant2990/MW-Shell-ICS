// -----------------------------------------------------------------------
// <copyright file="SharePointLibraryMetricsEntity.cs" company="Microsoft Corporation">
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
    /// The SharePoint library metrics entity class.
    /// </summary>
    public class SharePointLibraryMetricsEntity : TableEntity
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
        /// Gets or sets the file type.
        /// </summary>
        /// <value>The file type.</value>
        public string FileType { get; set; }

        /// <summary>
        /// Gets or sets the file size.
        /// </summary>
        /// <value>The file size.</value>
        public string Size { get; set; }

        /// <summary>
        /// Gets or sets the total row count.
        /// </summary>
        /// <value>The total row count.</value>
        public long TotalRowCount { get; set; }
    }
}
