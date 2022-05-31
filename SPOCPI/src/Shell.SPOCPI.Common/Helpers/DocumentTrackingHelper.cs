// -----------------------------------------------------------------------
// <copyright file="DocumentTrackingHelper.cs" company="Microsoft Corporation">
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
    using System.Globalization;
    using System.Threading.Tasks;

    /// <summary>
    /// The Document Tracking helper class.
    /// </summary>
    public class DocumentTrackingHelper
    {
        /// <summary>
        /// The logger component instance.
        /// </summary>
        private readonly ILoggerComponent logger;

        /// <summary>
        /// The configuration instance.
        /// </summary>
        private readonly IConfiguration config;

        /// <summary>
        /// The table instance.
        /// </summary>
        private readonly StorageTableContext storageTableContext;

        /// <summary>
        /// The search update key - used to perform read and write operations to search index.
        /// </summary>
        private readonly string searchUpdateKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentTrackingHelper"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="config">The configuration.</param>
        public DocumentTrackingHelper(ILoggerComponent logger, IConfiguration config)
        {
            this.logger = logger;
            this.config = config;
            this.storageTableContext = new StorageTableContext(
                KeyVaultHelper.GetSecret(config?.GetConfigValue(Resource.ConfigCPDriveDeltaStoreConnString))?.Result?.ToString(),
                config?.GetConfigValue(Resource.ConfigDocumentTrackingTableName));

            this.searchUpdateKey = KeyVaultHelper.GetSecret(this.config?.GetConfigValue(Common.Resource.ConfigSearchUpdateKey))?.Result?.ToString();
        }

        /// <summary>
        /// DeleteEntitiesFromDocumentTracking is used to delete entities from Document Tracking azure table based on the drive Id.
        /// </summary>
        /// <param name="partitionKey">The drive Id as partition key.</param>
        /// <returns>The boolean task object.</returns>
        public async Task<bool> DeleteEntities(string partitionKey)
        {
            var isEntitiesDeleted = false;

            var driveIdBytes = System.Text.Encoding.UTF8.GetBytes(partitionKey);

            var deleteStatusFilter = StorageTableHelper.CreateFilterQueryString(Constants.PartitionKey, Constants.SmallEqual, Convert.ToBase64String(driveIdBytes));

            var itemsToBeDeleted = await StorageTableHelper.GetTableResultBasedOnQueryFilter<DriveItemEntity>(this.storageTableContext.Table, deleteStatusFilter).ConfigureAwait(false);

            if (itemsToBeDeleted?.Count > 0)
            {
                isEntitiesDeleted = await StorageTableHelper.BatchDelete<DriveItemEntity>(this.storageTableContext.Table, itemsToBeDeleted).ConfigureAwait(false);
            }

            return isEntitiesDeleted;
        }

        /// <summary>
        /// Deletes the filtered entities.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>The boolean task object.</returns>
        public async Task<bool> DeleteFilteredEntities(string filter)
        {
            var isEntitiesDeleted = false;
            var itemsToBeDeleted = await StorageTableHelper.GetTableResultBasedOnQueryFilter<DriveItemEntity>(this.storageTableContext.Table, filter).ConfigureAwait(false);

            if (itemsToBeDeleted?.Count > 0)
            {
                isEntitiesDeleted = await StorageTableHelper.BatchDelete<DriveItemEntity>(this.storageTableContext.Table, itemsToBeDeleted).ConfigureAwait(false);
            }

            return isEntitiesDeleted;
        }

        /// <summary>
        /// DeleteDocumentTrackingData is used to delete entities from Document Tracking table.
        /// </summary>
        /// <param name="entity">The SubscriptionEntity object.</param>
        /// <returns>The Task object.</returns>
        public async Task DeleteDocumentTrackingData(SubscriptionEntity entity)
        {
            //// Delete items from Tracking table
            if (entity != null)
            {
                try
                {
                    var deleteTrackingTableEntity = await this.DeleteEntities(entity.DriveId).ConfigureAwait(false);

                    if (deleteTrackingTableEntity)
                    {
                        //// Delete items from Search Index (Azure Table Index)
                        var driveIdEncoded = System.Text.Encoding.UTF8.GetBytes(entity.DriveId);
                        var deleteTrackingSearchItems = await SearchHelperClient.DeleteDocumentsFromIndex(
                            this.config.GetConfigValue(Resource.ConfigSearchServiceName),
                            this.config.GetConfigValue(Resource.ConfigTrackingIndexName),
                            this.searchUpdateKey,
                            Constants.PartitionKey,
                            Convert.ToBase64String(driveIdEncoded)).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    //// update subscription table with error message
                    if (string.IsNullOrWhiteSpace(entity.Error))
                    {
                        entity.Error = string.Format(CultureInfo.InvariantCulture, Resource.DeleteDocumetErrorMessage, ex.Message);
                    }
                    else
                    {
                        entity.Error += string.Format(CultureInfo.InvariantCulture, Resource.DeleteDocumetErrorMessage, ex.Message);
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// Deletes the document tracking data.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="driveDelta">The drive delta.</param>
        /// <returns>Boolean indicating success or failure.</returns>
        public async Task<bool> DeleteDocumentTrackingData(SubscriptionEntity entity, DriveDeltaEntity driveDelta)
        {
            bool success = false;
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (driveDelta is null)
            {
                throw new ArgumentNullException(nameof(driveDelta));
            }

            //// Delete items from Tracking table
            try
            {
                var driveIdBytes = System.Text.Encoding.UTF8.GetBytes(entity.DriveId);
                var deleteStatusFilter = StorageTableHelper.CreateFilterQueryString(Constants.PartitionKey, Constants.SmallEqual, Convert.ToBase64String(driveIdBytes));
                var timeStampFilter = StorageTableHelper.CreateFilterQueryDate(Constants.Timestamp, Constants.SmallGreaterThanOrEqual, driveDelta.ReceivedTime);
                string filter = StorageTableHelper.CombineQueryFilters(deleteStatusFilter, Constants.AndToLower, timeStampFilter);
                var deleteTrackingTableEntity = await this.DeleteFilteredEntities(filter).ConfigureAwait(false);

                if (deleteTrackingTableEntity)
                {
                    var searchFilter = $"{Constants.Timestamp} {Constants.GreaterThanShort} {driveDelta.ReceivedTime.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)} {Constants.AndToLower} {Constants.PartitionKey} {Constants.EqualShort} '{Convert.ToBase64String(driveIdBytes)}'";
                    //// Delete items from Search Index (Azure Table Index)
                    success = await SearchHelperClient.DeleteDocumentsFromIndex(
                        this.config.GetConfigValue(Resource.ConfigSearchServiceName),
                        this.config.GetConfigValue(Resource.ConfigTrackingIndexName),
                        this.searchUpdateKey,
                        searchFilter,
                        Constants.PartitionKey,
                        Constants.Timestamp).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                //// update subscription table with error message
                if (string.IsNullOrWhiteSpace(entity.Error))
                {
                    entity.Error = string.Format(CultureInfo.InvariantCulture, Resource.DeleteDocumetErrorMessage, ex.Message);
                }
                else
                {
                    entity.Error += string.Format(CultureInfo.InvariantCulture, Resource.DeleteDocumetErrorMessage, ex.Message);
                }

                throw;
            }

            return success;
        }

        /// <summary>
        /// Runs the indexer.
        /// </summary>
        /// <param name="indexerName">Name of the indexer.</param>
        /// <returns>The Task object.</returns>
        /// <exception cref="ArgumentNullException">The indexerName.</exception>
        private async Task RunIndexer(string indexerName)
        {
            if (string.IsNullOrWhiteSpace(indexerName))
            {
                throw new ArgumentNullException(nameof(indexerName));
            }

            try
            {
                await SearchHelperClient.RunSearchIndexer(
                    this.config.GetConfigValue(Common.Resource.ConfigSearchServiceName),
                    this.searchUpdateKey,
                    this.config.GetConfigValue(indexerName)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}