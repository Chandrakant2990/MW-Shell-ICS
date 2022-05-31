// -----------------------------------------------------------------------
// <copyright file="RedisModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Shell.SPOCPI.WebHooksManager.UI.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// RedisRecreateResponseModel Model.
    /// </summary>
    public class RedisModel
    {
        /// <summary>
        /// Gets or sets the RedisKey .
        /// </summary>
        /// <value>
        /// The request identifier.
        /// </value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the Error .
        /// </summary>
        /// <value>
        /// The Error.
        /// </value>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the Status .
        /// </summary>
        /// <value>
        /// The Status.
        /// </value>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the Status .
        /// </summary>
        /// <value>
        /// The Status.
        /// </value>
        public Dictionary<string, string> Data { get; set; }
    }
}
