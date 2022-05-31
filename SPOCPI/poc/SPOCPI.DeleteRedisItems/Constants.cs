using System;
using System.Collections.Generic;
using System.Text;

namespace SPOCPI.DeleteRedisItems
{
    public class Constants
    {
        //public static List<string> fileExtensions = new List<string> { ".Jpeg", ".Jpg", ".Jpe", ".Png", ".Pnz", ".Tiff", ".Tif", ".JPEG", ".JPG", ".JPE", ".PNG", ".PNZ", ".TIFF", ".TIF" };
        public static List<string> fileExtensions = new List<string> { ".Jpg", ".Tif", ".JPEG", ".JPG", ".PNG", ".TIFF", ".TIF" };

        public static string PendingStatus = "Pending";

        public static string ProcessedStatus = "Done";

        /// <summary>
        /// The star.
        /// </summary>
        public const string WildCardSearch = "*";

        public const string DocumentTrackingTableName = "DocumentTracking";

        /// <summary>
        /// The partition key.
        /// </summary>
        public const string PartitionKey = "PartitionKey";

        /// <summary>
        /// The row key.
        /// </summary>
        public const string RowKey = "RowKey";

        /// <summary>
        /// The BlobPath.
        /// </summary>
        public const string BlobPath = "BlobPath";

        /// <summary>
        /// The DriveItemId.
        /// </summary>
        public const string DriveItemId = "DriveItemId";

        /// <summary>
        /// The FileExtension.
        /// </summary>
        public const string FileExtension = "FileExtension";

        /// <summary>
        /// The IsArchive.
        /// </summary>
        public const string IsArchive = "IsArchive";

        /// <summary>
        /// The OperationData.
        /// </summary>
        public const string OperationData = "OperationData";

        /// <summary>
        /// The OperationStatus.
        /// </summary>
        public const string OperationStatus = "OperationStatus";

        /// <summary>
        /// The PipelineRunId.
        /// </summary>
        public const string PipelineRunId = "PipelineRunId";

        /// <summary>
        /// The ErrorMessage.
        /// </summary>
        public const string ErrorMessage = "ErrorMessage";

        /// <summary>
        /// The IsActive constant.
        /// </summary>
        public const string IsActive = "IsActive";

        /// <summary>
        /// The DriveId constant.
        /// </summary>
        public const string DriveId = "DriveId";

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

        public const string OperationIDInvalidMessage = "Operation ID is invalid, expired or the results matching this operationId have been deleted.";
        public const string RequestAlreadySentMessage = "The request message was already sent. Cannot send the same request message multiple times.";

        /// <summary>
        /// Awaiting response.
        /// </summary>
        public const string AwaitingResponse = "AwaitingResponse";

        public const string MetricNameValue = "ReprocessingFailedOCRItems";

        public const string ApplicationNameValue = "SPOCPI";

        public const string ModuleNameValue = "DeletingRedisData";

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
    }
}
