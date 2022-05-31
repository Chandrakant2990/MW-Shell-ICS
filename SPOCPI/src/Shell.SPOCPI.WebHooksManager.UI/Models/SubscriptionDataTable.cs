// -----------------------------------------------------------------------
// <copyright file="SubscriptionDataTable.cs" company="Microsoft Corporation">
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
    using Azure;
    using Azure.Search.Documents.Models;
    using Shell.SPOCPI.Common;

    /// <summary>
    /// Subscription Data Table class
    /// </summary>
    public class SubscriptionDataTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionDataTable"/> class.
        /// </summary>
        public SubscriptionDataTable()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionDataTable"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="draw">The draw.</param>
        /// <param name="recordsFiltered">The records filtered.</param>
        /// <param name="recordsTotal">The records total.</param>
        public SubscriptionDataTable(Pageable<SearchResult<SubscriptionEntity>> data, int? draw, int? recordsFiltered, int? recordsTotal)
        {
            this.Data = data;
            this.Draw = draw;
            this.RecordsFiltered = recordsFiltered;
            this.RecordsTotal = recordsTotal;
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public Pageable<SearchResult<SubscriptionEntity>> Data { get; }

        /// <summary>
        /// Gets or sets the draw.
        /// </summary>
        /// <value>
        /// The draw.
        /// </value>
        public int? Draw { get; set; }

        /// <summary>
        /// Gets or sets the records filtered.
        /// </summary>
        /// <value>
        /// The records filtered.
        /// </value>
        public int? RecordsFiltered { get; set; }

        /// <summary>
        /// Gets or sets the records total.
        /// </summary>
        /// <value>
        /// The records total.
        /// </value>
        public int? RecordsTotal { get; set; }
    }
}
