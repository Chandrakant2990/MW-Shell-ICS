// -----------------------------------------------------------------------
// <copyright file="SiteAndLibraryModel.cs" company="Microsoft Corporation">
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
    /// <summary>
    /// The site and library model.
    /// </summary>
    public class SiteAndLibraryModel
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
    }
}