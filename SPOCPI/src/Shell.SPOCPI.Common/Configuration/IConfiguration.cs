// <copyright file="IConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.Common
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Configuration Interface.
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// Get configuration value
        /// </summary>
        /// <param name="configKey">Configuration key</param>
        /// <returns>Configuration value</returns>
        dynamic GetConfigValue(string configKey);
    }
}
