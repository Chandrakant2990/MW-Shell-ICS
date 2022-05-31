// -----------------------------------------------------------------------
// <copyright file="Response.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------

namespace Shell.SPOCPI.MigrateSubscription
{
    /// <summary>
    /// Migration response.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// The operation status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// The operation message.
        /// </summary>
        public string Message { get; set; }
    }
}
