// -----------------------------------------------------------------------
// <copyright file="ConfigurationEntity.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------

namespace Shell.SPOCPI.MigrationPipeline.WebJob
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// The Configuration Entity.
    /// </summary>
    /// <seealso cref="Microsoft.WindowsAzure.Storage.Table.TableEntity" />
    public class ConfigurationEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the configuration key.
        /// </summary>
        /// <value>
        /// The configuration key.
        /// </value>
        public string ConfigKey { get; set; }

        /// <summary>
        /// Gets or sets the configuration value.
        /// </summary>
        /// <value>
        /// The configuration value.
        /// </value>
        public string ConfigValue { get; set; }
    }
}
