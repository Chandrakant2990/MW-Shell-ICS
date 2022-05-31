// <copyright file="SPOCPIConfiguration.cs" company="Microsoft Corporation">
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
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;

    /// <summary>
    /// Configuration class - Responsible for populating config data from Configuration storage table.
    /// </summary>
    public class SPOCPIConfiguration : IConfiguration
    {
        /// <summary>
        /// The configuration data.
        /// </summary>
        private static Dictionary<string, string> configData = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SPOCPIConfiguration"/> class.
        /// </summary>
        public SPOCPIConfiguration()
        {
        }

        /// <summary>
        /// Get configuration value.
        /// </summary>
        /// <param name="configKey">Configuration key.</param>
        /// <returns>Configuration value.</returns>
        public dynamic GetConfigValue(string configKey)
        {
            string configValue = null;
            configData = (configData != null && configData.Count > 0) ? configData : this.PopulateConfigurationData().Result;
            if (!configData.TryGetValue(configKey, out configValue))
            {
                configValue = null;
            }

            return configValue;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]

        /// <summary>
        /// Populates config data object from config storage table.
        /// </summary>
        /// <returns>Configuration data.</returns>
        public async Task<Dictionary<string, string>> PopulateConfigurationData(string key = Constants.ConfigurationKey)
        {
            configData = new Dictionary<string, string>();
            string cacheConfiguration = string.Empty;

            try
            {
                CacheManager.GetCacheEntry(Constants.SPOCPI, key, out cacheConfiguration);
            }
            catch
            {
                //// Suppress the issues while trying to read data from Redis cache. Fall back to Storage table
            }

            if (!string.IsNullOrEmpty(cacheConfiguration))
            {
                configData = JsonConvert.DeserializeObject<Dictionary<string, string>>(cacheConfiguration);
            }
            else
            {
                var _ = new ConfigurationBuilder().AddEnvironmentVariables().Build();

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigHelper.StringReader(Constants.ConfigTableConnectionStringName, default(string)));
                CloudTable table = storageAccount.CreateCloudTableClient().GetTableReference(Constants.ConfigTableName);
                List<ConfigurationEntity> configTableData = await StorageTableHelper.GetTableResultBasedOnQueryFilter<ConfigurationEntity>(table, TableQuery.GenerateFilterCondition(Constants.PartitionKey, QueryComparisons.Equal, Constants.SPOCPI)).ConfigureAwait(true);

                // Execute the query and loop through the results
                foreach (ConfigurationEntity entity in configTableData)
                {
                    bool configExists = configData.Keys.Any(key => key.Equals(entity.ConfigKey, System.StringComparison.OrdinalIgnoreCase));
                    if (!configExists)
                    {
                        configData.Add(entity.ConfigKey, entity.ConfigValue);
                    }
                }

                // Add the item to cache
                CacheManager.AddCacheEntry(Constants.SPOCPI, key, configData);
            }

            return configData;
        }
    }
}