// -----------------------------------------------------------------------
// <copyright file="DriveDeltaEntity.cs" company="Microsoft Corporation">
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
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// The Drive Delta Entity.
    /// </summary>
    /// <seealso cref="Microsoft.Azure.Cosmos.Table.TableEntity" />
    public class DriveDeltaEntity : TableEntity
    {
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Reviewed.")]

        /// <summary>
        /// Gets or sets the delta URL.
        /// </summary>
        /// <value>
        /// The delta URL.
        /// </value>
        public string DeltaUrl { get; set; }

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [in progress].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [in progress]; otherwise, <c>false</c>.
        /// </value>
        public bool InProgress { get; set; }

        /// <summary>
        /// Gets or sets the received time.
        /// </summary>
        /// <value>
        /// The received time.
        /// </value>
        public DateTimeOffset ReceivedTime { get; set; }

        /// <summary>
        /// Gets or sets the delta token refreshed time.
        /// </summary>
        /// <value>
        /// The delta token refreshed time.
        /// </value>
        public DateTimeOffset? DeltaTokenRefreshed { get; set; }
    }
}
