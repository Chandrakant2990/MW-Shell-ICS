// -----------------------------------------------------------------------
// <copyright file="GraphResponse.cs" company="Microsoft Corporation">
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
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    /// <summary>
    /// Graph Response Model.
    /// </summary>
    /// <typeparam name="T">Generic Parameter.</typeparam>
    public class GraphResponse<T>
    {
        /// <summary>
        /// Gets or sets the context.
        /// </summary>
        /// <value>
        /// The context.
        /// </value>
        [JsonProperty(PropertyName = "@odata.context")]
        public string Context { get; set; }

        /// <summary>
        /// Gets or sets the delta link.
        /// </summary>
        /// <value>
        /// The delta link.
        /// </value>
        [JsonProperty(PropertyName = "@odata.deltalink")]
        public string DeltaLink { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Reviewed.")]

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        [JsonProperty(PropertyName = "value")]
        public List<T> Value { get; set; }
    }
}
