// -----------------------------------------------------------------------
// <copyright file="PopulateTracking.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------

namespace Shell.SPOCPI.PopulateTracking.FunctionApp
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Graph;
    using Newtonsoft.Json;
    using Shell.SPOCPI.Common;
    using Constants = Shell.SPOCPI.Common.Constants;

    /// <summary>
    /// Populate Tracking class - Responsible for populating document metadata into Tracking table.
    /// </summary>
    public class PopulateTracking
    {
        /// <summary>
        /// The logger component instance.
        /// </summary>
        private readonly ILoggerComponent loggerComponent;

        /// <summary>
        /// The configuration instance.
        /// </summary>
        private readonly IConfiguration config;

        /// <summary>
        /// The search update key - used to perform read and write operations to search index.
        /// </summary>
        private readonly string searchUpdateKey;

        /// <summary>
        /// The Subscription storage table context.
        /// </summary>
        private readonly StorageTableContext subscriptionTableContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PopulateTracking"/> class.
        /// </summary>
        /// <param name="loggerComponent">logger instance.</param>
        /// <param name="config">configuration instance.</param>
        public PopulateTracking(ILoggerComponent loggerComponent, IConfiguration config)
        {
            this.loggerComponent = loggerComponent ?? throw new ArgumentNullException(nameof(loggerComponent));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.searchUpdateKey = KeyVaultHelper.GetSecret(this.config?.GetConfigValue(Common.Resource.ConfigSearchUpdateKey))?.Result?.ToString();
            this.subscriptionTableContext = new StorageTableContext(
                KeyVaultHelper.GetSecret(config?.GetConfigValue(Common.Resource.ConfigWebHooksStoreConnectionString))?.Result?.ToString(),
                Constants.SubscriptionsTableName);
        }

        /// <summary>
        /// Responsible for populating document metadata into Tracking table.
        /// </summary>
        /// <param name="changeProcessOutputMessage">Message from Change Process Output Queue Service Bus.</param>
        /// <param name="docTrackingTable">Document tracking storage table.</param>
        /// <param name="queueBinder">Responsible for binding queues dynamically.</param>
        /// <returns>Task status.</returns>
        [FunctionName(Constants.PopulateTrackingFunctionName)]
        public async Task Run(
            [ServiceBusTrigger(Constants.ChangeProcessOutputQueue, Connection = Constants.ChangeProcessorServiceBusConnectionName)] string changeProcessOutputMessage,
            [Table(Constants.DocumentTrackingTableName)] CloudTable docTrackingTable,
            Binder queueBinder)
        {
            List<DocumentEntity> documentEntities = new List<DocumentEntity>();
            try
            {
                var dict = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(changeProcessOutputMessage);
                object obj;
                if (dict.Count > 0)
                {
                    if (!dict[0].TryGetValue(nameof(TableEntity.PartitionKey), out obj))
                    {
                        List<DriveItem> driveItems = JsonConvert.DeserializeObject<List<DriveItem>>(changeProcessOutputMessage);
                        this.loggerComponent.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.CPItemsReceived, driveItems.Count), Constants.SPOCPI, nameof(LogCategory.PopulateTrackingFunctionApp));
                        IList<SubscriptionEntity> subscriptionList = SubscriptionHelper.GetOrUpdateSubscriptionQueues(false).Result;
                        List<SubscriptionEntity> subscriptionEntities = subscriptionList?.Where(a => (a.DriveId != null && a.DriveId.Equals(driveItems?[0].ParentReference?.DriveId, StringComparison.OrdinalIgnoreCase))).ToList();
                        string folderPath = string.Empty;
                        if (subscriptionEntities.Any())
                        {
                            SubscriptionEntity subscriptionEntity = subscriptionEntities?[0];
                            folderPath = subscriptionEntity?.IncludeFolderRelativePath;
                        }

                        string libraryUrl = subscriptionEntities?[0].LibraryUrl;
                        foreach (DriveItem driveItem in driveItems)
                        {
                            DocumentEntity documentEntity = null;
                            string parentFolderUrl = string.Empty;
                            if (driveItem.ParentReference.Path != null)
                            {
                                string parentPath = driveItem.ParentReference.Path.Split("root:")?[1];
                                parentFolderUrl = libraryUrl + parentPath;
                            }
                            else
                            {
                                parentFolderUrl = libraryUrl;
                            }

                            try
                            {
                                string driveItemIdBase64 = StorageTableHelper.GetBase64String(driveItem.ParentReference.DriveId);

                                if (driveItem.Deleted?.State != null)
                                {
                                    await this.DeleteDocumentTrackingEntry(docTrackingTable, driveItem, driveItemIdBase64).ConfigureAwait(true);
                                    await SearchHelperClient.DeleteDocumentsFromIndex(
                                        this.config.GetConfigValue(Common.Resource.ConfigSearchServiceName),
                                        this.config.GetConfigValue(Common.Resource.ConfigTrackingIndexName),
                                        this.searchUpdateKey,
                                        Constants.RowKey,
                                        driveItem.Id).ConfigureAwait(true);
                                    documentEntity = PopulateDocumentEntityObject(driveItem, driveItemIdBase64, false, false, parentFolderUrl);
                                }
                                else if (!string.IsNullOrEmpty(folderPath))
                                {
                                    if ((!string.IsNullOrEmpty(driveItem.ParentReference.Path)) && (driveItem.ParentReference.Path.Split("root:").Length == 2) && (!string.IsNullOrEmpty(driveItem.ParentReference.Path.Split("root:")[1])))
                                    {
                                        string parentFolderPath = driveItem.ParentReference.Path.Split("root:/")[1];

                                        string[] paths = folderPath.Split(";");
                                        foreach (string path in paths)
                                        {
                                            if ((!string.IsNullOrEmpty(path)) && (!string.IsNullOrEmpty(parentFolderPath)) && parentFolderPath.Equals(path, StringComparison.OrdinalIgnoreCase))
                                            {
                                                documentEntity = await this.AddDocumentTrackingEntry(docTrackingTable, driveItem, driveItemIdBase64, parentFolderUrl).ConfigureAwait(true);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    documentEntity = await this.AddDocumentTrackingEntry(docTrackingTable, driveItem, driveItemIdBase64, parentFolderUrl).ConfigureAwait(true);
                                }
                            }
                            catch (Exception ex)
                            {
                                this.loggerComponent.LogError(ex, string.Format(CultureInfo.InvariantCulture, Resource.DriveItemAddingFailed, driveItem.Id), Constants.SPOCPI, nameof(LogCategory.PopulateTrackingFunctionApp));
                            }

                            if (documentEntity != null)
                            {
                                documentEntities.Add(documentEntity);
                            }
                        }
                    }
                    else
                    {
                        documentEntities = JsonConvert.DeserializeObject<List<DocumentEntity>>(changeProcessOutputMessage);
                        documentEntities.ForEach(elem =>
                        {
                            elem.DocumentCTagChange = true;
                            elem.DocumentETagChange = true;
                        });
                    }
                }

                await this.OutputToQueue(documentEntities, queueBinder).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.loggerComponent.LogError(ex, Resource.GeneralException, Constants.SPOCPI, nameof(LogCategory.PopulateTrackingFunctionApp));
                throw;
            }
        }

        /// <summary>
        /// Populates Document tracking instance from Graph item.
        /// </summary>
        /// <param name="driveItem">Microsoft Graph Item instance.</param>
        /// <param name="driveItemIdBase64">Base64 data of the Drive Item Id.</param>
        /// <param name="contentchanged">Returns <c>true</c> if item's content is changed, else <c>false</c> is returned.</param>
        /// <param name="metadatachanged">Returns <c>true</c> if item's metadata is changed, else <c>false</c> is returned.</param>
        /// <param name="parentFolderUrl">Parent Folder Url.</param>
        /// <returns>Instance of <see cref="DocumentEntity"/> class.</returns>
        private static DocumentEntity PopulateDocumentEntityObject(DriveItem driveItem, string driveItemIdBase64, bool contentchanged, bool metadatachanged, string parentFolderUrl)
        {
            DocumentEntity documentEntity = new DocumentEntity
            {
                // DriveItem Fields
                PartitionKey = driveItemIdBase64,
                RowKey = driveItem.Id,
                DriveId = driveItem.ParentReference.DriveId,
                DocumentCTag = driveItem.CTag,
                DocumentCTagChange = contentchanged,
                DocumentETag = driveItem.ETag,
                DocumentETagChange = metadatachanged,
                ListId = driveItem.SharepointIds?.ListId,
                ListItemId = driveItem.SharepointIds?.ListItemId,
                ListItemUniqueId = driveItem.SharepointIds?.ListItemUniqueId,
                SiteId = driveItem.SharepointIds?.SiteId,
                SiteUrl = driveItem.SharepointIds?.SiteUrl,
                WebId = driveItem.SharepointIds?.WebId,
                WebUrl = driveItem.WebUrl,
                Name = driveItem.Name,
                Extension = Path.GetExtension(driveItem.Name),
                IsFolder = driveItem.Folder != null,
                ParentID = driveItem.ParentReference.Id,
                CreatedDateTime = driveItem.CreatedDateTime?.DateTime,
                ModifiedDateTime = driveItem.LastModifiedDateTime?.DateTime,
                FileSize = driveItem.Size,
                IsDeleted = driveItem.Deleted != null,
                ParentFolderUrl = parentFolderUrl,
            };

            return documentEntity;
        }

        /// <summary>
        /// Gets the graph client.
        /// </summary>
        /// <returns>The Graph Service Client.</returns>
        private GraphServiceClient GetGraphClient()
        {
            var tenantName = this.config.GetConfigValue(Common.Resource.ConfigTenantName);
            var clientId = KeyVaultHelper.GetSecret(this.config.GetConfigValue(Common.Resource.ConfigAADAppClientId)).Result?.ToString();
            var clientSecret = KeyVaultHelper.GetSecret(this.config.GetConfigValue(Common.Resource.ConfigAADAppClientSecret)).Result?.ToString();
            return GraphHelper.GetGraphClient(tenantName, clientId, clientSecret);
        }

        /// <summary>
        /// Adds an entry in Document Tracking table.
        /// </summary>
        /// <param name="docTrackingTable">Document tracking table.</param>
        /// <param name="driveItem">Drive Item.</param>
        /// <param name="driveItemIdBase64">Base 64 version of Drive Item Id.</param>
        /// <param name="parentFolderUrl">Parent Folder Url.</param>
        /// <returns>Populated Document Entity instance.</returns>
        private async Task<DocumentEntity> AddDocumentTrackingEntry(CloudTable docTrackingTable, DriveItem driveItem, string driveItemIdBase64, string parentFolderUrl)
        {
            // Changes Indicators
            bool contentchanged = true;
            bool metadatachanged = true;

            // Try retrieving existing DriveItem in the Document Table
            DocumentEntity readDocument = StorageTableHelper.GetTableResult<DocumentEntity>(docTrackingTable, driveItemIdBase64, driveItem.Id, isCacheEnabled: true).Result;

            // Identify the type of changes CTag / ETag
            if (readDocument?.Name.Equals(Constants.Root, StringComparison.OrdinalIgnoreCase) == false)
            {
                this.loggerComponent.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.CTagReceived, driveItem.CTag, readDocument.DocumentCTag), Constants.SPOCPI, nameof(LogCategory.PopulateTrackingFunctionApp));
                contentchanged = !driveItem.CTag.Equals(readDocument.DocumentCTag, StringComparison.OrdinalIgnoreCase);

                this.loggerComponent.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.ETagReceived, driveItem.ETag, readDocument.DocumentETag), Constants.SPOCPI, nameof(LogCategory.PopulateTrackingFunctionApp));
                metadatachanged = !driveItem.ETag.Equals(readDocument.DocumentETag, StringComparison.OrdinalIgnoreCase);
            }

            // Do not output to queue when there is no change to content and metadata
            if (!(contentchanged || metadatachanged))
            {
                this.loggerComponent.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.DriveItemSkipped, driveItem.Id), Constants.SPOCPI, nameof(LogCategory.PopulateTrackingFunctionApp));
                return null;
            }

            DocumentEntity documentEntity = PopulateDocumentEntityObject(driveItem, driveItemIdBase64, contentchanged, metadatachanged, parentFolderUrl);

            // Add the item to tracking table only if it is not system update.
            if (readDocument == null || !readDocument.ModifiedDateTime.Equals(documentEntity.ModifiedDateTime))
            {
                // Add/Update item in Tracking table.
                await StorageTableHelper.AddOrReplaceTableItem(docTrackingTable, documentEntity, Constants.DocumentTrackingTableName, addToCache: true).ConfigureAwait(false);
                this.loggerComponent.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.DriveItemAdded, driveItem.Id), Constants.SPOCPI, nameof(LogCategory.PopulateTrackingFunctionApp));
            }
            else
            {
                documentEntity = null;
                this.loggerComponent.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.DriveItemSkipped, driveItem.Id), Constants.SPOCPI, nameof(LogCategory.PopulateTrackingFunctionApp));
            }

            return documentEntity;
        }

        /// <summary>
        /// Deletes Document tracking entry from Document Tracking table.
        /// </summary>
        /// <param name="docTrackingTable">The Document tracking table.</param>
        /// <param name="driveItem">The Drive item.</param>
        /// <param name="driveItemIdBase64">Base64 data of the Drive Item Id.</param>
        /// <returns>Task status.</returns>
        private async Task DeleteDocumentTrackingEntry(CloudTable docTrackingTable, DriveItem driveItem, string driveItemIdBase64)
        {
            bool isDeleted = true;

            string filter = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(Constants.PartitionKey, QueryComparisons.Equal, driveItemIdBase64),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(Constants.RowKey, QueryComparisons.Equal, driveItem.Id));

            var results = await StorageTableHelper.GetTableResultBasedOnQueryFilter<DriveDeltaEntity>(docTrackingTable, filter).ConfigureAwait(true);

            if (results.Count > 0)
            {
                var result = await StorageTableHelper.DeleteTableItem(docTrackingTable, results[0]).ConfigureAwait(true);
                isDeleted = result.HttpStatusCode == 204;
            }

            if (isDeleted)
            {
                this.loggerComponent.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.DriveItemDeleted, driveItem.Id), Constants.SPOCPI, nameof(LogCategory.PopulateTrackingFunctionApp));
            }
            else
            {
                this.loggerComponent.LogError(new Exception(string.Format(CultureInfo.InvariantCulture, Resource.DriveItemDeletingFailed, driveItem.Id)), Constants.SPOCPI, nameof(LogCategory.PopulateTrackingFunctionApp));
            }
        }

        /// <summary>
        /// Outputs the document entities to queue.
        /// </summary>
        /// <param name="documentEntities">The Document entities.</param>
        /// <param name="queueBinder">Queue Binder instance.</param>
        /// <returns>Task status.</returns>
        private async Task OutputToQueue(List<DocumentEntity> documentEntities, Binder queueBinder)
        {
            List<List<DocumentEntity>> groupedDocumentEntities = documentEntities.GroupBy(documentEntity => documentEntity.PartitionKey).Select(group => group.ToList()).ToList();
            List<SubscriptionEntity> subscriptionEntities = SubscriptionHelper.GetOrUpdateSubscriptionQueues(false).Result;
            foreach (var groupedDocumentEntity in groupedDocumentEntities)
            {
                IEnumerable<SubscriptionEntity> filteredSubscriptionEntities = subscriptionEntities.Where(subscriptionEntity => subscriptionEntity.DriveId != null && subscriptionEntity.DriveId == groupedDocumentEntity[0].DriveId);
                if (filteredSubscriptionEntities.Any())
                {
                    SubscriptionEntity subscriptionEntity = filteredSubscriptionEntities.FirstOrDefault();
                    if (subscriptionEntity != null && !string.IsNullOrEmpty(subscriptionEntity.OutputQueue))
                    {
                        string[] outputQueues = (subscriptionEntity.IndexQueues?.Length > 0) ? subscriptionEntity.IndexQueues.Split(';') : subscriptionEntity.OutputQueue.Split(';');
                        foreach (string queueName in outputQueues)
                        {
                            documentEntities.ForEach(documentEntity =>
                            {
                                documentEntity.SubscriptionId = subscriptionEntity.SubscriptionId;
                                documentEntity.QueueName = queueName;
                                documentEntity.Parameters = subscriptionEntity.Parameters;
                            });

                            await this.AddDriveItemsToQueue(documentEntities, queueBinder, groupedDocumentEntity[0].DriveId).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    string exceptionMessage = string.Format(Resource.NoSubscriptionFound, groupedDocumentEntity[0].DriveId);
                    this.loggerComponent.LogError(new Exception(exceptionMessage), exceptionMessage, Constants.SPOCPI, nameof(LogCategory.PopulateTrackingFunctionApp));
                }
            }
        }

        /// <summary>
        /// Add document items to Output queue.
        /// </summary>
        /// <param name="documentEntities">List of Document entities.</param>
        /// <param name="queueBinder">Queue Binder instance.</param>
        /// <param name="driveId">Drive Id.</param>
        /// <returns>Task status.</returns>
        private async Task AddDriveItemsToQueue(List<DocumentEntity> documentEntities, Binder queueBinder, string driveId)
        {
            string selectedQueue = this.SelectRandomQueue();
            if (!string.IsNullOrEmpty(selectedQueue))
            {
                try
                {
                    var serviceBusQueueAttribute = new ServiceBusAttribute(selectedQueue)
                    {
                        Connection = Constants.DocumentTrackingConnectionStringName,
                    };
                    var documentEntitiesString = JsonConvert.SerializeObject(documentEntities);
                    var outputMessages = await queueBinder.BindAsync<IAsyncCollector<string>>(serviceBusQueueAttribute).ConfigureAwait(false);
                    await outputMessages.AddAsync(documentEntitiesString).ConfigureAwait(false);
                    this.loggerComponent.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.DriveItemsAddedToOutputQueue, selectedQueue, driveId), Constants.SPOCPI, nameof(LogCategory.PopulateTrackingFunctionApp));
                }
                catch (Exception ex)
                {
                    this.loggerComponent.LogError(ex, string.Format(Resource.ErrorAddingToOutputQueue, selectedQueue, driveId), Constants.SPOCPI, nameof(LogCategory.PopulateTrackingFunctionApp));
                }
            }
        }

        /// <summary>
        /// Returns a queue name/activity name.
        /// </summary>
        /// <returns>a queue name.</returns>
        private string SelectRandomQueue()
        {
            string selectedQueue = string.Empty;
            string queue1 = this.config.GetConfigValue(Constants.QueueName1);
            string queue2 = this.config.GetConfigValue(Constants.QueueName2);
            string queue3 = this.config.GetConfigValue(Constants.QueueName3);
            string queue4 = this.config.GetConfigValue(Constants.QueueName4);
            string queue5 = this.config.GetConfigValue(Constants.QueueName5);
            string[] allQueues = { queue1, queue2, queue3, queue4, queue5 };

            // Create a Random object.
            Random rand = new Random();

            // Generate a random index less than the size of the array.
            int index = 0;
            index = rand.Next(0, 4);

            selectedQueue = allQueues[index];
            return selectedQueue;
        }
    }
}