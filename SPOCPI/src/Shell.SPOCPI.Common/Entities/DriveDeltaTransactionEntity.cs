// -----------------------------------------------------------------------
// <copyright file="DriveDeltaTransactionEntity.cs" company="Microsoft Corporation">
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
    public class DriveDeltaTransactionEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the old delta URL.
        /// </summary>
        /// <value>
        /// The old delta URL.
        /// </value>
        public string OldDeltaUrl { get; set; }

        /// <summary>
        /// Gets or sets the new delta URL.
        /// </summary>
        /// <value>
        /// The new delta URL.
        /// </value>
        public string NewDeltaUrl { get; set; }

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        public string OldToken { get; set; }

        /// <summary>
        /// Gets or sets the old token timestamp.
        /// </summary>
        /// <value>
        /// The old token timestamp.
        /// </value>
        public DateTimeOffset OldTokenTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        public string NewToken { get; set; }

        /// <summary>
        /// Gets or sets the agent.
        /// </summary>
        /// <value>
        /// The agent.
        /// </value>
        public string Agent { get; set; }
    }
}
