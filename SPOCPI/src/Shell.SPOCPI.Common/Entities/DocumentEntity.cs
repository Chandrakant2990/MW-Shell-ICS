// -----------------------------------------------------------------------
// <copyright file="DocumentEntity.cs" company="Microsoft Corporation">
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

    /// <summary>
    /// The Document Entity.
    /// </summary>
    /// <seealso cref="DriveItemEntity" />
    public class DocumentEntity : DriveItemEntity
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the extension.
        /// </summary>
        /// <value>
        /// The extension.
        /// </value>
        public string Extension { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether /[c tag change].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [c tag change]; otherwise, <c>false</c>.
        /// </value>
        public bool DocumentCTagChange { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [e tag change].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [e tag change]; otherwise, <c>false</c>.
        /// </value>
        public bool DocumentETagChange { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether DriveItem is folder or not
        /// </summary>
        /// <value>
        ///    <c>true</c> if the item is a folder; otherwise, <c>false</c>.
        /// </value>
        public bool IsFolder { get; set; }

        /// <summary>
        /// Gets or sets the size of drive item
        /// </summary>
        /// <value>File size.</value>
        public long? FileSize { get; set; }

        /// <summary>
        /// Gets or sets the internal subscription id of the drive
        /// </summary>
        /// <value>Internal Subscription Id.</value>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the Parent Id
        /// </summary>
        /// <value>Parent Id.</value>
        public string ParentID { get; set; }

        /// <summary>
        /// Gets or sets the created datetime of the drive item
        /// </summary>
        /// <value>Created date time.</value>
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the modified datetime of the drive item
        /// </summary>
        /// <value>Modified date time..</value>
        public DateTime? ModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether DriveItem is deleted or not
        /// </summary>
        /// <value>
        ///    <c>true</c> if deleted; otherwise, <c>false</c>.
        /// </value>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the queue name.
        /// </summary>
        /// <value>Queue Name.</value>
        public string QueueName { get; set; }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public string Parameters { get; set; }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public string ParentFolderUrl { get; set; }
    }
}
