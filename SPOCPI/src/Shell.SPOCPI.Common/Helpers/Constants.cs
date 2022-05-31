// -----------------------------------------------------------------------
// <copyright file="Constants.cs" company="Microsoft Corporation">
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
    /// <summary>
    /// The Constants class.
    /// </summary>
    public static class Constants
    {
        #region Application Insights Constant

        /// <summary>
        /// The application insights application name.
        /// </summary>
        public const string ApplicationName = "ApplicationName";

        /// <summary>
        /// The application insights module name.
        /// </summary>
        public const string ModuleName = "ModuleName";

        /// <summary>
        /// The application insights category.
        /// </summary>
        public const string AppInsightsCategory = "Category";

        /// <summary>
        /// The application insights message.
        /// </summary>
        public const string AppInsightsMessage = "Message";

        #endregion Application Insights Constant

        #region Table Names

        /// <summary>
        /// The configuration table name.
        /// </summary>
        public const string ConfigTableName = "Configuration";

        /// <summary>
        /// The notification storage table name.
        /// </summary>
        public const string NotificationTableName = "NotificationStorage";

        /// <summary>
        /// The document tracking table name.
        /// </summary>
        public const string DocumentTrackingTableName = "DocumentTracking";

        /// <summary>
        /// The notification storage table name.
        /// </summary>
        public const string NotificationStorageTableName = "NotificationStorage";

        /// <summary>
        /// The subscriptions table name.
        /// </summary>
        public const string SubscriptionsTableName = "Subscriptions";

        #endregion Table Names

        #region Connection Strings

        /// <summary>
        /// The Redis connection string.
        /// </summary>
        public const string RedisConnectionString = "ConnectionStrings:RedisConnectionString";

        /// <summary>
        /// The web hooks store connection string.
        /// </summary>
        public const string WebHooksStoreConnectionString = "WebHooksStoreConnectionString";

        /// <summary>
        /// The configuration table connection string name.
        /// </summary>
        public const string ConfigTableConnectionStringName = "ConnectionStrings:ConfigurationConnectionString";

        /// <summary>
        /// The change processor service bus connection name.
        /// </summary>
        public const string ChangeProcessorServiceBusConnectionName = "CPServiceBusConnectionString";

        /// <summary>
        /// The document tracking connection string name.
        /// </summary>
        public const string DocumentTrackingConnectionStringName = "DocTrackConnectionString";

        /// <summary>
        /// The notification receiver service bus connection name.
        /// </summary>
        public const string NotificationReceiverServiceBusConnectionName = "ServiceBusConnection";

        #endregion Connection Strings

        #region queueNames

        /// <summary>
        /// The queue name1.
        /// </summary>
        public const string QueueName1 = "QueueName1";

        /// <summary>
        /// The queue name2.
        /// </summary>
        public const string QueueName2 = "QueueName2";

        /// <summary>
        /// The queue name3.
        /// </summary>
        public const string QueueName3 = "QueueName3";

        /// <summary>
        /// The queue name4.
        /// </summary>
        public const string QueueName4 = "QueueName4";

        /// <summary>
        /// The queue name5.
        /// </summary>
        public const string QueueName5 = "QueueName5";

        #endregion

        #region Residual Names

        /// <summary>
        /// The web hooks queue name.
        /// </summary>
        public const string WebHooksQueueName = "webhooksnotificationqueue";

        /// <summary>
        /// The change processor function name.
        /// </summary>
        public const string ChangeProcessorFunctionName = "ChangeProcessor";

        /// <summary>
        /// The net core environment variable name.
        /// </summary>
        public const string NetCoreEnvironmentVariableName = "ASPNETCORE_ENVIRONMENT";

        /// <summary>
        /// The change processor output queue name.
        /// </summary>
        public const string ChangeProcessorOutputQueueName = "CPOutputQueueName";

        /// <summary>
        /// The change processor input queue name.
        /// </summary>
        public const string ChangeProcessorInputQueueName = "CPInputQueueName";

        /// <summary>
        /// The environment name.
        /// </summary>
        public const string EnvironmentName = "Development";

        /// <summary>
        /// The JSON file name.
        /// </summary>
        public const string JSONFileName = "appsettings.json";

        /// <summary>
        /// The notification receiver function name.
        /// </summary>
        public const string NotificationReceiverFunctionName = "NotificationReceiver";

        /// <summary>
        /// The populate tracking function name.
        /// </summary>
        public const string PopulateTrackingFunctionName = "PopulateTracking";

        /// <summary>
        /// The change processor output queue.
        /// </summary>
        public const string ChangeProcessOutputQueue = "changeprocessoutputqueue";

        /// <summary>
        /// The forward slash.
        /// </summary>
        public const string ForwardSlash = "/";

        /// <summary>
        /// The subscription queue name.
        /// </summary>
        public const string SubscriptionQueueName = "subscriptionqueue";

        #endregion Residual Names

        #region Status Names

        /// <summary>
        /// The subscribed.
        /// </summary>
        public const string Subscribed = "Subscribed";

        /// <summary>
        /// The not subscribed.
        /// </summary>
        public const string NotSubscribed = "Not Subscribed";

        /// <summary>
        /// The expiration date time.
        /// </summary>
        public const string ExpirationDateTime = "ExpirationDateTime";

        /// <summary>
        /// Subscription renewal day.
        /// </summary>
        public const double SubscriptionRenewDay = 15;

        /// <summary>
        /// The status.
        /// </summary>
        public const string Status = "Status";

        /// <summary>
        /// The delete.
        /// </summary>
        public const string Delete = "Deleted";

        /// <summary>
        /// The exception.
        /// </summary>
        public const string Exception = "Exception";

        /// <summary>
        /// The stack trace.
        /// </summary>
        public const string StackTrace = "Stack Trace";

        /// <summary>
        /// The QueuedForDeletion.
        /// </summary>
        public const string QueuedForDeletion = "Queued for Deletion";

        /// <summary>
        /// Disabled string constant.
        /// </summary>
        public const string Disabled = "Disabled";

        /// <summary>
        /// The subscription disabled.
        /// </summary>
        public const string SubscriptionDisabled = "SubscriptionDisabled";

        /// <summary>
        /// The notification status [in progress].
        /// </summary>
        public const string NotificationStatusInProgress = "InProgress";

        /// <summary>
        /// The notification status [idle].
        /// </summary>
        public const string NotificationStatusIdle = "Idle";

        /// <summary>
        /// The notification status [completed].
        /// </summary>
        public const string NotificationStatusCompleted = "Completed";

        /// <summary>
        /// The notification status [failed].
        /// </summary>
        public const string NotificationStatusFailed = "Failed";

        #endregion Status Names

        #region Projects Constant

        /// <summary>
        /// The threshold count that determines number of times to retry an operation before throwing an exception.
        /// </summary>
        public const string RetryThresholdCount = "RetryThresholdCount";

        /// <summary>
        /// The threshold count that determines number of milliseconds to wait before doing a retry.
        /// </summary>
        public const string RetryBackoffIntervalTime = "RetryBackoffIntervalTime";

        /// <summary>
        /// The authority URL.
        /// </summary>
        public const string AuthorityUrl = "https://login.microsoftonline.com/{0}";

        /// <summary>
        /// The graph resource URL.
        /// </summary>
        public const string GraphResourceUrl = "https://graph.microsoft.com";

        /// <summary>
        /// The graph version.
        /// </summary>
        public const string GraphVersion = "v1.0";

        /// <summary>
        /// The subscriptions entity set URL.
        /// </summary>
        public const string SubscriptionsEntitySetUrl = GraphResourceUrl + "/" + GraphVersion + "/subscriptions";

        /// <summary>
        /// The subscription URL template.
        /// </summary>
        public const string SubscriptionUrlTemplate = GraphResourceUrl + "/" + GraphVersion + "/subscriptions/{0}";

        /// <summary>
        /// The site drives graph template.
        /// </summary>
        public const string SiteDrivesGraphTemplate = GraphResourceUrl + "/" + GraphVersion + "/sites/{0}/drives";

        /// <summary>
        /// The web hook notification URL.
        /// </summary>
        public const string WebhookNotificationUrl = "WebhookNotificationUrl";

        /// <summary>
        /// The AAD application client identifier.
        /// </summary>
        public const string AADAppClientId = "AADAppClientId";

        /// <summary>
        /// The AAD application client secret.
        /// </summary>
        public const string AADAppClientSecret = "AADAppClientSecret";

        /// <summary>
        /// The tenant name.
        /// </summary>
        public const string TenantName = "TenantName";

        /// <summary>
        /// The subscription string value.
        /// </summary>
        public const string SubscriptionStringValue = "Subscription";

        /// <summary>
        /// The SPOCPI application name.
        /// </summary>
        public const string SPOCPI = "SPOCPI";

        /// <summary>
        /// The configuration key.
        /// </summary>
        public const string ConfigurationKey = "Configuration";

        /// <summary>
        /// The is Redis cache enabled.
        /// </summary>
        public const string IsRedisCacheEnabled = "IsRedisCacheEnabled";

        /// <summary>
        /// The ExpirationDateTimeUpperLimit constant.
        /// </summary>
        public const int ExpirationDateTimeUpperLimit = 7;

        /// <summary>
        /// The LocalNotificationUrl constant.
        /// </summary>
        public const string LocalNotificationUrl = "http://localhost:7071/api/NotificationReceiver";

        /// <summary>
        /// The default fields.
        /// </summary>
        public const string DefaultFields = "content.downloadUrl,id,name,deleted,createdDateTime,lastModifiedDateTime,webUrl,cTag,eTag,size,parentReference,file,folder,sharepointIds,location";

        /// <summary>
        /// The drive delta partition key.
        /// </summary>
        public const string DriveDeltaPartitionKey = "DriveDelta";

        /// <summary>
        /// The change processor maximum queue item to push.
        /// </summary>
        public const string ChangeProcessorDelay = "CPPageProcessingDelay";

        /// <summary>
        /// The change processor maximum queue item to push.
        /// </summary>
        public const string ChangeProcessorMaxQueueItemToPush = "CPMaxQueueItemsToPush";

        /// <summary>
        /// The caching disabled.
        /// </summary>
        public const string CachingDisabled = "CachingDisabled";

        /// <summary>
        /// The false.
        /// </summary>
        public const string False = "False";

        /// <summary>
        /// The true.
        /// </summary>
        public const string True = "true";

        /// <summary>
        /// The greater than or equal.
        /// </summary>
        public const string GreaterThanOrEqual = "GREATERTHANOREQUAL";

        /// <summary>
        /// The greater than short.
        /// </summary>
        public const string GreaterThanShort = "ge";

        /// <summary>
        /// The less than.
        /// </summary>
        public const string LessThan = "LESSTHAN";

        /// <summary>
        /// The equal.
        /// </summary>
        public const string Equal = "EQUAL";

        /// <summary>
        /// The equal.
        /// </summary>
        public const string SmallEqual = "equal";

        /// <summary>
        /// The and.
        /// </summary>
        public const string And = "AND";

        /// <summary>
        /// The and in lower case.
        /// </summary>
        public const string AndToLower = "and";

        /// <summary>
        /// The or.
        /// </summary>
        public const string Or = "OR";

        /// <summary>
        /// The small greater than or equal.
        /// </summary>
        public const string SmallGreaterThanOrEqual = "greaterthanorequal";

        /// <summary>
        /// The small less than.
        /// </summary>
        public const string SmallLessThan = "lessthan";

        /// <summary>
        /// Small letter short version of less than.
        /// </summary>
        public const string SmallLessThanShort = "lt";

        /// <summary>
        /// The validation token.
        /// </summary>
        public const string ValidationToken = "validationToken";

        /// <summary>
        /// The validation token.
        /// </summary>
        public const string Token = "token";

        /// <summary>
        /// The TokeninDeltaUrl.
        /// </summary>
        public const string TokeninDeltaUrl = "token='";

        /// <summary>
        /// The DrivesinReqUrl.
        /// </summary>
        public const string DrivesinReqUrl = "drives/";

        /// <summary>
        /// The DrivesinDeltaUrl.
        /// </summary>
        public const string DrivesinDeltaUrl = "drives('";

        /// <summary>
        /// The RootinReqUrl.
        /// </summary>
        public const string RootinReqUrl = "/root";

        /// <summary>
        /// The RootinDeltaUrl.
        /// </summary>
        public const string RootinDeltaUrl = "')/root";

        /// <summary>
        /// The content type.
        /// </summary>
        public const string ContentType = "application/json";

        /// <summary>
        /// The get.
        /// </summary>
        public const string Get = "get";

        /// <summary>
        /// The post.
        /// </summary>
        public const string Post = "post";

        /// <summary>
        /// The star.
        /// </summary>
        public const string WildCardSearch = "*";

        /// <summary>
        /// Maximum number of items that can be processed in a single batch.
        /// </summary>
        public const int TableServiceBatchMaximumOperations = 100;

        /// <summary>
        /// The Admin role.
        /// </summary>
        public const string AdminRole = "admin";

        /// <summary>
        /// The Operator role.
        /// </summary>
        public const string OperatorRole = "operator";

        /// <summary>
        /// The admin and operator role.
        /// </summary>
        public const string AdminAndOperatorRole = "admin,operator";

        /// <summary>
        /// The Route action.
        /// </summary>
        public const string RouteAction = "[action]";

        /// <summary>
        /// Route <c>Api</c> Controller action constant.
        /// </summary>
        public const string RouteApiControllerAction = "api/[controller]";

        /// <summary>
        /// The Auto complete suggestions constant.
        /// </summary>
        public const string AutocompleteSuggestions = "AutocompleteSuggestions";

        /// <summary>
        /// The Activate Subscription constant.
        /// </summary>
        public const string ActivateSubscription = "ActivateSubscription";

        /// <summary>
        /// The search constant.
        /// </summary>
        public const string Search = "Search";

        /// <summary>
        /// The tracking search constant.
        /// </summary>
        public const string TrackingSearch = "TrackingSearch";

        /// <summary>
        /// Azure Search Constant.
        /// </summary>
        public const string SearchValue = "search[value]";

        /// <summary>
        /// Azure Search Constant.
        /// </summary>
        public const string SearchDraw = "draw";

        /// <summary>
        /// Azure Search Constant.
        /// </summary>
        public const string SearchStart = "start";

        /// <summary>
        /// Azure Search Constant.
        /// </summary>
        public const string SearchLength = "length";

        /// <summary>
        /// Azure Search Constant.
        /// </summary>
        public const string SearchOrderBy = "orderBy";

        /// <summary>
        /// Azure Search Constant.
        /// </summary>
        public const string SearchDriveId = "driveId";

        /// <summary>
        /// Azure Search Constant.
        /// </summary>
        public const string SearchPartitionKey = "partitionKey";

        /// <summary>
        /// Azure Search Constant.
        /// </summary>
        public const string SearchRowKey = "rowKey";

        /// <summary>
        /// Azure Search Constant.
        /// </summary>
        public const string SearchDriveItemId = "driveItemId";

        /// <summary>
        /// Azure Search Constant.
        /// </summary>
        public const string SearchBase64DriveId = "base64DriveId";

        /// <summary>
        /// Azure Search Constant.
        /// </summary>
        public const string SearchOrderDirection = "orderDir";

        /// <summary>
        /// The Done constant.
        /// </summary>
        public const string Done = "done";

        /// <summary>
        /// The document parameter used in the UI.
        /// </summary>
        public const string SearchDocumentParameter = "document.";

        /// <summary>
        /// The Equal Short used in the UI.
        /// </summary>
        public const string EqualShort = "eq";

        /// <summary>
        /// The Time stamp filter hours.
        /// </summary>
        public const double TimestampFilterHours = -6;

        /// <summary>
        /// The Drive constant.
        /// </summary>
        public const string Drive = "drives";

        /// <summary>
        /// The root constant.
        /// </summary>
        public const string Root = "root";

        /// <summary>
        /// The anti forgery header.
        /// </summary>
        public const string AntiForgeryHeader = "X-CSRF-TOKEN-SPOCPI";

        #endregion Projects Constant

        #region Table Column Names

        /// <summary>
        /// The partition key.
        /// </summary>
        public const string PartitionKey = "PartitionKey";

        /// <summary>
        /// The row key.
        /// </summary>
        public const string RowKey = "RowKey";

        /// <summary>
        /// The IsActive constant.
        /// </summary>
        public const string IsActive = "IsActive";

        /// <summary>
        /// The DriveId constant.
        /// </summary>
        public const string DriveId = "DriveId";

        /// <summary>
        /// The subscription identifier.
        /// </summary>
        public const string SPOSubscriptionId = "SPOSubscriptionId";

        /// <summary>
        /// The description.
        /// </summary>
        public const string Description = "Description";

        /// <summary>
        /// The creation date time.
        /// </summary>
        public const string CreationDateTime = "CreationDateTime";

        /// <summary>
        /// The output queue.
        /// </summary>
        public const string OutputQueue = "OutputQueue";

        /// <summary>
        /// The automatic index.
        /// </summary>
        public const string AutoIndex = "AutoIndex";

        /// <summary>
        /// The include folder relative path.
        /// </summary>
        public const string IncludeFolderRelativePath = "IncludeFolderRelativePath";

        /// <summary>
        /// The LibraryURL constant.
        /// </summary>
        public const string LibraryURL = "LibraryUrl";

        /// <summary>
        /// The name.
        /// </summary>
        public const string Name = "Name";

        /// <summary>
        /// The site URL.
        /// </summary>
        public const string SiteUrl = "SiteUrl";

        /// <summary>
        /// The list identifier.
        /// </summary>
        public const string ListId = "ListId";

        /// <summary>
        /// The document e tag.
        /// </summary>
        public const string DocumentETag = "DocumentETag";

        /// <summary>
        /// The document e tag change.
        /// </summary>
        public const string DocumentETagChange = "DocumentETagChange";

        /// <summary>
        /// The document c tag.
        /// </summary>
        public const string DocumentCTag = "DocumentCTag";

        /// <summary>
        /// The document c tag change.
        /// </summary>
        public const string DocumentCTagChange = "DocumentCTagChange";

        /// <summary>
        /// The web URL.
        /// </summary>
        public const string WebUrl = "WebUrl";

        /// <summary>
        /// The list item identifier.
        /// </summary>
        public const string ListItemId = "ListItemId";

        /// <summary>
        /// The list item unique identifier.
        /// </summary>
        public const string ListItemUniqueId = "ListItemUniqueId";

        /// <summary>
        /// The site identifier.
        /// </summary>
        public const string SiteId = "SiteId";

        /// <summary>
        /// The web identifier.
        /// </summary>
        public const string WebId = "WebId";

        /// <summary>
        /// The timestamp.
        /// </summary>
        public const string Timestamp = "Timestamp";

        /// <summary>
        /// The extension.
        /// </summary>
        public const string Extension = "Extension";

        /// <summary>
        /// The Folder boolean.
        /// </summary>
        public const string IsFolder = "IsFolder";

        /// <summary>
        /// The Parent Folder Url.
        /// </summary>
        public const string ParentFolderUrl = "ParentFolderUrl";

        /// <summary>
        /// The Expiration Day max value.
        /// </summary>
        public const int ExpirationDaysMax = 21;

        #endregion Table Column Names

    }
}