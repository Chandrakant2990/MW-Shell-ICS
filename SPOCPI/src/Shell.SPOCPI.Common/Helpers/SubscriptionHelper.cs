// -----------------------------------------------------------------------
// <copyright file="SubscriptionHelper.cs" company="Microsoft Corporation">
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
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Search.Documents.Models;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Graph;
    using Newtonsoft.Json;
    using Shell.SPOCPI.Common.Helpers;

    /// <summary>
    /// The Subscription Helper Class.
    /// </summary>
    public class SubscriptionHelper
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
        /// The client instance.
        /// </summary>
        private readonly GraphServiceClient client;

        /// <summary>
        /// The queue client instance.
        /// </summary>
        private readonly IQueueClient queueClient;

        /// <summary>
        /// The documentTrackingHelper instance.
        /// </summary>
        private readonly DocumentTrackingHelper documentTrackingHelper;

        /// <summary>
        /// The drive delta helper instance.
        /// </summary>
        private readonly DriveDeltaHelper driveDeltaHelper;

        /// <summary>
        /// The client factory.
        /// </summary>
        private readonly IHttpClientFactory clientFactory;

        /// <summary>
        /// The search update key - used to perform read and write operations to search index.
        /// </summary>
        private readonly string searchUpdateKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionHelper"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="clientFactory">The client factory object.</param>
        public SubscriptionHelper(ILoggerComponent logger, IConfiguration config, IHttpClientFactory clientFactory)
        {
            this.logger = logger;
            this.config = config;
            this.clientFactory = clientFactory;
            this.storageTableContext = new StorageTableContext(KeyVaultHelper.GetSecret(config?.GetConfigValue(Resource.ConfigWebHooksStoreConnectionString))?.Result?.ToString(), Constants.SubscriptionsTableName);
            this.client = GraphHelper.GetGraphClient(
                 config?.GetConfigValue(Resource.ConfigTenantName),
                 KeyVaultHelper.GetSecret(config?.GetConfigValue(Resource.ConfigAADAppClientId)).Result?.ToString(),
                 KeyVaultHelper.GetSecret(config?.GetConfigValue(Resource.ConfigAADAppClientSecret)).Result?.ToString());
            this.documentTrackingHelper = new DocumentTrackingHelper(logger, config);
            this.driveDeltaHelper = new DriveDeltaHelper(this.config, this.logger);
            this.searchUpdateKey = KeyVaultHelper.GetSecret(this.config?.GetConfigValue(Common.Resource.ConfigSearchUpdateKey))?.Result?.ToString();
            this.queueClient = new QueueClient(KeyVaultHelper.GetSecret(this.config?.GetConfigValue(Constants.ChangeProcessorServiceBusConnectionName))?.Result?.ToString(), Constants.SubscriptionQueueName);
        }

        /// <summary>
        /// Gets Subscription entities or Update subscription entities in Cache.
        /// </summary>
        /// <param name="rebuildSubscriptionCache">if set to <c>true</c> [rebuild subscription cache].</param>
        /// <returns>Subscription data.</returns>
        public static async Task<List<SubscriptionEntity>> GetOrUpdateSubscriptionQueues(bool rebuildSubscriptionCache)
        {
            List<SubscriptionEntity> subscriptionQueues = new List<SubscriptionEntity>();

            if (!rebuildSubscriptionCache)
            {
                string subscriptionEntitiesJson = string.Empty;
                try
                {
                    CacheManager.GetCacheEntry(Constants.SPOCPI, Constants.SubscriptionsTableName, out subscriptionEntitiesJson);
                }
                catch
                {
                    //// Suppress the issues while trying to read data from Redis cache. Fall back to Storage table
                }

                if (!string.IsNullOrEmpty(subscriptionEntitiesJson))
                {
                    subscriptionQueues = JsonConvert.DeserializeObject<List<SubscriptionEntity>>(subscriptionEntitiesJson);
                }
            }

            if (subscriptionQueues == null || subscriptionQueues.Count == 0)
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigHelper.StringReader(Constants.ConfigTableConnectionStringName, default(string)));
                CloudTable subscriptionTable = storageAccount.CreateCloudTableClient().GetTableReference(Constants.SubscriptionsTableName);
                subscriptionQueues = await StorageTableHelper.GetTableResultBasedOnQueryFilter<SubscriptionEntity>(subscriptionTable, null).ConfigureAwait(false);
                CacheManager.AddCacheEntry(Constants.SPOCPI, Constants.SubscriptionsTableName, subscriptionQueues);
            }

            return subscriptionQueues;
        }

        /// <summary>
        /// Gets the subscription entity.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="rowKey">The row key.</param>
        /// <returns>The subscription entity.</returns>
        public static async Task<SubscriptionEntity> GetSubscriptionEntity(string partitionKey, string rowKey)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigHelper.StringReader(Constants.ConfigTableConnectionStringName, default(string)));
            CloudTable subscriptionTable = storageAccount.CreateCloudTableClient().GetTableReference(Constants.SubscriptionsTableName);
            string filter = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, partitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, rowKey));
            List<SubscriptionEntity> subscriptionEntities = await StorageTableHelper.GetTableResultBasedOnQueryFilter<SubscriptionEntity>(subscriptionTable, filter).ConfigureAwait(false);
            return (subscriptionEntities != null && subscriptionEntities.Count > 0) ? subscriptionEntities[0] : null;
        }

        /// <summary>
        /// CreateSubscription : This method is used to create graph subscription on a SharePoint library.
        /// </summary>
        /// <returns>Subscription creation task.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Reviewed.")]
        public async Task CreateSubscription()
        {
            try
            {
                var notificationUrl = this.config.GetConfigValue(Resource.ConfigWebhookNotificationUrl);
                var subscriptionExpirationDays = Convert.ToInt32(this.config.GetConfigValue(Resource.ConfigSubscriptionExpirationDays));

                if (subscriptionExpirationDays > Constants.ExpirationDaysMax)
                {
                    throw new InvalidDataException(this.config.GetConfigValue(Resource.SubscriptionExpirationDateUpdated));
                }

                var notSubscribedFilter = StorageTableHelper.CreateFilterQueryString(Constants.Status, Constants.SmallEqual, Constants.NotSubscribed);
                var activeFilter = StorageTableHelper.CreateFilterQueryBoolean(Constants.IsActive, Constants.SmallEqual, true);
                var createSubscriptionFilter = StorageTableHelper.CombineQueryFilters(notSubscribedFilter, Constants.And, activeFilter);
                var notSubscribedItems = await StorageTableHelper.GetTableResultBasedOnQueryFilter<SubscriptionEntity>(this.storageTableContext.Table, createSubscriptionFilter).ConfigureAwait(false);

                if (notSubscribedItems?.Count > 0)
                {
                    foreach (var item in notSubscribedItems)
                    {
                        try
                        {
                            // Getting Site
                            var site = SharePointHelper.GetSite(client: this.client, hostName: GetHostName(item.SiteUrl), siteRelativeUrl: GetSiteRelativeUrl(item.SiteUrl));
                            string siteName = item.SiteUrl.Split('/').Last();
                            string confidentialSite = this.config.GetConfigValue(Resource.ConfidentialSite);
                            IEnumerable<string> confidentialSiteArray = confidentialSite.Split(';');
                            if (confidentialSiteArray.Any(s => siteName.ToLower().StartsWith(s.ToLower())))
                            {
                                this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.ConfidentialSiteError, item.SiteUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                            }
                            else
                            {
                                // Getting drives ie. document libraries
                                var drives = SharePointHelper.GetDrives(this.client, site.Id);
                                if (drives?.Count > 0)
                                {
                                    await this.DriveOperation(drives, item, notificationUrl, site.Id, days: subscriptionExpirationDays).ConfigureAwait(false);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            this.logger.LogError(ex, nameof(this.CreateSubscription), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                            var updated = await this.UpdateSubscriptionTable(item, string.Empty, string.Empty, string.Empty, $"{Constants.Exception} : {ex.Message} | {Constants.StackTrace} : {ex.StackTrace}", true).ConfigureAwait(false);
                            if (updated)
                            {
                                this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.LibraryUrlError, item.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, nameof(this.CreateSubscription), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }
        }

        /// <summary>
        /// DeleteSubscription finds all the items where the status is "Delete". It first delete the subscription associated with the item and after that delete the item
        /// also.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Reviewed.")]
        public async void DeleteSubscription()
        {
            var deleteStatusFilter = StorageTableHelper.CreateFilterQueryString(Constants.Status, Constants.SmallEqual, Constants.Delete);
            try
            {
                var itemsToBeDeleted = await StorageTableHelper.GetTableResultBasedOnQueryFilter<SubscriptionEntity>(this.storageTableContext.Table, deleteStatusFilter).ConfigureAwait(false);

                if (itemsToBeDeleted?.Count > 0)
                {
                    foreach (var itemToBeDeleted in itemsToBeDeleted)
                    {
                        // The purpose of try-catch is to continue the process even if any error occurred
                        try
                        {
                            // Checking if the item is already subscribed
                            if (!string.IsNullOrWhiteSpace(itemToBeDeleted.SPOSubscriptionId))
                            {
                                // First delete the subscription
                                var subscriptionDeleted = await SharePointHelper.DeleteSubscription(this.client, itemToBeDeleted.SPOSubscriptionId).ConfigureAwait(false);
                                if (subscriptionDeleted)
                                {
                                    this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionDeleted, itemToBeDeleted.SPOSubscriptionId, itemToBeDeleted.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));

                                    itemToBeDeleted.Status = Constants.QueuedForDeletion;
                                    itemToBeDeleted.Error = string.Empty;
                                    await this.UpdateSubscriptionTable(itemToBeDeleted, itemToBeDeleted.SPOSubscriptionId, string.Empty, string.Empty, string.Empty).ConfigureAwait(false);

                                    //// Delete items from Tracking table
                                    await Task.Run(() => this.documentTrackingHelper.DeleteDocumentTrackingData(itemToBeDeleted)).ConfigureAwait(false);

                                    var tableResult = await StorageTableHelper.DeleteTableItem<SubscriptionEntity>(this.storageTableContext.Table, itemToBeDeleted).ConfigureAwait(false);
                                    if (tableResult?.Result != null)
                                    {
                                        //// Delete item from Subscription Search Index
                                        await this.DeleteSubscriptionItemFromSearch(itemToBeDeleted).ConfigureAwait(false);

                                        this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.TableEntryDeleted, itemToBeDeleted.RowKey), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                                    }
                                }
                            }
                            else
                            {
                                //// Delete disabled subscriptions
                                await this.DeleteDisabledSubscription(itemToBeDeleted).ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            this.logger.LogError(ex, nameof(this.DeleteSubscription), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));

                            var updated = await this.UpdateSubscriptionTable(itemToBeDeleted, itemToBeDeleted.SPOSubscriptionId, string.Empty, string.Empty, $"{Constants.Exception} : {ex.Message} | {Constants.StackTrace} : {ex.StackTrace}").ConfigureAwait(false);
                            if (updated)
                            {
                                this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.LibraryUrlError, itemToBeDeleted.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                            }
                        }
                    }
                }

                // if any item got missed from deletion from azure search, delete that item for the next iteration of delete subscription
                await Task.Run(() => this.DeleteMissingSubscriptionItemsFromAzureSearch().ConfigureAwait(false)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, nameof(this.DeleteSubscription), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }
        }

        /// <summary>
        /// UpdateSubscription find all the items where the ExpirationDateTime is going to expire in the next 7 days and update the subscription's ExpirationDateTime to next 21 days.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Reviewed.")]
        public async void RenewSubscription()
        {
            var expirationDateFilterLowerLimit = StorageTableHelper.CreateFilterQueryDate(Constants.ExpirationDateTime, Constants.SmallGreaterThanOrEqual, DateTimeOffset.UtcNow);
            var expirationDateFilterUpperLimit = StorageTableHelper.CreateFilterQueryDate(Constants.ExpirationDateTime, Constants.SmallLessThan, DateTimeOffset.UtcNow.AddDays(Constants.ExpirationDateTimeUpperLimit));
            var expirationDateFilter = StorageTableHelper.CombineQueryFilters(expirationDateFilterLowerLimit, Constants.And, expirationDateFilterUpperLimit);
            var subscribedFilter = StorageTableHelper.CreateFilterQueryString(Constants.Status, Constants.SmallEqual, Constants.Subscribed);
            var expirationDateAndSubscribedFilter = StorageTableHelper.CombineQueryFilters(expirationDateFilter, Constants.And, subscribedFilter);
            try
            {
                var expiringSubscriptionItems = await StorageTableHelper.GetTableResultBasedOnQueryFilter<SubscriptionEntity>(this.storageTableContext.Table, expirationDateAndSubscribedFilter).ConfigureAwait(false);

                foreach (var expiringSubscriptionItem in expiringSubscriptionItems)
                {
                    // The purpose of try-catch is to continue the process even if any error occurred
                    try
                    {
                        if (expiringSubscriptionItem.SPOSubscriptionId != null)
                        {
                            var subscription = await SharePointHelper.GetSubscription(this.client, expiringSubscriptionItem.SPOSubscriptionId).ConfigureAwait(false);
                            if (subscription != null)
                            {
                                if (subscription.ExpirationDateTime == expiringSubscriptionItem.ExpirationDateTime)
                                {
                                    try
                                    {
                                        this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionToBeRenewed, expiringSubscriptionItem.SubscriptionId, expiringSubscriptionItem.Status, expiringSubscriptionItem.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                                        this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionObjectToBeRenewed, subscription.Id, subscription.ExpirationDateTime.Value.UtcDateTime.AddDays(Constants.SubscriptionRenewDay)), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                                        var updatedSubscription = await SharePointHelper.RenewSubscription(this.client, subscription).ConfigureAwait(false);
                                        if (updatedSubscription != null)
                                        {
                                            this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionExpirationDateUpdated, updatedSubscription.Id, updatedSubscription.ExpirationDateTime), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                                            SubscriptionEntity entity = this.BuildSubscriptionEntity(updatedSubscription, expiringSubscriptionItem, expiringSubscriptionItem.SiteId, expiringSubscriptionItem.DriveId);
                                            var updated = await this.UpdateSubscriptionTable(entity).ConfigureAwait(false);
                                            if (updated)
                                            {
                                                this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionIdStatusUpdated, expiringSubscriptionItem.SPOSubscriptionId, expiringSubscriptionItem.Status, expiringSubscriptionItem.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        this.logger.LogError(ex, nameof(this.RenewSubscription), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                                        bool updated = await this.AddErrorEntryInSubscriptionTable(expiringSubscriptionItem, ex).ConfigureAwait(false);
                                        if (updated)
                                        {
                                            this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.LibraryUrlError, expiringSubscriptionItem.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                                        }
                                    }
                                }
                                else
                                {
                                    expiringSubscriptionItem.ExpirationDateTime = subscription.ExpirationDateTime.Value.UtcDateTime;
                                    await this.UpdateSubscriptionTable(expiringSubscriptionItem).ConfigureAwait(false);
                                    this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionExpirationDateUpdated, expiringSubscriptionItem.SPOSubscriptionId, expiringSubscriptionItem.ExpirationDateTime), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                                }
                            }
                            else
                            {
                                //// If subscription is not found. Create a new Subscription
                                expiringSubscriptionItem.Status = Constants.NotSubscribed;
                                await this.UpdateSubscriptionTable(expiringSubscriptionItem, expiringSubscriptionItem.SPOSubscriptionId, string.Empty, string.Empty, string.Empty, true).ConfigureAwait(false);
                                this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionStatusUpdateByRenew, expiringSubscriptionItem.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("ResourceNotFound"))
                        {
                            expiringSubscriptionItem.Status = Constants.NotSubscribed;
                            await this.UpdateSubscriptionTable(expiringSubscriptionItem, expiringSubscriptionItem.SPOSubscriptionId, string.Empty, string.Empty, string.Empty, true).ConfigureAwait(false);
                            this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionStatusUpdateByResourceNotFound, expiringSubscriptionItem.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                        }
                        else
                        {
                            this.logger.LogError(ex, nameof(this.RenewSubscription), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                            bool updated = await this.AddErrorEntryInSubscriptionTable(expiringSubscriptionItem, ex).ConfigureAwait(false);
                            if (updated)
                            {
                                this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.LibraryUrlError, expiringSubscriptionItem.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, nameof(this.RenewSubscription), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }
        }

        /// <summary>
        /// DisableSubscription finds all the items where the status is "Disabled" and delete the subscription associated with each item.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Reviewed.")]
        public async void DisableSubscription()
        {
            var disableStatusFilter = StorageTableHelper.CreateFilterQueryString(Constants.Status, Constants.SmallEqual, Constants.Disabled);
            try
            {
                var itemsToBeDisabled = await StorageTableHelper.GetTableResultBasedOnQueryFilter<SubscriptionEntity>(this.storageTableContext.Table, disableStatusFilter).ConfigureAwait(false);

                foreach (var itemToBeDisabled in itemsToBeDisabled)
                {
                    //// The purpose of try-catch is to continue the process even if any error occurred
                    try
                    {
                        //// Checking if the item is already subscribed
                        if (!string.IsNullOrWhiteSpace(itemToBeDisabled.SPOSubscriptionId))
                        {
                            //// First delete the subscription
                            var subscriptionDeleted = await SharePointHelper.DeleteSubscription(this.client, itemToBeDisabled.SPOSubscriptionId).ConfigureAwait(false);
                            if (subscriptionDeleted)
                            {
                                this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionDeleted, itemToBeDisabled.SPOSubscriptionId, itemToBeDisabled.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));

                                //// updating the subscription Id of the subscription table
                                itemToBeDisabled.Status = Constants.SubscriptionDisabled;
                                itemToBeDisabled.Error = string.Empty;
                                await this.UpdateSubscriptionTable(itemToBeDisabled, itemToBeDisabled.SPOSubscriptionId, string.Empty, string.Empty, string.Empty, true).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, nameof(this.DisableSubscription), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));

                        //// updating the subscription Id of the subscription table as the subscription is not found.
                        itemToBeDisabled.Status = Constants.SubscriptionDisabled;
                        await this.UpdateSubscriptionTable(itemToBeDisabled, itemToBeDisabled.SPOSubscriptionId, string.Empty, string.Empty, $"{Constants.Exception} : {ex.Message} | {Constants.StackTrace} : {ex.StackTrace}", true).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, nameof(this.DisableSubscription), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }
        }

        /// <summary>
        /// Runs the indexer.
        /// </summary>
        /// <param name="indexerName">Name of the indexer.</param>
        /// <returns>The task object.</returns>
        /// <exception cref="ArgumentNullException">The indexer Name.</exception>
        public async Task RunIndexer(string indexerName)
        {
            if (string.IsNullOrWhiteSpace(indexerName))
            {
                throw new ArgumentNullException(nameof(indexerName));
            }

            await SearchHelperClient.RunSearchIndexer(
                this.config.GetConfigValue(Common.Resource.ConfigSearchServiceName),
                this.searchUpdateKey,
                this.config.GetConfigValue(indexerName)).ConfigureAwait(false);
        }

        /// <summary>
        /// Outputs to queue.
        /// </summary>
        /// <param name="subscriptionEntity">The subscription entity.</param>
        /// <returns>The task.</returns>
        public async Task OutputToQueue(SubscriptionEntity subscriptionEntity)
        {
            try
            {
                var subscriptionEntityString = JsonConvert.SerializeObject(subscriptionEntity);
                var message = new Microsoft.Azure.ServiceBus.Message(Encoding.UTF8.GetBytes(subscriptionEntityString));

                await this.queueClient.SendAsync(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, nameof(this.OutputToQueue), Common.Constants.ApplicationName, nameof(Common.LogCategory.WebHooksManagerWebJob));
            }
        }

        /// <summary>
        /// GetExpirationDateTime is used to return an DateTime object based on the number of days. Here we are fixing it to 21 days maximum.
        /// </summary>
        /// <param name="expirationDateTime">The ExpirationDateTime returned from the Azure Table Storage.</param>
        /// <param name="days">Number of days to be added to the ExpirationTimeDate. Make sure it is not more than 23 days.</param>
        /// <returns>The ExpirationDateTime as a DateTime object.</returns>
        private static DateTime? GetExpirationDateTime(DateTime? expirationDateTime, int days = 21)
        {
            try
            {
                if (expirationDateTime is null || expirationDateTime == DateTime.MinValue)
                {
                    expirationDateTime = DateTime.UtcNow.AddDays(days);
                }

                if (expirationDateTime > DateTime.UtcNow.AddDays(days))
                {
                    expirationDateTime = DateTime.UtcNow.AddDays(days);
                }
            }
            catch (Exception)
            {
                expirationDateTime = DateTime.UtcNow.AddDays(days);
            }

            return expirationDateTime;
        }

        /// <summary>
        /// GetHostName method returns the hostname of the URL.
        /// </summary>
        /// <param name="path">The URL as string.</param>
        /// <returns>The hostname of the URL.</returns>
        private static string GetHostName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(Resource.UrlNotNull, nameof(path));
            }

            return new Uri(path).Host;
        }

        /// <summary>
        /// GetSiteRelativeUrl method returns the site relative URL of the URL provided.
        /// </summary>
        /// <param name="path">The URL as string.</param>
        /// <returns>The site relative URL.</returns>
        private static string GetSiteRelativeUrl(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(Resource.UrlNotNull, nameof(path));
            }

            var fullUrl = new Uri(path);
            return fullUrl.IsAbsoluteUri ? fullUrl.PathAndQuery : fullUrl.OriginalString;
        }

        /// <summary>
        /// CreateSubscriptionObject is used to create the Graph API Subscription object.
        /// </summary>
        /// <param name="clientState">The client state as string.</param>
        /// <param name="expirationDateTime">The subscription expiration date as DateTime.</param>
        /// <param name="notificationUrl">The notification URL for the subscription.</param>
        /// <param name="resource">The resource for the subscription.</param>
        /// <returns>The Subscription object.</returns>
        private static Subscription CreateSubscriptionObject(string clientState, DateTime? expirationDateTime, string notificationUrl, string resource)
        {
            return new Subscription
            {
                ChangeType = Resource.ChangeTypeUpdated,
                ClientState = clientState,
                ExpirationDateTime = expirationDateTime,
                NotificationUrl = notificationUrl,
                Resource = resource,
            };
        }

        /// <summary>
        /// DriveOperation is used to operate on the Drives returned from SharePoint graph API.
        /// </summary>
        /// <param name="drives">Collection of Drive objects.</param>
        /// <param name="entity">The SubscriptionEntity object.</param>
        /// <param name="notificationUrl">The notification listener URL as string.</param>
        /// <param name="siteId">The graph client SharePoint web site Id.</param>
        /// <param name="days">The number of days by which the subscription needs to expire.</param>
        /// <returns>The Task object.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]
        private async Task DriveOperation(IList<Drive> drives, SubscriptionEntity entity, string notificationUrl, string siteId, int days)
        {
            bool libraryExists = false;
            foreach (var drive in drives)
            {
                //// The purpose of try-catch is to continue the process even if any errors occurred
                try
                {
                    if (entity.LibraryUrl.ToUpperInvariant().Trim() == drive.WebUrl.ToUpperInvariant().Trim())
                    {
                        libraryExists = true;
                        var expirationDateTime = GetExpirationDateTime(entity.ExpirationDateTime, days: days);
                        var subscription = CreateSubscriptionObject(entity.RowKey, expirationDateTime, notificationUrl, $"{Constants.Drive}/{drive.Id}/{Constants.Root}");
                        this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionToBeCreated, entity.SubscriptionId, entity.Status, entity.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                        if (subscription != null)
                        {
                            this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionInfo, subscription.ChangeType, subscription.ClientState, subscription.ExpirationDateTime, subscription.NotificationUrl, subscription.Resource), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                        }

                        var subscriptionObject = await SharePointHelper.CreateSubscription(this.client, subscription).ConfigureAwait(false);
                        if (subscriptionObject != null)
                        {
                            entity.ExpirationDateTime = expirationDateTime;
                            entity.Error = string.Empty;
                            entity.ListId = SharePointHelper.GetListId(this.client, siteId, drive.Id);
                            entity = this.BuildSubscriptionEntity(subscriptionObject, entity, siteId, drive.Id);
                            var updated = await this.UpdateSubscriptionTable(entity).ConfigureAwait(false);
                            //// Send message to subscription queue, to do processing once the library is subscribed
                            await this.OutputToQueue(entity).ConfigureAwait(false);
                            if (updated)
                            {
                                //// If AutoIndex is true , then run full crawl
                                var subscribedSubscription = await StorageTableHelper.GetTableResult<SubscriptionEntity>(this.storageTableContext.Table, $"{Constants.SubscriptionStringValue}-{DateTime.Now.Year}", entity.RowKey).ConfigureAwait(false);
                                if (subscribedSubscription?.AutoIndex == true)
                                {
                                    var driveDeltaEntity = await this.driveDeltaHelper.UpdateDriveDeltaEntity(subscribedSubscription.RowKey).ConfigureAwait(false);
                                    if (driveDeltaEntity != null)
                                    {
                                        using (var client = this.clientFactory.CreateClient())
                                        {
                                            string crawlRequest = "{\"value\":[{\"subscriptionId\":\"" + subscribedSubscription.SPOSubscriptionId +
                                                "\",\"clientState\":\"" + subscribedSubscription.RowKey +
                                                "\",\"resource\":\"drive/" + subscribedSubscription.DriveId + "/root\"" +
                                                ",\"fullCrawl\":\"true\"}]}";
                                            await client.PostAsync(this.config.GetConfigValue(Resource.ConfigWebhookNotificationUrl), new StringContent(crawlRequest, Encoding.UTF8, Constants.ContentType)).ConfigureAwait(false);
                                        }
                                    }
                                }

                                this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionIdStatusUpdated, entity.SPOSubscriptionId, entity.Status, entity.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));

                    var updated = await this.UpdateSubscriptionTable(entity, string.Empty, string.Empty, string.Empty, ex.Message, true).ConfigureAwait(false);
                    if (updated)
                    {
                        this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.LibraryUrlError, entity.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                    }
                }
            }

            if (!libraryExists)
            {
                //// update subscription table that library doesn't exists.
                await this.UpdateSubscriptionTable(entity, string.Empty, string.Empty, string.Empty, Resource.LibraryDoesNotExist, true).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Builds the subscription entity.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="driveId">The drive identifier.</param>
        /// <returns>The updated subscription entity.</returns>
        private SubscriptionEntity BuildSubscriptionEntity(Subscription subscription, SubscriptionEntity entity, string siteId, string driveId)
        {
            if (subscription != null)
            {
                if (string.IsNullOrWhiteSpace(entity.SPOSubscriptionId) && !string.IsNullOrWhiteSpace(subscription.Id))
                {
                    entity.SPOSubscriptionId = subscription.Id;
                }

                if (string.IsNullOrWhiteSpace(entity.SiteId) && !string.IsNullOrWhiteSpace(siteId))
                {
                    entity.SiteId = siteId;
                }

                if (string.IsNullOrWhiteSpace(entity.DriveId) && !string.IsNullOrWhiteSpace(driveId))
                {
                    entity.DriveId = driveId;
                }

                if (!string.IsNullOrWhiteSpace(entity.Status) && entity.Status == Constants.NotSubscribed)
                {
                    entity.Status = Constants.Subscribed;
                }

                if (entity.CreationDateTime == null || entity.CreationDateTime == DateTime.MinValue)
                {
                    entity.CreationDateTime = DateTime.UtcNow;
                }

                entity.ExpirationDateTime = subscription.ExpirationDateTime.Value.UtcDateTime;
            }

            return entity;
        }

        /// <summary>
        /// UpdateSubscriptionTable is used to update the Azure "subscriptions" table.
        /// </summary>
        /// <param name="entity">The SubscriptionEntity object.</param>
        /// <returns>Boolean value to indicate whether the update is successful or not.</returns>
        private async Task<bool> UpdateSubscriptionTable(SubscriptionEntity entity)
        {
            var updated = false;
            try
            {
                if (entity != null)
                {
                    var tableResult = await StorageTableHelper.AddOrMergeAsync(this.storageTableContext.Table, entity).ConfigureAwait(false);
                    if (tableResult?.Result != null)
                    {
                        await GetOrUpdateSubscriptionQueues(true).ConfigureAwait(false);
                        updated = true;
                        this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionIdStatusUpdated, entity.SPOSubscriptionId, entity.Status, entity.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }

            return updated;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]

        /// <summary>
        /// UpdateSubscriptionTable is used to update the Azure "subscriptions" table.
        /// </summary>
        /// <param name="entity">The SubscriptionEntity object.</param>
        /// <param name="subscriptionId">The subscription Id.</param>
        /// <param name="siteId">The graph API site Id for a SharePoint site.</param>
        /// <param name="driveId">The graph API drive Id for a SharePoint library.</param>
        /// <param name="isDisabledOrNotSubscribed">True if the function is called with a subscriptionId otherwise false.</param>
        /// <returns>Boolean value to indicate whether the update is successful or not.</returns>
        private async Task<bool> UpdateSubscriptionTable(SubscriptionEntity entity, string subscriptionId, string siteId, string driveId, string errorMessage, bool isDisabledOrNotSubscribed = false)
        {
            var updated = false;
            try
            {
                if (!string.IsNullOrWhiteSpace(subscriptionId) || isDisabledOrNotSubscribed)
                {
                    if (string.IsNullOrWhiteSpace(entity.SPOSubscriptionId) && !string.IsNullOrWhiteSpace(subscriptionId))
                    {
                        entity.SPOSubscriptionId = subscriptionId;
                    }

                    if (string.IsNullOrWhiteSpace(entity.SiteId) && !string.IsNullOrWhiteSpace(siteId))
                    {
                        entity.SiteId = siteId;
                    }

                    if (string.IsNullOrWhiteSpace(entity.DriveId) && !string.IsNullOrWhiteSpace(driveId))
                    {
                        entity.DriveId = driveId;
                    }

                    if (!string.IsNullOrWhiteSpace(entity.Status) && entity.Status == Constants.NotSubscribed && !isDisabledOrNotSubscribed)
                    {
                        entity.Status = Constants.Subscribed;
                    }

                    if ((entity.CreationDateTime == null || entity.CreationDateTime == DateTime.MinValue) && !isDisabledOrNotSubscribed)
                    {
                        entity.CreationDateTime = DateTime.UtcNow;
                    }

                    if (isDisabledOrNotSubscribed)
                    {
                        entity.SPOSubscriptionId = string.Empty;
                        entity.ExpirationDateTime = null;
                    }

                    entity.Error = errorMessage;

                    var tableResult = await StorageTableHelper.AddOrReplaceTableItem(this.storageTableContext.Table, entity, Constants.SubscriptionsTableName, addToCache: false).ConfigureAwait(false);
                    if (tableResult?.Result != null)
                    {
                        updated = true;
                        await SubscriptionHelper.GetOrUpdateSubscriptionQueues(true).ConfigureAwait(false);
                        this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.SubscriptionIdStatusUpdated, entity.SPOSubscriptionId, entity.Status, entity.LibraryUrl), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }

            return updated;
        }

        /// <summary>
        /// Delete entries from the Subscription Azure Search.
        /// </summary>
        /// <param name="itemToBeDeleted">The SubscriptionEntity item to be deleted.</param>
        /// <returns>The Task object.</returns>
        private async Task DeleteSubscriptionItemFromSearch(SubscriptionEntity itemToBeDeleted)
        {
            //// Delete items from Subscription Index
            await SearchHelperClient.DeleteDocumentsFromIndex(
                this.config.GetConfigValue(Resource.ConfigSearchServiceName),
                this.config.GetConfigValue(Resource.ConfigSubscriptionIndexName),
                this.searchUpdateKey,
                Constants.DriveId,
                itemToBeDeleted.DriveId).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the Error message and update the subscription entry with the error message.
        /// </summary>
        /// <param name="itemToBeDisabled">The subscription Entity object.</param>
        /// <param name="ex">The exception object.</param>
        /// <returns>bool value indicating success or failure.</returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Reviewed.")]
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Reviewed.")]
        private async Task<bool> AddErrorEntryInSubscriptionTable(SubscriptionEntity itemToBeDisabled, Exception ex)
        {
            try
            {
                return await this.UpdateSubscriptionTable(itemToBeDisabled, itemToBeDisabled.SPOSubscriptionId, string.Empty, string.Empty, $"{Constants.Exception} : {ex.Message} | {Constants.StackTrace} : {ex.StackTrace}").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, nameof(this.AddErrorEntryInSubscriptionTable), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
                return false;
            }
        }

        /// <summary>
        /// DeleteMissingItemFromSearch is used to delete any item which is missed from deletion from the subscription azure search index.
        /// </summary>
        /// <returns>The task object.</returns>
        private async Task DeleteMissingSubscriptionItemsFromAzureSearch()
        {
            SearchResults<SubscriptionEntity> result = await this.GetItemsFromSubscriptionAzureSearchStatusDeleted().ConfigureAwait(false);
            Pageable<SearchResult<SubscriptionEntity>> searchResults = result?.GetResults();
            if (searchResults != null && searchResults.Any())
            {
                foreach (var item in searchResults)
                {
                    // check for the entry in SubscriptionTable
                    SubscriptionEntity subscriptionItem = StorageTableHelper.GetTableResult<SubscriptionEntity>(this.storageTableContext.Table, item.Document.PartitionKey, item.Document.RowKey).Result;

                    // if the item don't exists in the subscription table, delete it from azure search also
                    if (subscriptionItem is null)
                    {
                        // delete the item from azure subscription search index
                        await this.DeleteSubscriptionItemFromSearch(subscriptionItem).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// The method is used to get search result from subscription azure search based on the filter where Status is Deleted.
        /// </summary>
        /// <returns>The dynamic task object.</returns>
        private async Task<dynamic> GetItemsFromSubscriptionAzureSearchStatusDeleted()
        {
            var searchServiceName = this.config?.GetConfigValue(Common.Resource.ConfigSearchServiceName);
            var subscriptionIndexname = this.config?.GetConfigValue(Common.Resource.ConfigSubscriptionIndexName);
            var selectProperties = new[] { Constants.PartitionKey, Constants.RowKey, Constants.Status };
            var filter = $"{Constants.Status} {Constants.EqualShort} '{Constants.Delete}' {Constants.AndToLower} {Constants.Timestamp} {Constants.SmallLessThanShort} {DateTimeOffset.UtcNow.AddMinutes(-10).ToString("O", CultureInfo.InvariantCulture)}";
            return (SearchResults<SubscriptionEntity>)await SearchHelper.Search<SubscriptionEntity>(searchServiceName, subscriptionIndexname, this.searchUpdateKey, Constants.WildCardSearch, filter, selectProperties).ConfigureAwait(false);
        }

        /// <summary>
        /// This method is used to delete subscription which are in disabled state.
        /// </summary>
        /// <param name="itemToBeDeleted">The subscription entity object where the subscription Id is null.</param>
        /// <returns>The task object.</returns>
        private async Task DeleteDisabledSubscription(SubscriptionEntity itemToBeDeleted)
        {
            var tableResult = await StorageTableHelper.DeleteTableItem<SubscriptionEntity>(this.storageTableContext.Table, itemToBeDeleted).ConfigureAwait(false);
            if (tableResult?.Result != null)
            {
                //// Delete item from Subscription Search Index
                await this.DeleteSubscriptionItemFromSearch(itemToBeDeleted).ConfigureAwait(false);
                await SubscriptionHelper.GetOrUpdateSubscriptionQueues(true).ConfigureAwait(false);
                this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.TableEntryDeleted, itemToBeDeleted.RowKey), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerWebJob));
            }
        }
    }
}