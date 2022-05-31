// -----------------------------------------------------------------------
// <copyright file="TrackingController.cs" company="Microsoft Corporation">
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
    using System.Threading.Tasks;
    using Azure.Search.Documents.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Shell.SPOCPI.Common;
    using Shell.SPOCPI.Common.Helpers;
    using Shell.SPOCPI.WebHooksManager.UI.Models;
    using Constants = Shell.SPOCPI.Common.Constants;

    /// <summary>
    /// Tracking Controller API class.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
    [Route(Constants.RouteApiControllerAction)]
    [ApiController]
    public class TrackingController : ControllerBase
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
        private readonly ILoggerComponent loggerComponent;

        /// <summary>
        /// The configuration instance.
        /// </summary>
        private readonly IConfiguration configurationInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingController" /> class.
        /// </summary>
        /// <param name="configurationInstance">The configuration instance.</param>
        /// <param name="loggerComponent">The logger component.</param>
        public TrackingController(IConfiguration configurationInstance, ILoggerComponent loggerComponent)
        {
            this.loggerComponent = loggerComponent;
            this.configurationInstance = configurationInstance;
            this.storageTableContext = new StorageTableContext(
                KeyVaultHelper.GetSecret(configurationInstance?.GetConfigValue(Resource.ConfigWebHooksStoreConnectionString))?.Result?.ToString(),
                Constants.SubscriptionsTableName);

            string searchUpdateKey = KeyVaultHelper.GetSecret(this.configurationInstance?.GetConfigValue(Resource.ConfigSearchUpdateKey))?.Result?.ToString();
            this.searchHelperClient = new SearchHelperClient(
                configurationInstance?.GetConfigValue(Resource.ConfigSearchServiceName),
                configurationInstance?.GetConfigValue(Resource.ConfigTrackingIndexName),
                searchUpdateKey);
        }

        /// <summary>
        /// Gets this instance.
        /// </summary>
        /// <returns>Action Result.</returns>
        [HttpGet]
        [Route("")]
        public ActionResult<string> Get()
        {
            return this.Ok(Constants.Done);
        }

        /// <summary>
        /// Searches the specified query text.
        /// </summary>
        /// <param name="queryText">The query text.</param>
        /// <returns>Document Search Result.</returns>
        [HttpGet(Name = Constants.TrackingSearch)]
        [Route(Constants.RouteAction)]
        public async Task<ActionResult<SearchResults<DocumentEntity>>> Search(string queryText = "*")
        {
            try
            {
                queryText = this.HttpContext.Request.Query[Constants.SearchValue].ToString();
                var pattern = this.configurationInstance.GetConfigValue(Resource.ConfigHTMLInputValidation);
                if (UserInputValidation.CheckValidInput(queryText, pattern))
                {
                    int draw = Convert.ToInt32(this.HttpContext.Request.Query[Constants.SearchDraw].ToString(), CultureInfo.InvariantCulture);
                    int start = Convert.ToInt32(this.HttpContext.Request.Query[Constants.SearchStart].ToString(), CultureInfo.InvariantCulture);
                    int length = Convert.ToInt32(this.HttpContext.Request.Query[Constants.SearchLength].ToString(), CultureInfo.InvariantCulture);
                    string orderColumn = this.HttpContext.Request.Query[Constants.SearchOrderBy].ToString();
                    string driveId = this.HttpContext.Request.Query[Constants.SearchDriveId].ToString();
                    string orderDir = this.HttpContext.Request.Query[Constants.SearchOrderDirection].ToString();

                    if (!string.IsNullOrEmpty(orderColumn) && !string.IsNullOrEmpty(orderDir))
                    {
                        orderColumn = orderColumn.Replace(Constants.SearchDocumentParameter, string.Empty, StringComparison.InvariantCulture);
                        orderColumn = char.ToUpper(orderColumn[0], CultureInfo.InvariantCulture) + orderColumn.Substring(1);
                        orderColumn = string.Format(CultureInfo.InvariantCulture, "{0} {1}", orderColumn, orderDir);
                    }

                    driveId = !string.IsNullOrEmpty(driveId) ?
                     $"{Constants.PartitionKey} {Constants.EqualShort} '{StorageTableHelper.GetBase64String(driveId)}'" : null;
                    IList<string> selectProperties = new[] { Constants.PartitionKey, Constants.RowKey, Constants.Name, Constants.SiteUrl, Constants.ListId, Constants.DocumentETag, Constants.DocumentETagChange, Constants.DocumentCTag, Constants.DocumentCTagChange, Constants.WebUrl, Constants.ListItemId, Constants.ListItemUniqueId, Constants.SiteId, Constants.WebId, Constants.Timestamp, Constants.Extension, Constants.IsFolder, Constants.ParentFolderUrl };
                    var searchResults = await SearchHelper.Search<DocumentEntity>(this.searchHelperClient.SearchClient, queryText, select: selectProperties, orderBy: orderColumn, skip: start, top: length, filter: driveId).ConfigureAwait(true);
                    var resultObject = new DataTableObject<DocumentEntity>(searchResults.GetResults(), draw, (int)searchResults.TotalCount, (int)searchResults.TotalCount);
                    return this.Ok(resultObject);
                }
                else
                {
                    return this.BadRequest(Resource.InvalidUserInput);
                }
            }
            catch (ArgumentNullException ex)
            {
                this.loggerComponent.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.loggerComponent.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}