// <copyright file="Configuration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.MigrationPipeline.WebJob
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Shell.Common.Core;

    /// <summary>
    ///  Configuration class - Responsible for populating config data from Configuration storage table.
    /// </summary>
    public class Configuration : Shell.Common.Core.IConfiguration
    {
        /// <summary>
        /// The configuration data.
        /// </summary>
        private static Dictionary<string, string> configData = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        public Configuration()
        {
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1126:PrefixCallsCorrectly", Justification = "Stylecop cannot process out variables. False positive.")]

        /// <summary>
        /// Get configuration value
        /// </summary>
        /// <param name="configKey">Configuration key</param>
        /// <returns>Configuration value</returns>
        public dynamic GetConfigValue(string configKey)
        {
            configData = (configData?.Count > 0) ? configData : PopulateConfigurationData().Result;
            if (!configData.TryGetValue(configKey, out string configValue))
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
        private static async Task<Dictionary<string, string>> PopulateConfigurationData()
        {
            configData = new Dictionary<string, string>();
            string cacheConfiguration = string.Empty;

            try
            {
                CacheManager.GetCacheEntry(Constants.SDU, "Configuration", out cacheConfiguration);
            }
            catch
            {
                //// Suppress the issues while trying to read data from Redis cache. Fall back to Storage table
            }

            if (!string.IsNullOrWhiteSpace(cacheConfiguration))
            {
                configData = JsonConvert.DeserializeObject<Dictionary<string, string>>(cacheConfiguration);
            }
            else
            {
                var _ = new ConfigurationBuilder().AddEnvironmentVariables().Build();

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigHelper.StringReader(Constants.MigrationStorageConnectionString, default(string)));
                CloudTable table = storageAccount.CreateCloudTableClient().GetTableReference("Configuration");
                IList<DynamicTableEntity> configTableData = await StorageTableHelper.GetTableRows(table).ConfigureAwait(false);

                // Execute the query and loop through the results
                foreach (DynamicTableEntity entity in configTableData)
                {
                    const string CONFIGENTITYKEYNAME = nameof(ConfigurationEntity.ConfigKey);
                    const string CONFIGENTITYVALUENAME = nameof(ConfigurationEntity.ConfigValue);
                    string configKeyName = entity.Properties[CONFIGENTITYKEYNAME]?.StringValue;
                    string configKeyValue = entity.Properties[CONFIGENTITYVALUENAME]?.StringValue;

                    if (!configData.ContainsKey(configKeyName))
                    {
                        configData.Add(configKeyName, configKeyValue);
                    }
                }

                CacheManager.AddCacheEntry(Constants.SDU, "Configuration", configData);
            }

            return configData;
        }
    }
}