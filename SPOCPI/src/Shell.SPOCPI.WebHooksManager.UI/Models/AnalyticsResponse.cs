// -----------------------------------------------------------------------
// <copyright file="AnalyticsResponse.cs" company="Microsoft Corporation">
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
    using System.Collections.Generic;
    using Azure.Search.Documents.Models;
    using Newtonsoft.Json;
    using Shell.SPOCPI.WebHooksManager.UI.Entities;

    /// <summary>
    /// The analytics response.
    /// </summary>
    public class AnalyticsResponse
    {
        /// <summary>
        ///  Gets or sets the common metrics information.
        /// </summary>
        /// <value>The common metrics information.</value>
        [JsonProperty("common")]
        public Azure.Pageable<SearchResult<AzureCommonMetricsEntity>> Common { get; set; }

        /// <summary>
        ///  Gets or sets the azure library metrics information.
        /// </summary>
        /// <value>The azure library metrics information.</value>
        [JsonProperty("azure")]
        public List<SearchResult<AzureLibraryMetricsEntity>> Azure { get; set; }

        /// <summary>
        ///  Gets or sets the sharepoint library metrics information.
        /// </summary>
        /// <value>The sharepoint library metrics information.</value>
        [JsonProperty("sharepoint")]
        public List<SearchResult<SharePointLibraryMetricsEntity>> SharePoint { get; set; }

        /// <summary>
        ///  Gets or sets the gijgo compatible sites dropdown json.
        /// </summary>
        /// <value>The gijgo compatible sites dropdown json.</value>
        [JsonProperty("sites")]
        public string Sites { get; set; }
    }
}
