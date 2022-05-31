// -----------------------------------------------------------------------
// <copyright file="CPHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------

namespace Shell.SPOCPI.ChangeProcessor.WebJob
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Graph;
    using Newtonsoft.Json;
    using Shell.SPOCPI.Common;
    using Constants = Common.Constants;

    /// <summary>
    /// Change Processing Job helper class.
    /// </summary>
    internal class CPHelper
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
        /// The queue helper instance.
        /// </summary>
        private readonly QueueHelper queueHelper;

        /// <summary>
        /// The drive delta table instance.
        /// </summary>
        private readonly CloudTable driveDeltaTable;

        /// <summary>
        /// The drive delta transaction table instance.
        /// </summary>
        private readonly CloudTable driveDeltaTransactionTable;

        /// <summary>
        /// The message storage table.
        /// </summary>
        private readonly CloudTable messageStorageTable;

        /// <summary>
        /// The document tracking table.
        /// </summary>
        private readonly CloudTable docTrackTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="CPHelper"/> class.
        /// </summary>
        /// <param name="loggerComponent">The logger.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="queueHelperInstance">The queue helper.</param>
        internal CPHelper(ILoggerComponent loggerComponent, IConfiguration configuration, QueueHelper queueHelperInstance)
        {
            this.logger = loggerComponent;
            this.config = configuration;
            this.queueHelper = queueHelperInstance;
            this.messageStorageTable = StorageTableHelper.GetTableEntity(KeyVaultHelper.GetSecret(this.config.GetConfigValue(Common.Resource.ConfigCPDriveDeltaStoreConnString))?.Result?.ToString(), Common.Constants.NotificationTableName);
            this.driveDeltaTable = StorageTableHelper.GetTableEntity(KeyVaultHelper.GetSecret(this.config.GetConfigValue(Common.Resource.ConfigCPDriveDeltaStoreConnString))?.Result?.ToString(), this.config.GetConfigValue(Common.Resource.ConfigCPDriveDeltaTableName));
            this.driveDeltaTransactionTable = StorageTableHelper.GetTableEntity(KeyVaultHelper.GetSecret(this.config.GetConfigValue(Common.Resource.ConfigCPDriveDeltaStoreConnString))?.Result?.ToString(), this.config.GetConfigValue(Common.Resource.ConfigCPDriveDeltaTransTableName));
            this.docTrackTable = StorageTableHelper.GetTableEntity(KeyVaultHelper.GetSecret(this.config.GetConfigValue(Common.Resource.ConfigCPDriveDeltaStoreConnString))?.Result?.ToString(), this.config.GetConfigValue(Common.Resource.ConfigDocumentTrackingTableName));
        }

        /// <summary>
        /// Processes the queue messages.
        /// </summary>
        /// <returns>The task status.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Reviewed.")]
        internal async Task ProcessQueueMessages()
        {
            bool processChanges = false;
            List<string> inProgressSubscriptions = new List<string>();
            string completedSubscription = string.Empty;

            try
            {
                var notificationEntities = await this.GetMessageFromStorage().ConfigureAwait(false);
                foreach (var notificationEntity in notificationEntities)
                {
                    var notification = JsonConvert.DeserializeObject<Notification>(notificationEntity.MessageJson);
                    if (notification == null || inProgressSubscriptions.Contains(notification.ClientState))
                    {
                        continue;
                    }
                    else if (notification.ClientState == completedSubscription)
                    {
                        // Already handled by LockBatch and Unblock batch.
                        ////notificationEntity.Status = Common.Constants.NotificationStatusCompleted;
                        ////await this.UpdateNotificationInStorage(notificationEntity).ConfigureAwait(false);
                        continue;
                    }
                   
                    this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.NotificationReceived, notification.ClientState), Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));

                        if (!notification.BypassSpoNotification)
                        {
                            // Get Token from Delta table if not first time
                            DriveDeltaEntity driveDelta = await this.GetDriveDeltaForProcessing(notification.ClientState).ConfigureAwait(false);
                            if (driveDelta != null && driveDelta.InProgress)
                            {
                                // If the drive delta is locked, look for a notification from different subscription.
                                inProgressSubscriptions.Add(driveDelta.RowKey);
                                continue;
                            }
                            else if (driveDelta != null)
                            {
                                processChanges = await this.LockForProcessing(driveDelta, notificationEntity, notificationEntities).ConfigureAwait(false);
                            }

                            if (processChanges)
                            {
                                // Process the changes from SharePoint
                                await this.ProcessSharePointChanges(driveDelta, notificationEntity, notification).ConfigureAwait(false);
                                if (notificationEntity.Status == Common.Constants.NotificationStatusCompleted)
                                {
                                    completedSubscription = notification.ClientState;

                                    // Update the other notifications in the batch with same client state as Completed/Idle
                                    // Both for Performance and Scaling Out multiple instances
                                    await this.UnlockBatchNotifications(notificationEntity, notificationEntities);

                                    // Once processed successfully, break out of the loop.
                                    break;
                                }
                            }
                            else
                            {
                                // If the drive delta is unable to lock for processing, look for a notification from different subscription.
                                inProgressSubscriptions.Add(driveDelta?.RowKey);
                            }
                        }
                        else
                        {
                            // Construct batches using data in doc track table.
                            await this.ConstructBatchesUsingDocTrackTable(notificationEntity, notification);
                        }
                    }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            }
        }

        /// <summary>
        /// Constructs the batches using document track table.
        /// </summary>
        /// <param name="notificationEntity">The notification entity.</param>
        /// <param name="notification">The notification.</param>
        private async Task ConstructBatchesUsingDocTrackTable(NotificationEntity notificationEntity, Notification notification)
        {
            bool isError = false;
            try
            {
                await this.UpdateNotificationForProcessing(notificationEntity, true).ConfigureAwait(false);
                string base64DriveId = EncodeBase64(GetDriveFromResource(notification.Resource));
                string filter = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, base64DriveId);
                int maxItemsToPush = ConfigHelper.IntegerReader(Common.Constants.ChangeProcessorMaxQueueItemToPush, 50);
                List<DocumentEntity> documentEntities;
                int? totalCount = 0;
                List<DocumentEntity> itemsToPush;
                TableContinuationToken continuationToken = null;
                var query = new TableQuery<DocumentEntity>().Where(filter);

                do
                {
                    TableQuerySegment<DocumentEntity> tableQuerySegment = null;
                    if (this.docTrackTable != null)
                    {
                        tableQuerySegment = await this.docTrackTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                    }

                    if (tableQuerySegment != null && tableQuerySegment.Results != null)
                    {
                        documentEntities = tableQuerySegment.Results;
                        int index = 0;
                        int count = documentEntities.Count;
                        totalCount += count;
                        while (index < count)
                        {
                            if (index == 0)
                            {
                                itemsToPush = documentEntities.Take(maxItemsToPush).ToList();
                            }
                            else
                            {
                                itemsToPush = documentEntities.Skip(index).Take(maxItemsToPush).ToList();
                            }

                            index += maxItemsToPush;
                            await this.queueHelper.PushMessagesIntoQueue(itemsToPush, maxItemsToPush).ConfigureAwait(false);
                        }
                    }

                    continuationToken = tableQuerySegment?.ContinuationToken;
                } while (continuationToken != null);

                this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.DriveItemCount, totalCount), Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                this.logger.LogInformation(Resource.ProcessSharePointChangesSPCompleted, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                isError = true;
                throw;
            }
            finally
            {
                notificationEntity.Status = isError ? Common.Constants.NotificationStatusFailed : Common.Constants.NotificationStatusCompleted;
                await this.UpdateNotificationInStorage(notificationEntity).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Encodes the base64.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The base 64 encoded text</returns>
        public static string EncodeBase64(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentNullException(nameof(text));
            }

            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(textBytes);
        }

        /// <summary>
        /// Gets the drive from resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>The drive id.</returns>
        private static string GetDriveFromResource(string resource)
        {
            if (string.IsNullOrEmpty(resource))
            {
                return null;
            }

            string[] tokens = resource.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return tokens != null && tokens.Length > 1 ? tokens[1] : null;
        }

        /// <summary>
        /// Resets the delta token.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="hasNextPage">if set to <c>true</c> [has next page].</param>
        /// <param name="deltaUrl">the delta URL for the page.</param>
        /// <returns>The latest delta token retrieved from [the specified response].</returns>
        private static string ResetDeltaToken(IDriveItemDeltaCollectionPage response, out bool hasNextPage, out string deltaUrl)
        {
            var token = "";
            deltaUrl = "";

            if (response.NextPageRequest == null)
            {
                deltaUrl = GraphHelper.GetDeltaLink(response);
                hasNextPage = false;
                token = GraphHelper.GetTokenFromDeltaUrl(deltaUrl);
            }
            else
            {
                var requestUrl = response.NextPageRequest.RequestUrl;
                if (requestUrl.Contains(Constants.Token))
                {
                    deltaUrl = requestUrl;
                    token = deltaUrl.Substring(deltaUrl.IndexOf(Constants.TokeninDeltaUrl) + 7);
                    token = token.Remove(token.Length - 2);
                }
                else
                {
                    token = response.NextPageRequest?.QueryOptions?.FirstOrDefault(
                                x => string.Equals(Constants.Token, WebUtility.UrlDecode(x.Name), StringComparison.InvariantCultureIgnoreCase))?
                                .Value ?? "0";
                    if (token != null && requestUrl != null)
                    {
                        requestUrl = requestUrl.Replace(Constants.DrivesinReqUrl, Constants.DrivesinDeltaUrl);
                        requestUrl = requestUrl.Replace(Constants.RootinReqUrl, Constants.RootinDeltaUrl);

                        deltaUrl = requestUrl.Substring(0, requestUrl.Length - 1) + Constants.TokeninDeltaUrl + token + "')";
                    }
                }
                hasNextPage = true;
            }

            string deltaToken = token;
            return deltaToken;
        }

        /// <summary>
        /// Gets the drive delta for processing.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <returns>Drive delta entity.</returns>
        /// <remarks>Creates a new drive delta entity if not available for the subscription.</remarks>
        private async Task<DriveDeltaEntity> GetDriveDeltaForProcessing(string subscriptionId)
        {
            DriveDeltaEntity driveDelta = await this.GetDeltaTokenIfExists(subscriptionId).ConfigureAwait(false);
            if (driveDelta == null)
            {
                // Add a new record with lock state in the delta table
                driveDelta = await this.AddDriveDeltaEntity(subscriptionId).ConfigureAwait(false);
            }

            return driveDelta;
        }

        /// <summary>
        /// Updates the notification for processing.
        /// </summary>
        /// <param name="notificationEntity">The notification entity.</param>
        /// <param name="increaseAttemptsCount">if set to <c>true</c> [increase attempts count].</param>
        /// <returns>
        ///   <c>true</c> if [the update is successful for] [the specified notificationEntity]; otherwise, <c>false</c>.
        /// </returns>
        private async Task<bool> UpdateNotificationForProcessing(NotificationEntity notificationEntity, bool increaseAttemptsCount)
        {
            if (notificationEntity != null)
            {
                notificationEntity.Status = Common.Constants.NotificationStatusInProgress;
                if (increaseAttemptsCount)
                {
                    notificationEntity.AttemptsCount += 1;
                }

                return await this.UpdateNotificationInStorage(notificationEntity).ConfigureAwait(false);
            }

            return false;
        }

        /// <summary>
        /// Locks for processing.
        /// </summary>
        /// <param name="driveDelta">The drive delta.</param>
        /// <param name="notificationEntity">The notification entity.</param>
        /// <param name="notificationEntities">The notification entities.</param>
        /// <returns>
        ///   <c>true</c> if [the locking is successful for] [the specified driveDelta and notificationEntity]; otherwise, <c>false</c>.
        /// </returns>
        private async Task<bool> LockForProcessing(DriveDeltaEntity driveDelta, NotificationEntity notificationEntity, List<NotificationEntity> notificationEntities)
        {
            bool result = false;
            if (!driveDelta.InProgress)
            {
                // Update the status to lock the subscription
                driveDelta.InProgress = true;
                result = await this.UpdateDriveDeltaStatus(driveDelta).ConfigureAwait(false);
                if (result)
                {
                    this.logger.LogInformation(Resource.LockForProcessingDeltaSuccess, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    try
                    {
                        result = await this.UpdateNotificationForProcessing(notificationEntity, true).ConfigureAwait(false);

                        // Update the other notifications in the batch with same client state as InProgress
                        // Both for Performance and Scaling Out multiple instances
                        await this.LockBatchNotifications(notificationEntity, notificationEntities).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogWarning(Resource.LockForProcessingNotificationError + "\n" + ex.Message + "\n" + ex.StackTrace, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    }

                    // Rollback the lock on Drive delta
                    if (!result)
                    {
                        this.logger.LogInformation(Resource.LockForProcessingDeltaFailed, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                        driveDelta.InProgress = false;
                        result = await this.UpdateDriveDeltaStatus(driveDelta).ConfigureAwait(false);
                        if (!result)
                        {
                            this.logger.LogError(new Exception(Resource.LockForProcessingDeltaReleaseSuccess), Resource.LockForProcessingDeltaReleaseSuccess, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                        }

                        return false;
                    }
                    else
                    {
                        this.logger.LogInformation(Resource.LockForProcessingNotificationSuccess, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    }
                }
                else
                {
                    this.logger.LogInformation(Resource.LockForProcessingNotificationFailed, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                }
            }

            return result;
        }

        /// <summary>
        /// Locks the batch notifications.
        /// </summary>
        /// <param name="notificationEntity">The notification entity.</param>
        /// <param name="notificationEntities">The notification entities.</param>
        /// <returns>The async task.</returns>
        private async Task LockBatchNotifications(NotificationEntity notificationEntity, List<NotificationEntity> notificationEntities)
        {
            Notification currNotification = JsonConvert.DeserializeObject<Notification>(notificationEntity.MessageJson);
            int sucessCount = 0;
            int failedCount = 0;
            foreach (var item in notificationEntities)
            {
                Notification notification = JsonConvert.DeserializeObject<Notification>(item.MessageJson);
                if (notification != null && string.Equals(currNotification.ClientState, notification.ClientState, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await this.UpdateNotificationForProcessing(item, false).ConfigureAwait(false);
                        sucessCount++;
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        this.logger.LogWarning(Resource.LockForProcessingBatchNotificationError + "\n" + ex.Message + "\n" + ex.StackTrace, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    }
                }

                if (sucessCount > 0 && failedCount > 0)
                {
                    this.logger.LogWarning(Resource.LockForProcessingBatchNotificationPartialSuccess, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                }
                else if (sucessCount > 0)
                {
                    this.logger.LogInformation(Resource.LockForProcessingBatchNotificationSuccess, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                }
                else if (failedCount > 0)
                {
                    this.logger.LogWarning(Resource.LockForProcessingBatchNotificationFailed, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                }
            }
        }

        /// <summary>
        /// Locks the batch notifications.
        /// </summary>
        /// <param name="notificationEntity">The notification entity.</param>
        /// <param name="notificationEntities">The notification entities.</param>
        /// <returns>The async task.</returns>
        private async Task UnlockBatchNotifications(NotificationEntity notificationEntity, List<NotificationEntity> notificationEntities)
        {
            try
            {
                var status = notificationEntity.Status == Common.Constants.NotificationStatusCompleted ? Common.Constants.NotificationStatusCompleted : Common.Constants.NotificationStatusIdle;
                Notification currNotification = JsonConvert.DeserializeObject<Notification>(notificationEntity.MessageJson);

                int sucessCount = 0;
                int failedCount = 0;
                foreach (var item in notificationEntities)
                {
                    Notification notification = JsonConvert.DeserializeObject<Notification>(item.MessageJson);
                    if (notification != null && string.Equals(currNotification.ClientState, notification.ClientState, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            item.Status = status;
                            await this.UpdateNotificationInStorage(item).ConfigureAwait(false);
                            sucessCount++;
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            this.logger.LogWarning(Resource.UnlockForProcessingBatchNotificationError + "\n" + ex.Message + "\n" + ex.StackTrace, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                        }
                    }

                    if (sucessCount > 0 && failedCount > 0)
                    {
                        this.logger.LogWarning(Resource.UnlockForProcessingBatchNotificationPartialSuccess, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    }
                    else if (sucessCount > 0)
                    {
                        this.logger.LogInformation(Resource.UnlockForProcessingBatchNotificationSuccess, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    }
                    else if (failedCount > 0)
                    {
                        this.logger.LogWarning(Resource.UnlockForProcessingBatchNotificationFailed, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            }
        }

        /// <summary>
        /// Updates the notification in storage.
        /// </summary>
        /// <param name="notificationEntity">The notification entity.</param>
        /// <returns>
        ///   <c>true</c> if [the update is successful for] [the specified notificationEntity]; otherwise, <c>false</c>.
        /// </returns>
        private async Task<bool> UpdateNotificationInStorage(NotificationEntity notificationEntity)
        {
            if (this.messageStorageTable != null)
            {
                var results = await StorageTableHelper.UpdateItem(this.messageStorageTable, notificationEntity).ConfigureAwait(false);
                if (results != null && (results.HttpStatusCode == (int)HttpStatusCode.OK || results.HttpStatusCode == (int)HttpStatusCode.NoContent))
                {
                    notificationEntity.ETag = results.Etag;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the message from storage.
        /// </summary>
        /// <returns>The list of notification entities from the storage.</returns>
        private async Task<List<NotificationEntity>> GetMessageFromStorage()
        {
            int notificationFetchCount = Convert.ToInt32(this.config.GetConfigValue(Common.Resource.ConfigCPNotificationFetchCount)) < 1 ? 50 : Convert.ToInt32(this.config.GetConfigValue(Common.Resource.ConfigCPNotificationFetchCount));

            if (this.messageStorageTable != null)
            {
                var filterQuery = TableQuery.GenerateFilterCondition(Constants.Status, QueryComparisons.Equal, Common.Constants.NotificationStatusIdle);
                var results = await StorageTableHelper.GetTopTableResultBasedOnQueryFilter<NotificationEntity>(this.messageStorageTable, notificationFetchCount, filterQuery).ConfigureAwait(false);

                if (results != null)
                {
                    this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.MessagesCount, results.Count), Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                }

                return results;
            }
            else
            {
                this.logger.LogWarning(Resource.GetMessageFromStorageNoNotification, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            }

            return default;
        }

        /// <summary>
        /// Processes the share point changes.
        /// </summary>
        /// <param name="driveDelta">The drive delta.</param>
        /// <param name="notificationEntity">The notification entity.</param>
        /// <param name="notification">THe actual notification from Graph API.</param>
        /// <returns>The task status.</returns>
        private async Task ProcessSharePointChanges(DriveDeltaEntity driveDelta, NotificationEntity notificationEntity, Notification notification)
        {
            if (notificationEntity == null)
            {
                return;
            }

            string deltaUrl = driveDelta?.DeltaUrl;
            string deltaToken = driveDelta?.Token;
            bool isError = false;

            try
            {
                int delay = ConfigHelper.IntegerReader(Common.Constants.ChangeProcessorDelay, 5000);
                int maxItemsToPush = ConfigHelper.IntegerReader(Common.Constants.ChangeProcessorMaxQueueItemToPush, 50);
                IDriveItemDeltaCollectionPage response;
                bool hasNextPage;
                List<DriveItem> driveItems;
                int? totalCount = 0;
                List<DriveItem> itemsToPush;

                // Pages of changes from the document library
                do
                {
                    hasNextPage = false;
                    try
                    {
                        response = await this.GetSharePointChanges(deltaToken, notification).ConfigureAwait(false);
                        if (response == null)
                        {
                            break;
                        }

                        this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.DeltaNumberOfChange, response.Count), Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                        driveItems = this.ProcessGraphResponse(response);
                        int count = Convert.ToInt32(driveItems?.Count, CultureInfo.InvariantCulture);
                        totalCount += count;

                        this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.DriveItemsQueueCount, driveItems?.Count), Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                        int index = 0;
                        while (index < count)
                        {
                            if (index == 0)
                            {
                                itemsToPush = driveItems.Take(maxItemsToPush).ToList();
                            }
                            else
                            {
                                itemsToPush = driveItems.Skip(index).Take(maxItemsToPush).ToList();
                            }

                            index += maxItemsToPush;
                            await this.queueHelper.PushMessagesIntoQueue(itemsToPush, maxItemsToPush).ConfigureAwait(false);
                            await Task.Delay(delay).ConfigureAwait(false);
                        }

                        this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.DriveItemsQueueCompleted, count), Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));

                        // Move the Delta Token
                        string nextToken = CPHelper.ResetDeltaToken(response, out hasNextPage, out deltaUrl);
                        if (!string.IsNullOrEmpty(deltaToken) && deltaToken.Equals(nextToken, StringComparison.OrdinalIgnoreCase))
                        {
                            hasNextPage = false;
                        }
                        else
                        {
                            deltaToken = nextToken;
                            // Update the new token to table
                            await this.UpdateIntermediateDeltaToken(driveDelta, deltaUrl, deltaToken).ConfigureAwait(false);
                        }

                        this.logger.LogInformation(Resource.ProcessSharePointChangesDeltaTokenSet, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                        isError = true;
                        break;
                    }

                    // Add delay to prevent throttling
                    if (hasNextPage)
                    {
                        await Task.Delay(delay).ConfigureAwait(true);
                    }
                }
                while (hasNextPage);

                this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.DriveItemCount, totalCount), Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                this.logger.LogInformation(Resource.ProcessSharePointChangesSPCompleted, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                isError = true;
                throw;
            }
            finally
            {
                await this.CompleteMessageProcessing(driveDelta, notificationEntity, deltaUrl, deltaToken, isError).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Completes the message processing.
        /// </summary>
        /// <param name="driveDelta">The drive delta.</param>
        /// <param name="notificationEntity">The notification entity.</param>
        /// <param name="deltaUrl">The delta URL.</param>
        /// <param name="deltaToken">The delta token.</param>
        /// <param name="isError">if set to <c>true</c> [is error].</param>
        /// <returns>Task status.</returns>
        private async Task CompleteMessageProcessing(DriveDeltaEntity driveDelta, NotificationEntity notificationEntity, string deltaUrl, string deltaToken, bool isError)
        {
            try
            {
                // Update the delta url in the delta table
                var result = await this.UpdateDeltaToken(driveDelta, deltaUrl, deltaToken).ConfigureAwait(false);
                if (result.HttpStatusCode == (int)HttpStatusCode.OK || result.HttpStatusCode == (int)HttpStatusCode.NoContent)
                {
                    this.logger.LogInformation(Resource.CompleteMessageProcessingTokenSuccess, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                }
                else
                {
                    this.logger.LogWarning(Resource.CompleteMessageProcessingTokenError, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                }

                notificationEntity.Status = isError ? Common.Constants.NotificationStatusFailed : Common.Constants.NotificationStatusCompleted;
                var updateStatus = await this.UpdateNotificationInStorage(notificationEntity).ConfigureAwait(false);
                if (updateStatus)
                {
                    this.logger.LogInformation(Resource.CompleteMessageProcessingNotificationSuccess, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                }
                else
                {
                    this.logger.LogWarning(Resource.CompleteMessageProcessingNotificationError, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            }
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
        /// Processes the graph response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>The drive Items list.</returns>
        private List<DriveItem> ProcessGraphResponse(IDriveItemDeltaCollectionPage response)
        {
            try
            {
                List<DriveItem> driveItems = new List<DriveItem>();
                IEnumerator<DriveItem> enumerator = response.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    DriveItem item = enumerator.Current;
                    driveItems.Add(item);
                    this.logger.LogInformation(Resource.AddedDriveItem + item.Id, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                }

                return driveItems;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                throw;
            }
        }

        /// <summary>
        /// Gets the share point changes.
        /// </summary>
        /// <param name="deltaToken">The delta token.</param>
        /// <param name="notif">The notification.</param>
        /// <returns>The drive item collection.</returns>
        /// <exception cref="Exception">Maximum retry attempts {retryAttempts}, has be attempted.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Reviewed.")]
        private async Task<IDriveItemDeltaCollectionPage> GetSharePointChanges(string deltaToken, Notification notif)
        {
            int retryAttempts = 0;
            int backoffInterval = 2000;
            int maxRetryAttempts = Convert.ToInt32(this.config.GetConfigValue(Common.Resource.ConfigCPMaxRetryAttempts)) < 1 ? 5 : Convert.ToInt32(this.config.GetConfigValue(Common.Resource.ConfigCPMaxRetryAttempts));
            string fieldsToSelect = string.IsNullOrEmpty(this.config.GetConfigValue(Common.Resource.ConfigCPQueryFields)) ? Constants.DefaultFields : this.config.GetConfigValue(Common.Resource.ConfigCPQueryFields);
            var gclient = this.GetGraphClient();
            string driveId = GetDriveFromResource(notif.Resource);

            // Do while retry attempt is less than retry count
            while (retryAttempts < maxRetryAttempts)
            {
                try
                {
                    // Format: drives / b!8UPd1gqKsk2gQoM1zLvb8JZPxsnCjANFnWsj4pXKj0Jq9JMg34RtQYxA3ffUT1WR / root / delta
                    var response = await GraphHelper.GetDriveItems(gclient, driveId, deltaToken, fieldsToSelect).ConfigureAwait(false);
                    return response;
                }
                catch (ServiceException ex)
                {
                    this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    if (GraphHelper.IsTransientException(ex))
                    {
                        // Make Delta token as empty so that the resync happens.
                        if (GraphHelper.IsResyncRequired(ex))
                        {
                            deltaToken = string.Empty;
                        }

                        // Add delay for retry
                        await Task.Delay(backoffInterval).ConfigureAwait(true);

                        // Add to retry count and increase delay.
                        retryAttempts++;
                        backoffInterval *= 2;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    throw;
                }
            }

            this.logger.LogWarning(string.Format(CultureInfo.InvariantCulture, Resource.MaximumRetryAttemptWarning, retryAttempts), Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            throw new Exception(string.Format(CultureInfo.InvariantCulture, Resource.MaximumRetryAttemptWarning, retryAttempts));
        }

        /// <summary>
        /// Adds the drive delta Entity to the storage.
        /// </summary>
        /// <param name="rowKey">The row key.</param>
        /// <returns>The newly added Drive Delta entity.</returns>
        private async Task<DriveDeltaEntity> AddDriveDeltaEntity(string rowKey)
        {
            if (!string.IsNullOrEmpty(rowKey))
            {
                DriveDeltaEntity newSubscriptionSet = new DriveDeltaEntity()
                {
                    PartitionKey = Constants.DriveDeltaPartitionKey,
                    RowKey = rowKey,
                    InProgress = false,
                    ReceivedTime = DateTime.UtcNow,
                };

                if (this.driveDeltaTable != null)
                {
                    var result = await StorageTableHelper.AddTableItem(this.driveDeltaTable, newSubscriptionSet).ConfigureAwait(false);
                    if (result.HttpStatusCode == (int)HttpStatusCode.OK || result.HttpStatusCode == (int)HttpStatusCode.NoContent)
                    {
                        newSubscriptionSet.ETag = result.Etag;
                        return newSubscriptionSet;
                    }
                }
            }
            else
            {
                this.logger.LogInformation(Resource.AddDriveDeltaEntityNoRowKey, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            }

            return null;
        }

        /// <summary>
        /// Updates the delta token during progress through sharepoint change pages.
        /// </summary>
        /// <param name="driveDelta">The drive delta entity.</param>
        /// <param name="deltatUrl">The delta URL.</param>
        /// <param name="deltaToken">The delta token.</param>
        /// <returns>The table result task.</returns>
        private async Task UpdateIntermediateDeltaToken(DriveDeltaEntity driveDelta, string deltatUrl, string deltaToken)
        {
            if (string.IsNullOrEmpty(deltatUrl))
            {
                this.logger.LogInformation(Resource.UpdateDeltaTokenNoDeltaUrl, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            }

            if (this.driveDeltaTable != null)
            {
                try
                {
                    driveDelta.DeltaUrl = deltatUrl;
                    driveDelta.Token = deltaToken;
                    var result = await StorageTableHelper.UpdateItem(this.driveDeltaTable, driveDelta);
                    if (result.HttpStatusCode == (int)HttpStatusCode.OK || result.HttpStatusCode == (int)HttpStatusCode.NoContent)
                    {
                        driveDelta.ETag = result.Etag;
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                }
            }
        }

        /// <summary>
        /// Updates the delta token.
        /// </summary>
        /// <param name="driveDelta">The drive delta entity.</param>
        /// <param name="deltatUrl">The delta URL.</param>
        /// <param name="deltaToken">The delta token.</param>
        /// <returns>The table result task.</returns>
        private async Task<TableResult> UpdateDeltaToken(DriveDeltaEntity driveDelta, string deltatUrl, string deltaToken)
        {
            this.logger.LogInformation(Resource.UpdateDeltaTokenStart, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            if (string.IsNullOrEmpty(deltatUrl))
            {
                this.logger.LogInformation(Resource.UpdateDeltaTokenNoDeltaUrl, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            }

            if (this.driveDeltaTable != null && this.driveDeltaTransactionTable != null)
            {
                // Add a transaction entity to keep track to delta token changes.
                await this.AddDeltaTransactionEntity(driveDelta, deltatUrl, deltaToken);

                driveDelta.DeltaUrl = deltatUrl;
                driveDelta.Token = deltaToken;
                driveDelta.InProgress = false;
                driveDelta.ETag = Constants.WildCardSearch;
                return await StorageTableHelper.UpdateItem(this.driveDeltaTable, driveDelta);
            }

            return default;
        }

        /// <summary>
        /// Gets the delta token if exists.
        /// </summary>
        /// <param name="rowKey">The row key.</param>
        /// <returns>The delta token.</returns>
        private async Task<DriveDeltaEntity> GetDeltaTokenIfExists(string rowKey)
        {
            if (this.driveDeltaTable != null)
            {
                string filter = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(Constants.PartitionKey, QueryComparisons.Equal, Constants.DriveDeltaPartitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(Constants.RowKey, QueryComparisons.Equal, rowKey));

                var results = await StorageTableHelper.GetTableResultBasedOnQueryFilter<DriveDeltaEntity>(this.driveDeltaTable, filter).ConfigureAwait(false);

                DriveDeltaEntity deltaRow = null;
                if (results != null && results.Count > 0)
                {
                    this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.DeltaTokenResultsCount, results.Count), Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    deltaRow = results.OrderByDescending(r => r.Timestamp).FirstOrDefault();
                    return deltaRow;
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
        private async Task<bool> UpdateDriveDeltaStatus(DriveDeltaEntity deltaRow)
        {
            try
            {
                if (this.driveDeltaTable != null)
                {
                    var result = await StorageTableHelper.UpdateItem(this.driveDeltaTable, deltaRow).ConfigureAwait(false);
                    if (result.HttpStatusCode == (int)HttpStatusCode.OK || result.HttpStatusCode == (int)HttpStatusCode.NoContent)
                    {
                        deltaRow.ETag = result.Etag;
                        return true;
                    }
                    else if (result.HttpStatusCode == (int)HttpStatusCode.Conflict)
                    {
                        this.logger.LogWarning(Resource.UpdateDriveDeltaStatusConflict, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    }
                    else
                    {
                        this.logger.LogWarning(Resource.UpdateDriveDeltaStatusError, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
                    }
                }
            }
            catch (StorageException ex)
            {
                this.logger.LogWarning(Resource.UpdateDriveDeltaStatusConflict + "\n" + ex.Message + "\n" + ex.StackTrace, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.ChangeProcessorWebJob));
            }

            return false;
        }

        /// <summary>
        /// Adds the delta transaction entity.
        /// </summary>
        /// <param name="driveDelta">The drive delta.</param>
        /// <param name="deltaUrl">The delta URL.</param>
        /// <param name="deltaToken">The delta token.</param>
        /// <returns>
        ///   <c>true</c> if [is delta token addition success] [in the specified drive delta]; otherwise, <c>false</c>.
        /// </returns>
        private async Task<bool> AddDeltaTransactionEntity(DriveDeltaEntity driveDelta, string deltaUrl, string deltaToken)
        {
            var driveDeltaTransactionEntity = new DriveDeltaTransactionEntity()
            {
                PartitionKey = driveDelta.RowKey,
                RowKey = Guid.NewGuid().ToString(),
                Agent = nameof(LogCategory.ChangeProcessorWebJob),
                OldDeltaUrl = driveDelta.DeltaUrl,
                OldToken = driveDelta.Token,
                OldTokenTimestamp = driveDelta.Timestamp,
                NewDeltaUrl = deltaUrl,
                NewToken = deltaToken,
            };

            var result = await StorageTableHelper.AddTableItem(this.driveDeltaTransactionTable, driveDeltaTransactionEntity);
            return result.HttpStatusCode == (int)HttpStatusCode.OK || result.HttpStatusCode == (int)HttpStatusCode.NoContent;
        }
    }
}