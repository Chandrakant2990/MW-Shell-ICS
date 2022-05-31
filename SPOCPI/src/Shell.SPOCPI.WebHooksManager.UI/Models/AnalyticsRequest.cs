// -----------------------------------------------------------------------
// <copyright file="AnalyticsRequest.cs" company="Microsoft Corporation">
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

    /// <summary>
    ///  The analytics request.
    /// </summary>
    public class AnalyticsRequest
    {
        /// <summary>
        /// Gets or sets the site urls.
        /// </summary>
        /// <value>The site urls.</value>
        public List<string> SiteUrls { get; set; }

        /// <summary>
        /// Gets or sets the library urls.
        /// </summary>
        /// <value>The library urls.</value>
        public List<string> LibraryUrls { get; set; }

        /// <summary>
        /// Gets or sets the fetch list.
        /// </summary>
        /// <value>The fetch list.</value>
        public List<string> FetchList { get; set; }
    }
}
