// -----------------------------------------------------------------------
// <copyright file="DriveItemEntity.cs" company="Microsoft Corporation">
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
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// The DriveItemEntity.
    /// </summary>
    /// <seealso cref="Microsoft.Azure.Cosmos.Table.TableEntity" />
    public class DriveItemEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the DriveId for the library.
        /// </summary>
        /// <value>
        /// The Drive Id
        /// </value>
        public string DriveId { get; set; }

        /// <summary>
        /// Gets or sets the c tag.
        /// </summary>
        /// <value>
        /// The c tag.
        /// </value>
        public string DocumentCTag { get; set; }

        /// <summary>
        /// Gets or sets the e tag.
        /// </summary>
        /// <value>
        /// The e tag.
        /// </value>
        public string DocumentETag { get; set; }

        /// <summary>
        /// Gets or sets the list identifier.
        /// </summary>
        /// <value>
        /// The list identifier.
        /// </value>
        public string ListId { get; set; }

        /// <summary>
        /// Gets or sets the list item identifier.
        /// </summary>
        /// <value>
        /// The list item identifier.
        /// </value>
        public string ListItemId { get; set; }

        /// <summary>
        /// Gets or sets the list item unique identifier.
        /// </summary>
        /// <value>
        /// The list item unique identifier.
        /// </value>
        public string ListItemUniqueId { get; set; }

        /// <summary>
        /// Gets or sets the site identifier.
        /// </summary>
        /// <value>
        /// The site identifier.
        /// </value>
        public string SiteId { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Reviewed.")]

        /// <summary>
        /// Gets or sets the site URL.
        /// </summary>
        /// <value>
        /// The site URL.
        /// </value>
        public string SiteUrl { get; set; }

        /// <summary>
        /// Gets or sets the web identifier.
        /// </summary>
        /// <value>
        /// The web identifier.
        /// </value>
        public string WebId { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Reviewed.")]

        /// <summary>
        /// Gets or sets the web URL.
        /// </summary>
        /// <value>
        /// The web URL.
        /// </value>
        public string WebUrl { get; set; }
    }
}
