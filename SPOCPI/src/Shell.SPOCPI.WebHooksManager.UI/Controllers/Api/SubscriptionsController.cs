// -----------------------------------------------------------------------
// <copyright file="SubscriptionsController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------
namespace Shell.SPOCPI.WebHooksManager.UI.Controllers.Api
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Azure.Search.Documents.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.ServiceBus;
    using Newtonsoft.Json;
    using Shell.SPOCPI.Common;
    using Shell.SPOCPI.Common.Helpers;
    using Shell.SPOCPI.WebHooksManager.UI.Models;
    using Constants = Shell.SPOCPI.Common.Constants;
    using Resource = Shell.SPOCPI.WebHooksManager.UI.Resource;

    /// <summary>
    /// Subscriptions controller.
    /// </summary>
    /// <seealso cref="ControllerBase" />
    [Route(Constants.RouteApiControllerAction)]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        /// <summary>
        /// The search helper client.
        /// </summary>
        private readonly SearchHelperClient searchHelperClient;

        /// <summary>
        /// The storage table context.
        /// </summary>
        private readonly StorageTableContext storageTableContext;

        /// <summary>
        /// The logger component.
        /// </summary>
        private readonly ILoggerComponent logger;

        /// <summary>
        /// The configuration instance.
        /// </summary>
        private readonly IConfiguration config;

        /// <summary>
        /// The client factory.
        /// </summary>
        private readonly IHttpClientFactory clientFactory;

        /// <summary>
        /// The drive delta helper instance.
        /// </summary>
        private readonly UI.DriveDeltaHelper driveDeltaHelper;

        /// <summary>
        /// The search update key - used to perform read and write operations to search index.
        /// </summary>
        private readonly string searchUpdateKey;

        /// <summary>
        /// The queue client instance.
        /// </summary>
        private readonly IQueueClient queueClient;

        /// <summary>
        /// The documentTrackingHelper instance.
        /// </summary>
        private readonly DocumentTrackingHelper documentTrackingHelper;

        /// <summary>
        /// The subscriptionHelper instance.
        /// </summary>
        private readonly SubscriptionHelper subscriptionHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionsController" /> class.
        /// </summary>
        /// <param name="config">The configuration instance.</param>
        /// <param name="logger">The logger component.</param>
        /// <param name="clientFactory">The client factory.</param>
        public SubscriptionsController(IConfiguration config, ILoggerComponent logger, IHttpClientFactory clientFactory)
        {
            this.logger = logger;
            this.config = config;
            this.clientFactory = clientFactory;
            this.storageTableContext = new StorageTableContext(
                KeyVaultHelper.GetSecret(config?.GetConfigValue(Common.Resource.ConfigWebHooksStoreConnectionString))?.Result?.ToString(),
                Constants.SubscriptionsTableName);
            this.searchUpdateKey = KeyVaultHelper.GetSecret(config?.GetConfigValue(Common.Resource.ConfigSearchUpdateKey))?.Result?.ToString();
            this.searchHelperClient = new SearchHelperClient(
                config?.GetConfigValue(Common.Resource.ConfigSearchServiceName),
                config?.GetConfigValue(Common.Resource.ConfigSubscriptionIndexName),
                this.searchUpdateKey);
            this.driveDeltaHelper = new UI.DriveDeltaHelper(this.config, this.logger);
            this.queueClient = new QueueClient(KeyVaultHelper.GetSecret(this.config.GetConfigValue(Common.Resource.ConfigCPServiceBusConnectionString))?.Result?.ToString(), this.config.GetConfigValue(Common.Resource.ConfigOutputQueueNames));
            this.documentTrackingHelper = new DocumentTrackingHelper(logger, config);
            this.subscriptionHelper = new SubscriptionHelper(logger, config, clientFactory);
        }

        /// <summary>
        /// Gets this instance.
        /// </summary>
        /// <returns>Action Result.</returns>
        [HttpGet]
        [Route("")]
        public async Task<ActionResult<IEnumerable<SubscriptionEntity>>> Get()
        {
            try
            {
                var returnObject = await StorageTableHelper.GetTableResultBasedOnQueryFilter<SubscriptionEntity>(this.storageTableContext.Table, string.Empty).ConfigureAwait(true);
                return this.Ok(returnObject);
            }
            catch (ArgumentNullException ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Posts the specified values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns>Table Result.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        [HttpPost]
        public async Task<ActionResult<IList<TableResult>>> Post([FromBody] SubscriptionEntity[] values)
        {
            try
            {
                string confidentialSite = this.config.GetConfigValue(Common.Resource.ConfidentialSite);
                var stripTrailingSlash = values.Select(s => s.SiteUrl.EndsWith("/") ? s.SiteUrl.Substring(0, s.SiteUrl.Length - 1) : s.SiteUrl).ToList();
                var inputSiteArray = stripTrailingSlash.Select(s => s.Split('/').Last()).ToList();
                var confidentialSiteArray = confidentialSite.Split(';');
                foreach (var item in inputSiteArray)
                {
                    if (confidentialSiteArray.Any(s => item.ToLower().StartsWith(s.ToLower())))
                    {
                        return this.Conflict(Common.Resource.ConfidentialSubscriptionError);
                    }
                }

                IList<TableResult> returnObject = null;
                values = values.GroupBy(item => item.LibraryUrl).Select(item => item.First()).ToArray();
                List<SubscriptionEntity> subscriptions = new List<SubscriptionEntity>();
                foreach (var value in values)
                {
                    value.LibraryUrl = new Uri(value.LibraryUrl).AbsoluteUri;
                    bool isDuplicate = StorageTableHelper.CheckDuplicateSubscription(this.storageTableContext.Table, value.LibraryUrl).GetAwaiter().GetResult();
                    if (!isDuplicate)
                    {
                        value.PartitionKey = string.Format(CultureInfo.InvariantCulture, Resource.Subscription, DateTime.Now.Year);
                        value.RowKey = Guid.NewGuid().ToString();
                        value.Status = Constants.NotSubscribed;
                        subscriptions.Add(value);
                    }
                }

                if (subscriptions.Count > 0)
                {
                    if (subscriptions.Count > 1000)
                    {
                        return this.BadRequest(Resource.BulkUploadProcessingMessage);
                    }
                    else
                    {
                        //// Batching items in a group of 100
                        for (int i = 0; i < subscriptions.Count; i += 100)
                        {
                            var batchItems = subscriptions.Skip(i)
                                                 .Take(100)
                                                 .ToList();
                            returnObject = await StorageTableHelper.AddOrReplaceMultipleTableItems(this.storageTableContext.Table, batchItems.ToArray()).ConfigureAwait(true);
                            await SubscriptionHelper.GetOrUpdateSubscriptionQueues(true).ConfigureAwait(true);
                        }

                        return this.Ok(returnObject);
                    }
                }
                else
                {
                    return this.Conflict(Resource.SubscriptionAlreadyPresent);
                }
            }
            catch (ArgumentNullException ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Deletes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Action Result.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        [HttpDelete]
        public async Task<IActionResult> Delete(SubscriptionEntity value)
        {
            try
            {
                return await this.DeleteSubscription(value).ConfigureAwait(false);
            }
            catch (ArgumentNullException ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Disables the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Action Result.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        [HttpPatch]
        public async Task<IActionResult> Disable(SubscriptionEntity value)
        {
            try
            {
                return await this.DisableSubscription(value).ConfigureAwait(false);
            }
            catch (ArgumentNullException ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Searches the specified query text.
        /// </summary>
        /// <param name="queryText">The query text.</param>
        /// <returns>Document Search Result(Subscription Entity).</returns>
        [HttpGet(Name = Constants.Search)]
        [Route(Constants.RouteAction)]
        public async Task<ActionResult<SearchResults<SubscriptionEntity>>> Search()
        {
            try
            {
                string queryText = this.HttpContext.Request.Query[Constants.SearchValue].ToString();
                var pattern = this.config.GetConfigValue(Common.Resource.ConfigHTMLInputValidation);
                if (UserInputValidation.CheckValidInput(queryText, pattern))
                {
                    int draw = Convert.ToInt32(this.HttpContext.Request.Query[Constants.SearchDraw].ToString(), CultureInfo.InvariantCulture);
                    int start = Convert.ToInt32(this.HttpContext.Request.Query[Constants.SearchStart].ToString(), CultureInfo.InvariantCulture);
                    int length = Convert.ToInt32(this.HttpContext.Request.Query[Constants.SearchLength].ToString(), CultureInfo.InvariantCulture);
                    string orderColumn = this.HttpContext.Request.Query[Constants.SearchOrderBy].ToString();
                    string orderDir = this.HttpContext.Request.Query[Constants.SearchOrderDirection].ToString();
                    if (!string.IsNullOrEmpty(orderColumn) && !string.IsNullOrEmpty(orderDir))
                    {
                        orderColumn = orderColumn.Replace(Constants.SearchDocumentParameter, string.Empty, StringComparison.InvariantCulture);
                        orderColumn = char.ToUpper(orderColumn[0], CultureInfo.InvariantCulture) + orderColumn[1..];
                        orderColumn = string.Format(CultureInfo.InvariantCulture, "{0} {1}", orderColumn, orderDir);
                    }

                    IList<string> selectProperties = new[] { Constants.PartitionKey, Constants.RowKey, Constants.DriveId, Constants.LibraryURL, Constants.SiteUrl, Constants.IsActive, Constants.Status, Constants.SPOSubscriptionId, Constants.Description, Constants.CreationDateTime, Constants.OutputQueue, Constants.AutoIndex, Constants.IncludeFolderRelativePath, nameof(SubscriptionEntity.Parameters) };
                    var searchResults = await SearchHelper.Search<SubscriptionEntity>(this.searchHelperClient.SearchClient, queryText, select: selectProperties, orderBy: orderColumn, skip: start, top: length).ConfigureAwait(true);
                    SubscriptionDataTable resultObject = new SubscriptionDataTable(searchResults.GetResults(), draw, (int)searchResults.TotalCount, (int)searchResults.TotalCount);
                    return this.Ok(resultObject);
                }
                else
                {
                    return this.BadRequest(Common.Resource.InvalidUserInput);
                }
            }
            catch (ArgumentNullException ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Run Crawl for the specified subscription.
        /// </summary>
        /// <param name="request">The request body.</param>
        /// <returns>Action Result.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route(Constants.RouteAction)]
        public async Task<ActionResult> Crawl([FromBody] IndexRequest request)
        {
            if (request.Subscription != null && !string.IsNullOrWhiteSpace(request.Subscription.RowKey))
            {
                return await this.RunCrawl(request.Subscription, request.BypassSpoNotification, request.QueueNames).ConfigureAwait(false);
            }
            else
            {
                return this.BadRequest(new ArgumentNullException(nameof(request)));
            }
        }

        /// <summary>
        /// THe function is used to run the crawl from last drive delta entry for the subscription.
        /// </summary>
        /// <param name="subscription">The subscription object.</param>
        /// <returns>The ActionResult object.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route(Constants.RouteAction)]
        public async Task<IActionResult> RunLastCrawl([FromBody] SubscriptionEntity subscription)
        {
            if (subscription is null)
            {
                return this.BadRequest(new ArgumentNullException(nameof(subscription)));
            }

            try
            {
                var driveDeltaEntity = await this.driveDeltaHelper.GetDeltaTokenIfExists(subscription.RowKey).ConfigureAwait(false);
                if (driveDeltaEntity != null)
                {
                    var client = this.clientFactory.CreateClient();
                    string crawlRequest = "{\"value\":[{\"subscriptionId\":\"" + subscription.SPOSubscriptionId +
                        "\",\"clientState\":\"" + subscription.RowKey +
                        "\",\"resource\":\"drive/" + subscription.DriveId + "/root\"" +
                        ",\"fullCrawl\":\"true\"}]}";
                    var result = await client.PostAsync(this.config.GetConfigValue(Common.Resource.ConfigWebhookNotificationUrl), new StringContent(crawlRequest, Encoding.UTF8, "application/json")).ConfigureAwait(false);

                    //// Delete items from Tracking table
                    await Task.Run(() => this.documentTrackingHelper.DeleteDocumentTrackingData(subscription, driveDeltaEntity)).ConfigureAwait(false);

                    return this.Ok(result);
                }
                else
                {
                    return this.Ok(string.Format(CultureInfo.InvariantCulture, Resource.NoDriveDeltaEntryFound, subscription.SPOSubscriptionId));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex);
            }
        }

        /// <summary>
        /// Activate the specified subscription.
        /// </summary>
        /// <param name="subscriptionEntity">The subscription.</param>
        /// <returns>Action Result returning whether the operation was success or fail.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route(Constants.RouteAction)]
        public async Task<ActionResult> ActivateSubscription([FromBody] SubscriptionEntity subscriptionEntity)
        {
            if (subscriptionEntity is null)
            {
                throw new ArgumentNullException(nameof(subscriptionEntity));
            }

            try
            {
                Dictionary<string, bool> properties = new Dictionary<string, bool>();
                if (!subscriptionEntity.IsActive)
                {
                    properties.Add(Constants.IsActive, true);
                    await StorageTableHelper.ModifyEntity(this.storageTableContext.Table, subscriptionEntity, properties: properties).ConfigureAwait(false);
                    return this.NoContent();
                }
                else
                {
                    return this.BadRequest(Resource.SubscriptionEntityNull);
                }
            }
            catch (ArgumentNullException ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Returns query suggestions.
        /// </summary>
        /// <returns>List of suggestions.</returns>
        [HttpGet(Name = Constants.AutocompleteSuggestions)]
        [Route(Constants.RouteAction)]
        public ActionResult<List<string>> AutocompleteSuggestions()
        {
            List<string> outputQueues = ((string[])this.config.GetConfigValue(Common.Resource.ConfigOutputQueueNames).Split(';', StringSplitOptions.RemoveEmptyEntries)).ToList();
            return this.Ok(outputQueues);
        }

        /// <summary>
        /// Disable the selected subscription.
        /// </summary>
        /// <param name="subscriptions">The array of subscription entity object.</param>
        /// <returns>Action Results.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route(Constants.RouteAction)]
        public async Task<ActionResult> DisableSubscriptions([FromBody] SubscriptionEntity[] subscriptions)
        {
            var entities = new List<ActionResult>();
            try
            {
                if (subscriptions == null || subscriptions.Length == 0)
                {
                    return this.BadRequest(new ArgumentNullException(nameof(subscriptions)));
                }
                else
                {
                    await Task.Run(async () =>
                    {
                        for (int i = 0; i < subscriptions.Length; i++)
                        {
                            var result = await this.DisableSubscription(subscriptions[i], runSearchIndexer: false).ConfigureAwait(false);
                            entities.Add(result);
                        }
                    }).ConfigureAwait(false);
                }

                await SearchHelper.RefreshSearchIndex(
                   this.config.GetConfigValue(Common.Resource.ConfigSearchServiceName),
                   this.searchUpdateKey,
                   this.config.GetConfigValue(Common.Resource.ConfigSubscriptionIndexerName)).ConfigureAwait(false);
                return this.Ok(entities);
            }
            catch (ArgumentNullException ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Delete the selected subscription.
        /// </summary>
        /// <param name="subscriptions">The array of subscription entity object.</param>
        /// <returns>Action Results.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route(Constants.RouteAction)]
        public async Task<IActionResult> DeleteSubscriptions([FromBody] SubscriptionEntity[] subscriptions)
        {
            var entities = new List<ActionResult>();
            try
            {
                if (subscriptions == null || subscriptions.Length == 0)
                {
                    return this.BadRequest(new ArgumentNullException(nameof(subscriptions)));
                }
                else
                {
                    Task.Run(async () =>
                    {
                        for (int i = 0; i < subscriptions.Length; i++)
                        {
                            var result = await this.DeleteSubscription(subscriptions[i], runSearchIndexer: false).ConfigureAwait(false);
                            entities.Add(result);
                        }
                    });
                }

                await SearchHelper.RefreshSearchIndex(
                   this.config.GetConfigValue(Common.Resource.ConfigSearchServiceName),
                   this.searchUpdateKey,
                   this.config.GetConfigValue(Common.Resource.ConfigSubscriptionIndexerName)).ConfigureAwait(false);
                return this.Ok(entities);
            }
            catch (ArgumentNullException ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Updates the subscription.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Action Result.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route(Constants.RouteAction)]
        public async Task<IActionResult> Update([FromBody] SubscriptionEntity value)
        {
            try
            {
                return await this.UpdateSubscription(value).ConfigureAwait(false);
            }
            catch (ArgumentNullException ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Run crawl is used to full crawl of the subscription.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="bypassSpoNotification">if set to <c>true</c> [bypass spo notification].</param>
        /// <param name="queueNames">The queue names.</param>
        /// <returns>Action Result.</returns>
        private async Task<ActionResult> RunCrawl(SubscriptionEntity subscription, bool bypassSpoNotification, string queueNames)
        {
            if (subscription is null)
            {
                return this.BadRequest(new ArgumentNullException(nameof(subscription)));
            }

            // fetch entire subscription entity.
            SubscriptionEntity subscriptionEntity = await SubscriptionHelper.GetSubscriptionEntity(subscription.PartitionKey, subscription.RowKey).ConfigureAwait(false);

            // Add entry to subscription queue.
            await this.subscriptionHelper.OutputToQueue(subscriptionEntity).ConfigureAwait(false);

            // Update queues to index for this crawl in subscription table
            subscription.IndexQueues = queueNames;
            await StorageTableHelper.AddOrMergeAsync(this.storageTableContext.Table, subscription).ConfigureAwait(true);
            await SubscriptionHelper.GetOrUpdateSubscriptionQueues(true).ConfigureAwait(true);

            if (!bypassSpoNotification)
            {
                var driveDeltaEntity = await this.driveDeltaHelper.UpdateDriveDeltaEntity(subscription.RowKey).ConfigureAwait(false);
                if (driveDeltaEntity == null)
                {
                    return this.Ok(string.Format(CultureInfo.InvariantCulture, Resource.NoDriveDeltaEntryFound, subscription.SPOSubscriptionId));
                }
            }
            else
            {
                subscription.RowKey = "custom-" + subscription.RowKey;
            }

            try
            {
                var client = this.clientFactory.CreateClient();
                string crawlRequest = "{\"value\":[{\"subscriptionId\":\"" + subscription.SPOSubscriptionId +
                    "\",\"clientState\":\"" + subscription.RowKey +
                    "\",\"resource\":\"drive/" + subscription.DriveId + "/root\"" +
                    ",\"fullCrawl\":\"true\",\"bypassSpoNotification\":\"" + bypassSpoNotification +
                    "\",\"queueNames\":\"" + queueNames + "\",\"changeType\":\"custom\"}]}";

                var result = await client.PostAsync(this.config.GetConfigValue(Common.Resource.ConfigWebhookNotificationUrl), new StringContent(crawlRequest, Encoding.UTF8, "application/json")).ConfigureAwait(false);

                if (!bypassSpoNotification)
                {
                    // delete the notification in a background thread
                    //// Delete items from Tracking table
                    await Task.Run(() => this.documentTrackingHelper.DeleteDocumentTrackingData(subscription)).ConfigureAwait(false);
                }

                return this.Ok(result);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex);
            }
        }

        /// <summary>
        /// Delete the provided subscription from Storage and Azure Search.
        /// </summary>
        /// <param name="value">The SubscriptionEntity object.</param>
        /// <param name="runSearchIndexer">Boolean indicating whether to run the search indexer or not.</param>
        /// <returns>The Action result.</returns>
        private async Task<ActionResult> DeleteSubscription(SubscriptionEntity value, bool runSearchIndexer = true)
        {
            if (value != null)
            {
                if (await StorageTableHelper.IsSubscriptionDeleted(this.storageTableContext.Table, value).ConfigureAwait(false))
                {
                    return this.Conflict(Resource.SubscritionMarkedDeletion);
                }
                else
                {
                    value.Status = Constants.Delete;
                    await StorageTableHelper.DeleteSubscription(this.storageTableContext.Table, value).ConfigureAwait(false);
                    await SubscriptionHelper.GetOrUpdateSubscriptionQueues(true).ConfigureAwait(false);
                    if (runSearchIndexer)
                    {
                        await SearchHelper.RefreshSearchIndex(
                           this.config.GetConfigValue(Common.Resource.ConfigSearchServiceName),
                           this.searchUpdateKey,
                           this.config.GetConfigValue(Common.Resource.ConfigSubscriptionIndexerName)).ConfigureAwait(false);
                    }
                    //// if the deletion is successful, send a message to the service bus queue with the Drive Id and IsDeleted property.
                    if (!string.IsNullOrWhiteSpace(value.DriveId))
                    {
                        await this.SendDeleteNotification(value).ConfigureAwait(false);
                    }

                    return this.NoContent();
                }
            }
            else
            {
                return this.BadRequest(Resource.SubscriptionEntityNull);
            }
        }

        /// <summary>
        /// This method is used to send a message in the service bus queue with DriveId and IsDeleted property.
        /// </summary>
        /// <param name="value">The subscription entity object.</param>
        /// <returns>Return success or failure as bool value.</returns>
        /// <exception cref="ArgumentNullException">The SubscriptionEntity value.</exception>
        private async Task<bool> SendDeleteNotification(SubscriptionEntity value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            bool success = false;
            try
            {
                List<DocumentEntity> documentEntities = new List<DocumentEntity>();
                var documentEntity = new DocumentEntity()
                {
                    PartitionKey = StorageTableHelper.GetBase64String(value.DriveId), // should be base64 Drive Id
                    IsDeleted = true,
                    RowKey = Constants.Root,
                    SubscriptionId = value.SubscriptionId,
                };
                documentEntities.Add(documentEntity);

                var documentEntitiesString = JsonConvert.SerializeObject(documentEntities);
                var message = new Message(Encoding.UTF8.GetBytes(documentEntitiesString));

                //// Send the message to the queue
                await this.queueClient.SendAsync(message).ConfigureAwait(true);
                success = true;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, nameof(this.SendDeleteNotification), Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
            }

            return success;
        }

        /// <summary>
        /// Disable the provided subscription from storage and azure search.
        /// </summary>
        /// <param name="value">The subscriptionEntity object.</param>
        /// <param name="runSearchIndexer">Boolean indicating whether to run the search indexer or not.</param>
        /// <returns>The action result.</returns>
        private async Task<ActionResult> DisableSubscription(SubscriptionEntity value, bool runSearchIndexer = true)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            if (value != null && !string.IsNullOrEmpty(value.Status))
            {
                if (await StorageTableHelper.IsSubscriptionDisabled(this.storageTableContext.Table, value).ConfigureAwait(false))
                {
                    return this.Conflict(Resource.SubscriptionMarked + value.Status);
                }
                else
                {
                    properties.Add(Constants.Status, value.Status);
                    await StorageTableHelper.ModifyEntity(this.storageTableContext.Table, value, properties).ConfigureAwait(false);
                    if (runSearchIndexer)
                    {
                        await SearchHelper.RefreshSearchIndex(
                           this.config.GetConfigValue(Common.Resource.ConfigSearchServiceName),
                           this.searchUpdateKey,
                           this.config.GetConfigValue(Common.Resource.ConfigSubscriptionIndexerName)).ConfigureAwait(false);
                    }

                    return this.NoContent();
                }
            }
            else
            {
                return this.BadRequest(Resource.SubscriptionEntityNull);
            }
        }

        /// <summary>
        /// Update the provided subscription with provided details in storage and azure search.
        /// </summary>
        /// <param name="value">The subscriptionEntity object.</param>
        /// <param name="runSearchIndexer">The parameter that indicates whether to run the search index or not.</param>
        /// <returns>The action result.</returns>
        private async Task<ActionResult> UpdateSubscription(SubscriptionEntity value, bool runSearchIndexer = true)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            if (value != null && !string.IsNullOrEmpty(value.OutputQueue))
            {
                properties.Add(Constants.OutputQueue, value.OutputQueue);
                properties.Add(Constants.Description, value.Description);
                properties.Add(nameof(SubscriptionEntity.Parameters), value.Parameters);
                await StorageTableHelper.ModifyEntity(this.storageTableContext.Table, value, properties).ConfigureAwait(false);
                await SubscriptionHelper.GetOrUpdateSubscriptionQueues(true).ConfigureAwait(false);
                if (runSearchIndexer)
                {
                    await SearchHelper.RefreshSearchIndex(
                        this.config.GetConfigValue(Common.Resource.ConfigSearchServiceName),
                        this.searchUpdateKey,
                        this.config.GetConfigValue(Common.Resource.ConfigSubscriptionIndexerName)).ConfigureAwait(false);
                }

                return this.NoContent();
            }
            else
            {
                return this.BadRequest(Resource.SubscriptionEntityNull);
            }
        }
    }
}