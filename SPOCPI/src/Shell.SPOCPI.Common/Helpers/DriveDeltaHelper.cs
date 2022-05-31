// -----------------------------------------------------------------------
// <copyright file="DriveDeltaHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------

namespace Shell.SPOCPI.Common.Helpers
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// The Drive Delta helper class.
    /// </summary>
    public class DriveDeltaHelper
    {
        /// <summary>
        /// The logger component.
        /// </summary>
        private readonly ILoggerComponent logger;

        /// <summary>
        /// The configuration instance.
        /// </summary>
        private readonly IConfiguration config;

        /// <summary>
        /// The drive delta table instance.
        /// </summary>
        private readonly CloudTable driveDeltaTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="DriveDeltaHelper" /> class.
        /// </summary>
        /// <param name="config">The configuration instance.</param>
        /// <param name="logger">The logger component.</param>
        public DriveDeltaHelper(IConfiguration config, ILoggerComponent logger)
        {
            this.logger = logger;
            this.config = config;
            this.driveDeltaTable = StorageTableHelper.GetTableEntity(KeyVaultHelper.GetSecret(this.config.GetConfigValue(Common.Resource.ConfigCPDriveDeltaStoreConnString))?.Result?.ToString(), this.config.GetConfigValue(Common.Resource.ConfigCPDriveDeltaTableName));
        }

        /// <summary>
        /// Update the drive delta Entity to the storage.
        /// </summary>
        /// <param name="rowKey">The row key.</param>
        /// <returns>The updated Drive Delta entity.</returns>
        public async Task<DriveDeltaEntity> UpdateDriveDeltaEntity(string rowKey)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                throw new ArgumentNullException(nameof(rowKey));
            }

            DriveDeltaEntity deltaEntity;
            try
            {
                deltaEntity = await this.GetDeltaTokenIfExists(rowKey).ConfigureAwait(false);
                if (deltaEntity != null)
                {
                    deltaEntity.DeltaUrl = null;
                    deltaEntity.Token = null;
                    deltaEntity = await this.UpdateDriveDeltaStatus(deltaEntity).ConfigureAwait(false);
                }
                else
                {
                    // Add a new record with lock state in the delta table
                    deltaEntity = await this.AddDriveDeltaEntity(rowKey).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                throw;
            }

            return deltaEntity;
        }

        /// <summary>
        /// Gets the delta token if exists.
        /// </summary>
        /// <param name="rowKey">The row key.</param>
        /// <returns>The delta token.</returns>
        public async Task<DriveDeltaEntity> GetDeltaTokenIfExists(string rowKey)
        {
            if (this.driveDeltaTable != null)
            {
                string filter = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(Constants.PartitionKey, QueryComparisons.Equal, Constants.DriveDeltaPartitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(Constants.RowKey, QueryComparisons.Equal, rowKey));

                var results = await StorageTableHelper.GetTableResultBasedOnQueryFilter<DriveDeltaEntity>(this.driveDeltaTable, filter).ConfigureAwait(false);

                if (results?.Count > 0)
                {
                    this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.DeltaTokenResultsCount, results.Count), Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    return results.OrderByDescending(r => r.Timestamp).FirstOrDefault();
                }
            }

            return null;
        }

        /// <summary>
        /// Updates the drive delta status.
        /// </summary>
        /// <param name="deltaRow">The delta row.</param>
        /// <returns>
        ///   <c>true</c> if [the update is successful for] [the specified deltaRow]; otherwise, <c>false</c>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Reviewed.")]
        private async Task<DriveDeltaEntity> UpdateDriveDeltaStatus(DriveDeltaEntity deltaRow)
        {
            DriveDeltaEntity deltaEntity = null;
            try
            {
                if (this.driveDeltaTable != null)
                {
                    var result = await StorageTableHelper.UpdateItem(this.driveDeltaTable, deltaRow).ConfigureAwait(false);
                    if (result.HttpStatusCode == (int)HttpStatusCode.OK || result.HttpStatusCode == (int)HttpStatusCode.NoContent)
                    {
                        deltaRow.ETag = result.Etag;
                        return deltaRow;
                    }
                    else if (result.HttpStatusCode == (int)HttpStatusCode.Conflict)
                    {
                        this.logger.LogWarning(Resource.UpdateDriveDeltaStatusConflict, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                    }
                    else
                    {
                        this.logger.LogWarning(Resource.UpdateDriveDeltaStatusError, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
            }

            return deltaEntity;
        }

        /// <summary>
        /// Adds the drive delta Entity to the storage.
        /// </summary>
        /// <param name="rowKey">The row key.</param>
        /// <returns>The newly added Drive Delta entity.</returns>
        private async Task<DriveDeltaEntity> AddDriveDeltaEntity(string rowKey)
        {
            DriveDeltaEntity newSubscriptionSet = null;

            if (this.driveDeltaTable != null)
            {
                newSubscriptionSet = new DriveDeltaEntity()
                {
                    PartitionKey = Constants.DriveDeltaPartitionKey,
                    RowKey = rowKey,
                    InProgress = false,
                    ReceivedTime = DateTime.UtcNow,
                };

                var result = await StorageTableHelper.AddTableItem(this.driveDeltaTable, newSubscriptionSet).ConfigureAwait(false);
                if (result.HttpStatusCode == (int)HttpStatusCode.OK || result.HttpStatusCode == (int)HttpStatusCode.NoContent)
                {
                    newSubscriptionSet.ETag = result.Etag;
                }
            }

            return newSubscriptionSet;
        }
    }
}
