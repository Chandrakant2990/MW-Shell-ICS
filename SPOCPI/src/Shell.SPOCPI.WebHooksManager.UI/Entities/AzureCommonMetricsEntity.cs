// -----------------------------------------------------------------------
// <copyright file="AzureCommonMetricsEntity.cs" company="Microsoft Corporation">
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
    /// The Azure common metrics entity class.
    /// </summary>
    public class AzureCommonMetricsEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the total item count.
        /// </summary>
        public int? TotalCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the item count in last 1 hour.
        /// </summary>
        public string Last01Hours { get; set; }

        /// <summary>
        /// Gets or sets the item count in last 24 hours.
        /// </summary>
        public string Last24Hours { get; set; }
    }
}