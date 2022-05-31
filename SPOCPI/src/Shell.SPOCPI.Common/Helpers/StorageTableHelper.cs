// -----------------------------------------------------------------------
// <copyright file="StorageTableHelper.cs" company="Microsoft Corporation">
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
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Newtonsoft.Json;

    /// <summary>
    /// Storage table helper class.
    /// </summary>
    public static class StorageTableHelper
    {
        /// <summary>
        /// Gets the table result.
        /// </summary>
        /// <typeparam name="T">Generic Parameter.</typeparam>
        /// <param name="cloudTable">The cloud table.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="rowKey">The row key.</param>
        /// <returns>Table Result.</returns>
        public static async Task<T> GetTableResult<T>(CloudTable cloudTable, string partitionKey, string rowKey)
            where T : TableEntity
        {
            TableOperation tableOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            TableResult tableResult = new TableResult();
            if (cloudTable != null)
            {
                tableResult = await cloudTable.ExecuteAsync(tableOperation).ConfigureAwait(false);
            }

            return (T)tableResult.Result;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]

        /// <summary>
        /// Gets the table result from cache first, then in table.
        /// </summary>
        /// <typeparam name="T">Generic Parameter.</typeparam>
        /// <param name="cloudTable">The cloud table.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="rowKey">The row key.</param>
        /// <param name="isCacheEnabled">Indicates whether data needs to be added to cache</param>
        /// <returns>Table Result.</returns>
        public static async Task<T> GetTableResult<T>(CloudTable cloudTable, string partitionKey, string rowKey, bool isCacheEnabled)
            where T : TableEntity
        {
            T tableResult = null;
            string cacheKey = $"{partitionKey}-{rowKey}";
            string driveItemFromCache = string.Empty;

            if (isCacheEnabled)
            {
                try
                {
                    CacheManager.GetCacheEntry(Constants.SPOCPI, cacheKey, out driveItemFromCache);
                }
                catch
                {
                    //// Suppress the issues while trying to read data from Redis cache. Fall back to Storage table
                }

                if (!string.IsNullOrEmpty(driveItemFromCache))
                {
                    tableResult = JsonConvert.DeserializeObject<T>(driveItemFromCache);
                }
            }

            if (string.IsNullOrEmpty(driveItemFromCache) || !isCacheEnabled)
            {
                // Get item from Storage Table and add the item to Cache
                tableResult = await GetTableResult<T>(cloudTable, partitionKey, rowKey).ConfigureAwait(false);
            }

            return tableResult;
        }

        /// <summary>
        /// Updates the item.
        /// </summary>
        /// <param name="cloudTable">The cloud table.</param>
        /// <param name="tableEntity">The table entity.</param>
        /// <returns>The table result.</returns>
        public static async Task<TableResult> UpdateItem(CloudTable cloudTable, TableEntity tableEntity)
        {
            TableOperation updateOperation = TableOperation.Replace(tableEntity);
            TableResult tableResult = new TableResult();
            if (cloudTable != null)
            {
                tableResult = await cloudTable.ExecuteAsync(updateOperation).ConfigureAwait(false);
            }

            return tableResult;
        }

        /// <summary>
        /// Adds the table item.
        /// </summary>
        /// <param name="cloudTable">The cloud table.</param>
        /// <param name="tableEntity">The table entity.</param>
        /// <returns>The table result object.</returns>
        public static async Task<TableResult> AddTableItem(CloudTable cloudTable, TableEntity tableEntity)
        {
            TableOperation insertOperation = TableOperation.Insert(tableEntity);
            TableResult tableResult = new TableResult();
            if (cloudTable != null)
            {
                tableResult = await cloudTable.ExecuteAsync(insertOperation).ConfigureAwait(false);
            }

            return tableResult;
        }

        /// <summary>
        /// Adds the or replace table item.
        /// </summary>
        /// <param name="cloudTable">The cloud table.</param>
        /// <param name="tableEntity">The table entity.</param>
        /// <param name="categoryName">The category name.</param>
        /// <param name="addToCache">Indicates whether data needs to be added to cache.</param>
        /// <returns>Added entity.</returns>
        public static async Task<TableResult> AddOrReplaceTableItem(CloudTable cloudTable, TableEntity tableEntity, string categoryName, bool addToCache = true)
        {
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(tableEntity);
            TableResult tableResult = new TableResult();
            if (cloudTable != null)
            {
                tableResult = await cloudTable.ExecuteAsync(insertOrReplaceOperation).ConfigureAwait(false);
            }

            try
            {
                if (addToCache && tableResult != null && tableEntity != null)
                {
                    CacheManager.AddCacheEntry(Constants.SPOCPI, $"{categoryName}-{tableEntity.PartitionKey}-{tableEntity.RowKey}", tableEntity);
                }
            }
            catch
            {
                //// Suppress the issues while trying to add data from Redis cache.
            }

            return tableResult;
        }

        /// <summary>
        /// Adds or merges the table item.
        /// </summary>
        /// <param name="cloudTable">The cloud table.</param>
        /// <param name="tableEntity">The table entity.</param>
        /// <returns>
        /// Added entity.
        /// </returns>
        public static async Task<TableResult> AddOrMergeAsync(CloudTable cloudTable, TableEntity tableEntity)
        {
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(tableEntity);
            TableResult tableResult = null;
            if (cloudTable != null)
            {
                tableResult = await cloudTable.ExecuteAsync(insertOrMergeOperation).ConfigureAwait(false);
            }

            return tableResult;
        }

        /// <summary>
        /// Deletes the subscription.
        /// </summary>
        /// <param name="cloudTable">The cloud table.</param>
        /// <param name="tableEntity">The table entity.</param>
        /// <returns>Deleted entity.</returns>
        public static async Task<TableResult> DeleteSubscription(CloudTable cloudTable, TableEntity tableEntity)
        {
            var entity = new DynamicTableEntity(tableEntity?.PartitionKey, tableEntity?.RowKey);
            entity.ETag = Constants.WildCardSearch;
            entity.Properties.Add(Constants.Status, new EntityProperty(Constants.Delete));
            TableOperation mergeOperation = TableOperation.Merge(entity);
            TableResult tableResult = new TableResult();
            if (cloudTable != null)
            {
                tableResult = await cloudTable.ExecuteAsync(mergeOperation).ConfigureAwait(false);
            }

            return tableResult;
        }

        /// <summary>
        /// Modifies the entity.
        /// </summary>
        /// <param name="cloudTable">The cloud table.</param>
        /// <param name="tableEntity">The table entity.</param>
        /// <param name="properties">The properties.</param>
        /// <returns>Modified entity.</returns>
        public static async Task<TableResult> ModifyEntity(CloudTable cloudTable, TableEntity tableEntity, Dictionary<string, string> properties)
        {
            TableResult tableResult = null;

            if (properties != null && properties.Count > 0)
            {
                var entity = new DynamicTableEntity(tableEntity?.PartitionKey, tableEntity?.RowKey);
                entity.ETag = Constants.WildCardSearch;
                foreach (var prop in properties)
                {
                    entity.Properties.Add(prop.Key, new EntityProperty(prop.Value));
                }

                TableOperation mergeOperation = TableOperation.Merge(entity);
                tableResult = new TableResult();
                if (cloudTable != null)
                {
                    tableResult = await cloudTable.ExecuteAsync(mergeOperation).ConfigureAwait(false);
                }
            }

            return tableResult;
        }

        /// <summary>
        /// Modifies the entity with any boolean properties.
        /// </summary>
        /// <param name="cloudTable">The cloud table.</param>
        /// <param name="tableEntity">The table entity.</param>
        /// <param name="properties">The properties.</param>
        /// <returns>Modified entity.</returns>
        public static async Task<TableResult> ModifyEntity(CloudTable cloudTable, TableEntity tableEntity, Dictionary<string, bool> properties)
        {
            TableResult tableResult = null;

            if (properties != null && properties.Count > 0)
            {
                var entity = new DynamicTableEntity(tableEntity?.PartitionKey, tableEntity?.RowKey);
                entity.ETag = Constants.WildCardSearch;
                foreach (var prop in properties)
                {
                    entity.Properties.Add(prop.Key, new EntityProperty(prop.Value));
                }

                TableOperation mergeOperation = TableOperation.Merge(entity);
                tableResult = new TableResult();
                if (cloudTable != null)
                {
                    tableResult = await cloudTable.ExecuteAsync(mergeOperation).ConfigureAwait(false);
                }
            }

            return tableResult;
        }

        /// <summary>
        /// Adds the or replace multiple table items.
        /// </summary>
        /// <param name="cloudTable">The cloud table.</param>
        /// <param name="tableEntities">The table entities.</param>
        /// <returns>Table Result.</returns>
        public static async Task<IList<TableResult>> AddOrReplaceMultipleTableItems(CloudTable cloudTable, TableEntity[] tableEntities)
        {
            IList<TableResult> tableResult = null;

            if (tableEntities != null && tableEntities.Length > 0)
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (var tableEntity in tableEntities)
                {
                    batchOperation.InsertOrMerge(tableEntity);
                }

                if (cloudTable != null)
                {
                    tableResult = await cloudTable.ExecuteBatchAsync(batchOperation).ConfigureAwait(false);
                }
            }

            return tableResult;
        }

        /// <summary>
        /// Deletes the table item.
        /// </summary>
        /// <param name="cloudTable">The cloud table.</param>
        /// <param name="tableEntity">The table entity.</param>
        /// <returns>Table Result.</returns>
        public static async Task<TableResult> DeleteTableItem(CloudTable cloudTable, TableEntity tableEntity)
        {
            TableOperation deleteOperation = TableOperation.Delete(tableEntity);
            TableResult tableResult = new TableResult();
            if (cloudTable != null)
            {
                tableResult = await cloudTable.ExecuteAsync(deleteOperation).ConfigureAwait(false);
            }

            return tableResult;
        }

        /// <summary>
        /// GetTableResultBasedOnQueryFilter is used to get the List of entities based on the filter condition provided.
        /// </summary>
        /// <typeparam name="T">Generic entity object.</typeparam>
        /// <param name="cloudTable">The CloudTable object.</param>
        /// <param name="filter">The table filter condition as string.</param>
        /// <returns>List of entities.</returns>
        public static async Task<List<T>> GetTableResultBasedOnQueryFilter<T>(CloudTable cloudTable, string filter)
            where T : TableEntity, new()
        {
            List<T> entities = new List<T>();
            var query = new TableQuery<T>().Where(filter);
            TableContinuationToken continuationToken = null;
            do
            {
                TableQuerySegment<T> tableQuerySegment = null;
                if (cloudTable != null)
                {
                    tableQuerySegment = await cloudTable.ExecuteQuerySegmentedAsync<T>(query, continuationToken).ConfigureAwait(false);
                }

                if (tableQuerySegment != null && tableQuerySegment.Results != null)
                {
                    foreach (var entity in tableQuerySegment.Results)
                    {
                        entities.Add(entity);
                    }
                }

                continuationToken = tableQuerySegment?.ContinuationToken;
            }
            while (continuationToken != null);

            return entities;
        }

        /// <summary>
        /// GetTableResultBasedOnQueryFilter is used to get the List of entities based on the filter condition provided.
        /// </summary>
        /// <typeparam name="T">Generic entity object.</typeparam>
        /// <param name="cloudTable">The CloudTable object.</param>
        /// <param name="topCount">The top count.</param>
        /// <param name="filter">The table filter condition as string.</param>
        /// <returns>
        /// List of entities.
        /// </returns>
        public static async Task<List<T>> GetTopTableResultBasedOnQueryFilter<T>(CloudTable cloudTable, int topCount = 1, string filter = null)
            where T : TableEntity, new()
        {
            List<T> entities = null;
            TableQuery<T> query;
            int count = 0;

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = new TableQuery<T>().Where(filter).Take(topCount);
            }
            else
            {
                query = new TableQuery<T>().Take(topCount);
            }

            TableContinuationToken continuationToken = null;
            entities = new List<T>();
            do
            {
                TableQuerySegment<T> tableQuerySegment = null;

                if (cloudTable != null)
                {
                    tableQuerySegment = await cloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                }

                if (tableQuerySegment != null && tableQuerySegment.Results != null)
                {
                    foreach (var entity in tableQuerySegment.Results)
                    {
                        entities.Add(entity);
                        count++;
                        if (count == topCount)
                        {
                            break;
                        }
                    }
                }

                if (count == topCount)
                {
                    break;
                }

                continuationToken = tableQuerySegment?.ContinuationToken;
            }
            while (continuationToken != null);

            return entities;
        }

        /// <summary>
        /// DeleteTableItem is used to delete an entity from the Azure Table.
        /// </summary>
        /// <typeparam name="T">Generic entity object.</typeparam>
        /// <param name="cloudTable">The CloudTable object.</param>
        /// <param name="entity">The TableEntity object.</param>
        /// <returns>The TaskResult object.</returns>
        public static async Task<TableResult> DeleteTableItem<T>(CloudTable cloudTable, TableEntity entity)
            where T : TableEntity
        {
            TableResult tableResult = null;
            if (entity != null)
            {
                entity.ETag = Constants.WildCardSearch;
                TableOperation delete = TableOperation.Delete(entity);
                tableResult = new TableResult();
                if (cloudTable != null)
                {
                    tableResult = await cloudTable.ExecuteAsync(delete).ConfigureAwait(false);
                }
            }

            return tableResult;
        }

        [SuppressMessage("Microsoft.Design", "CA1054:UriPropertiesShouldNotBeStrings", Justification = "Reviewed.")]

        /// <summary>
        /// Checks the duplicate subscription.
        /// </summary>
        /// <param name="cloudTable">The table.</param>
        /// <param name="libraryUrl">The library URL.</param>
        /// <returns>True if duplicate subscription is found and vice versa.</returns>
        public static async Task<bool> CheckDuplicateSubscription(CloudTable cloudTable, string libraryUrl)
        {
            var query = new TableQuery<SubscriptionEntity>().Where(
                TableQuery.GenerateFilterCondition(Resource.LibraryUrl, QueryComparisons.Equal, libraryUrl));

            TableQuerySegment<SubscriptionEntity> queryResult = null;
            if (cloudTable != null)
            {
                queryResult = await cloudTable.ExecuteQuerySegmentedAsync(query, null).ConfigureAwait(false);
            }

            return queryResult?.Results.Count > 0;
        }

        /// <summary>
        /// Determines whether [is subscription deleted] [the specified table].
        /// </summary>
        /// <param name="cloudTable">The table.</param>
        /// <param name="subscription">The subscription.</param>
        /// <returns>True if the Subscription is deleted and vice versa.</returns>
        public static async Task<bool> IsSubscriptionDeleted(CloudTable cloudTable, SubscriptionEntity subscription)
        {
            var query = new TableQuery<SubscriptionEntity>().Where(
            TableQuery.CombineFilters(
            TableQuery.GenerateFilterCondition(Constants.RowKey, QueryComparisons.Equal, subscription?.RowKey),
            TableOperators.And,
            TableQuery.GenerateFilterCondition(Constants.PartitionKey, QueryComparisons.Equal, subscription?.PartitionKey)));

            TableQuerySegment<SubscriptionEntity> queryResult = null;

            if (cloudTable != null)
            {
                queryResult = await cloudTable.ExecuteQuerySegmentedAsync(query, null).ConfigureAwait(true);
            }

            if (queryResult?.Results.Count > 0)
            {
                return queryResult.Results[0].Status == Constants.Delete;
            }
            else
            {
                throw new StorageException(Resource.ItemNotFound);
            }
        }

        /// <summary>
        /// Determines whether [is subscription disabled] [the specified table].
        /// </summary>
        /// <param name="cloudTable">The table.</param>
        /// <param name="subscription">The subscription.</param>
        /// <returns>True if the Subscription is disabled and vice versa.</returns>
        /// <exception cref="StorageException">The requested table item was not found, please wait for the azure index to update, approximate 5 minutes.</exception>
        public static async Task<bool> IsSubscriptionDisabled(CloudTable cloudTable, SubscriptionEntity subscription)
        {
            var query = new TableQuery<SubscriptionEntity>().Where(
            TableQuery.CombineFilters(
            TableQuery.GenerateFilterCondition(Constants.RowKey, QueryComparisons.Equal, subscription?.RowKey),
            TableOperators.And,
            TableQuery.GenerateFilterCondition(Constants.PartitionKey, QueryComparisons.Equal, subscription?.PartitionKey)));

            TableQuerySegment<SubscriptionEntity> queryResult = null;

            if (cloudTable != null)
            {
                queryResult = await cloudTable.ExecuteQuerySegmentedAsync(query, null).ConfigureAwait(true);
            }

            if (queryResult?.Results.Count > 0)
            {
                return queryResult.Results[0].Status == subscription?.Status;
            }
            else
            {
                throw new StorageException(Resource.ItemNotFound);
            }
        }

        /// <summary>
        /// Creates the table entity.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>Cloud table Client.</returns>
        public static CloudTable CreateTableEntity(string connectionString, string tableName)
        {
            CloudTable cloudTable = null;
            if (!string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(tableName))
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                cloudTable = tableClient.GetTableReference(tableName);
            }

            return cloudTable;
        }

        /// <summary>
        /// CreateFilterQueryDate is used to create TableQuery using the GenerateFilterConditionForDate method.
        /// </summary>
        /// <param name="variable">The string object on which to fire query.</param>
        /// <param name="queryComparisonOperator">The QueryComparisons operator in upper characters as string.</param>
        /// <param name="value">The value for the variable object.</param>
        /// <returns>The filter as a string.</returns>
        public static string CreateFilterQueryDate(string variable, string queryComparisonOperator, DateTimeOffset value)
        {
            var filter = string.Empty;
            if (!string.IsNullOrWhiteSpace(queryComparisonOperator))
            {
                //// switch can be extended further by adding additional case statements
                switch (queryComparisonOperator.ToUpperInvariant())
                {
                    case Constants.GreaterThanOrEqual:
                        filter = TableQuery.GenerateFilterConditionForDate(variable, QueryComparisons.GreaterThanOrEqual, value);
                        break;

                    case Constants.LessThan:
                        filter = TableQuery.GenerateFilterConditionForDate(variable, QueryComparisons.LessThan, value);
                        break;

                    case Constants.Equal:
                        filter = TableQuery.GenerateFilterConditionForDate(variable, QueryComparisons.Equal, value);
                        break;

                    default:
                        break;
                }
            }

            return filter;
        }

        /// <summary>
        /// CreateFilterQueryString is used to create TableQuery using the GenerateFilterCondition method.
        /// </summary>
        /// <param name="variable">The string object on which to fire query.</param>
        /// <param name="queryComparisonOperator">The QueryComparisons operator in upper characters as string.</param>
        /// <param name="value">The value for the variable object.</param>
        /// <returns>The filter as a string.</returns>
        public static string CreateFilterQueryString(string variable, string queryComparisonOperator, string value)
        {
            var filter = string.Empty;
            if (!string.IsNullOrWhiteSpace(queryComparisonOperator))
            {
                // switch can be extended further by adding additional case statements
                switch (queryComparisonOperator.ToUpperInvariant())
                {
                    case Constants.Equal:
                        filter = TableQuery.GenerateFilterCondition(variable, QueryComparisons.Equal, value);
                        break;

                    default:
                        break;
                }
            }

            return filter;
        }

        /// <summary>
        /// CreateFilterQueryBoolean is used to create TableQuery using the GenerateFilterConditionForBool method.
        /// </summary>
        /// <param name="variable">The string object on which to fire query.</param>
        /// <param name="queryComparisonOperator">The QueryComparisons operator in upper characters as string.</param>
        /// <param name="value">The value for the variable boolean object.</param>
        /// <returns>The filter as a string.</returns>
        public static string CreateFilterQueryBoolean(string variable, string queryComparisonOperator, bool value)
        {
            var filter = string.Empty;
            if (!string.IsNullOrWhiteSpace(queryComparisonOperator))
            {
                // switch can be extended further by adding additional case statements
                switch (queryComparisonOperator.ToUpperInvariant())
                {
                    case Constants.Equal:
                        filter = TableQuery.GenerateFilterConditionForBool(variable, QueryComparisons.Equal, value);
                        break;

                    default:
                        break;
                }
            }

            return filter;
        }

        /// <summary>
        /// GetTableEntity is used to get an Azure Table based on the storage connection string and table name.
        /// </summary>
        /// <param name="connectionString">the storage connection string as string.</param>
        /// <param name="tableName">The table name as string.</param>
        /// <returns>The CloudTable object.</returns>
        public static CloudTable GetTableEntity(string connectionString, string tableName)
        {
            CloudTable table = null;
            if (!string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(tableName))
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                table = tableClient.GetTableReference(tableName);
            }

            return table;
        }

        /// <summary>
        /// GetTableEntity is used to get an Azure Table based on the storage connection string and table name.
        /// </summary>
        /// <param name="storageAccount">The storage account.</param>
        /// <param name="tableName">The table name as string.</param>
        /// <returns>
        /// The CloudTable object.
        /// </returns>
        public static CloudTable GetTableEntity(CloudStorageAccount storageAccount, string tableName)
        {
            CloudTable table = null;
            if (storageAccount != null && !string.IsNullOrWhiteSpace(tableName))
            {
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                table = tableClient.GetTableReference(tableName);
            }

            return table;
        }

        /// <summary>
        /// Gets the storage account.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The cloud storage account instance.</returns>
        public static CloudStorageAccount GetStorageAccount(string connectionString)
        {
            CloudStorageAccount storageAccount = null;
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                storageAccount = CloudStorageAccount.Parse(connectionString);
            }

            return storageAccount;
        }

        /// <summary>
        /// CombineQueryFilters is used to combine two TableQuery filter objects as string.
        /// </summary>
        /// <param name="firstFilter">The first TableQuery filter object as string.</param>
        /// <param name="tableOperator">The TableOperator in small characters as string.</param>
        /// <param name="secondFilter">The second TableQuery filter object as string.</param>
        /// <returns>The final filter as a string.</returns>
        public static string CombineQueryFilters(string firstFilter, string tableOperator, string secondFilter)
        {
            var combinedQueryFilter = string.Empty;
            if (!string.IsNullOrWhiteSpace(tableOperator))
            {
                switch (tableOperator.Trim().ToUpperInvariant())
                {
                    case Constants.And:
                        combinedQueryFilter = TableQuery.CombineFilters(firstFilter, TableOperators.And, secondFilter);
                        break;

                    case Constants.Or:
                        combinedQueryFilter = TableQuery.CombineFilters(firstFilter, TableOperators.Or, secondFilter);
                        break;

                    default:
                        break;
                }
            }

            return combinedQueryFilter;
        }

        /// <summary>
        /// Gets Base64 string version of original string.
        /// </summary>
        /// <param name="originalString">Actual string.</param>
        /// <returns>Base64 version.</returns>
        public static string GetBase64String(string originalString)
        {
            string base64String = string.Empty;
            if (!string.IsNullOrEmpty(originalString))
            {
                var driveIdBytes = System.Text.Encoding.UTF8.GetBytes(originalString);
                base64String = Convert.ToBase64String(driveIdBytes);
            }

            return base64String;
        }

        /// <summary>
        /// This method is used to batch entities in a number of 100 items.
        /// </summary>
        /// <typeparam name="T">Generic entity object.</typeparam>
        /// <param name="table">The CloudTable object.</param>
        /// <param name="entities">The TableEntity object collection.</param>
        /// <returns>Returns a boolean task object.</returns>
        public static async Task<bool> BatchEntities<T>(CloudTable table, List<T> entities)
            where T : TableEntity
        {
            var isSuccess = true;
            var maximumBatchSize = 100;
            if (table != null && entities != null && entities.Count > 0)
            {
                for (int i = 0; i < entities.Count; i += maximumBatchSize)
                {
                    List<T> batchEntities = new List<T>();
                    var batchItems = entities.Skip(i).Take(maximumBatchSize).ToList();
                    batchItems.ForEach(item => batchEntities.Add(item));
                    await DeleteEntitiesInBatch(table, batchEntities).ConfigureAwait(false);
                }
            }

            return isSuccess;
        }

        /// <summary>
        /// This method is used to delete items from the Azure Storage table as a batch of 100 items.
        /// </summary>
        /// <typeparam name="T">Generic entity object.</typeparam>
        /// <param name="table">The CloudTable object.</param>
        /// <param name="entitiesToDelete">The TableEntity object collection.</param>
        /// <returns>Returns a boolean task object.</returns>
        public static async Task<bool> DeleteEntitiesInBatch<T>(CloudTable table, List<T> entitiesToDelete)
            where T : TableEntity
        {
            var isSuccess = false;
            TableBatchOperation tableBatchOperation = new TableBatchOperation();

            if (table != null && entitiesToDelete != null && entitiesToDelete.Count > 0)
            {
                entitiesToDelete.ForEach(entity => tableBatchOperation.Add(TableOperation.Delete(entity)));
                await table.ExecuteBatchAsync(tableBatchOperation).ConfigureAwait(false);
                isSuccess = true;
            }

            return isSuccess;
        }

        /// <summary>
        /// This method is used to batch delete azure table storage items.
        /// </summary>
        /// <typeparam name="T">The Generic type parameter.</typeparam>
        /// <param name="tableContext">The CloudTable object.</param>
        /// <param name="entities">List of items to delete.</param>
        /// <returns>boolean indicating operation success or failure.</returns>
        public static async Task<bool> BatchDelete<T>(CloudTable tableContext, List<T> entities)
            where T : TableEntity
        {
            if (tableContext is null)
            {
                throw new ArgumentNullException(nameof(tableContext));
            }

            if (entities is null || entities.Count == 0)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            for (var i = 0; i < entities.Count; i += Constants.TableServiceBatchMaximumOperations)
            {
                var batchItems = entities.Skip(i)
                                         .Take(Constants.TableServiceBatchMaximumOperations)
                                         .ToList();

                var batch = new TableBatchOperation();
                foreach (var item in batchItems)
                {
                    item.ETag = Constants.WildCardSearch;
                    batch.Delete(item);
                }

                try
                {
                    await tableContext.ExecuteBatchAsync(batch).ConfigureAwait(false);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }
    }
}