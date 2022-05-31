// <copyright file="NotificationEntity.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.Common
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// The notification message entity.
    /// </summary>
    /// <seealso cref="Microsoft.Azure.Cosmos.Table.TableEntity" />
    public class NotificationEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the message JSON.
        /// </summary>
        /// <value>
        /// The message JSON.
        /// </value>
        public string MessageJson { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the attempts count.
        /// </summary>
        /// <value>
        /// The attempts count.
        /// </value>
        public int AttemptsCount { get; set; }

        /// <summary>
        /// Gets or sets the received time.
        /// </summary>
        /// <value>
        /// The received time.
        /// </value>
        public DateTimeOffset ReceivedTime { get; set; }
    }
}
