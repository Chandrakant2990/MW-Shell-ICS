// -----------------------------------------------------------------------
// <copyright file="TablesController.cs" company="Microsoft Corporation">
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
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Shell.SPOCPI.Common;

    /// <summary>
    /// Tables controller API.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
    [Route(Constants.RouteApiControllerAction)]
    [ApiController]
    public class TablesController : ControllerBase
    {
        /// <summary>
        /// The storage table context.
        /// </summary>
        private readonly StorageTableContext storageTableContext;

        /// <summary>
        /// The notification table context.
        /// </summary>
        private readonly StorageTableContext notificationTableContext;

        /// <summary>
        /// The logger component.
        /// </summary>
        private readonly ILoggerComponent loggerComponent;

        /// <summary>
        /// The configuration instance.
        /// </summary>
        private readonly IConfiguration configurationInstance;

        /// <summary>
        /// The notification instance.
        /// </summary>
        private readonly IConfiguration notificationInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="TablesController"/> class.
        /// </summary>
        /// <param name="configurationInstance">The configuration instance.</param>
        /// <param name="loggerComponent">The logger component.</param>
        /// <param name="notificationInstance">The notification instance.</param>
        public TablesController(IConfiguration configurationInstance, ILoggerComponent loggerComponent, IConfiguration notificationInstance)
        {
            this.loggerComponent = loggerComponent;
            this.configurationInstance = configurationInstance;
            this.notificationInstance = notificationInstance;

            this.storageTableContext = new StorageTableContext(
                KeyVaultHelper.GetSecret(configurationInstance?.GetConfigValue(Common.Resource.ConfigWebHooksStoreConnectionString))?.Result?.ToString(),
                Common.Constants.ConfigTableName);

            this.notificationTableContext = new StorageTableContext(
                KeyVaultHelper.GetSecret(notificationInstance?.GetConfigValue(Common.Resource.ConfigCPDriveDeltaStoreConnString))?.Result?.ToString(),
                Common.Constants.NotificationTableName);
        }

        /// <summary>
        /// Configurations this instance.
        /// </summary>
        /// <returns>Returns all the items in the configuration table.</returns>
        [HttpGet]
        [Route(Constants.RouteAction)]
        public async Task<ActionResult<List<ConfigurationEntity>>> Configuration()
        {
            try
            {
                int draw = Convert.ToInt32((this.HttpContext.Request.Query[Constants.SearchDraw].ToString()?.Length == 0) ? "0" : this.HttpContext.Request.Query[Constants.SearchDraw].ToString(), CultureInfo.InvariantCulture);
                string orderColumn = this.HttpContext.Request.Query[Constants.SearchOrderBy].ToString();
                string orderDir = this.HttpContext.Request.Query[Constants.SearchOrderDirection].ToString();
                if (!string.IsNullOrEmpty(orderColumn) && !string.IsNullOrEmpty(orderDir))
                {
                    orderColumn = orderColumn.Replace(Constants.SearchDocumentParameter, string.Empty, StringComparison.InvariantCulture);
                    orderColumn = char.ToUpper(orderColumn[0], CultureInfo.InvariantCulture) + orderColumn.Substring(1);
                    orderColumn = string.Format(CultureInfo.InvariantCulture, "{0} {1}", orderColumn, orderDir);
                }

                var tableResults = await StorageTableHelper.GetTableResultBasedOnQueryFilter<ConfigurationEntity>(this.storageTableContext.Table, null).ConfigureAwait(false);
                var count = tableResults.Count;
                return this.Ok(new { data = tableResults, draw, recordsFiltered = count, recordsTotal = count });
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

        /// <summary>
        /// Get all the items from notification storage table.
        /// </summary>
        /// <returns>Returns all the items in the notification storage table.</returns>
        [HttpGet]
        [Route(Constants.RouteAction)]
        public async Task<ActionResult<List<NotificationEntity>>> Notification()
        {
            try
            {
                int draw = Convert.ToInt32((this.HttpContext.Request.Query[Constants.SearchDraw].ToString()?.Length == 0) ? "0" : this.HttpContext.Request.Query[Constants.SearchDraw].ToString(), CultureInfo.InvariantCulture);

                var lastSixHoursTimestamp = StorageTableHelper.CreateFilterQueryDate(Constants.Timestamp, Constants.GreaterThanOrEqual, DateTimeOffset.UtcNow.AddHours(Constants.TimestampFilterHours));
                var tableResults = await StorageTableHelper.GetTopTableResultBasedOnQueryFilter<NotificationEntity>(this.notificationTableContext.Table, topCount: 999, filter: lastSixHoursTimestamp).ConfigureAwait(false);
                var count = tableResults.Count;
                return this.Ok(new { data = tableResults, draw, recordsFiltered = count, recordsTotal = count });
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

        /// <summary>
        /// Adds the configuration.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Added configuration entity.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route(Constants.RouteAction)]
        public async Task<ActionResult> AddConfiguration(ConfigurationEntity entity)
        {
            try
            {
                var tableResult = await StorageTableHelper.AddOrReplaceTableItem(this.storageTableContext.Table, entity, Constants.ConfigTableName, addToCache: true).ConfigureAwait(false);
                return this.Ok(tableResult);
            }
            catch (Exception ex)
            {
                this.loggerComponent.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex);
            }
        }

        /// <summary>
        /// Adds the notification.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Added notification entity.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        [HttpPost]

        [ValidateAntiForgeryToken]
        [Route(Constants.RouteAction)]
        public async Task<ActionResult> AddNotification(NotificationEntity entity)
        {
            try
            {
                var tableResult = await StorageTableHelper.AddOrReplaceTableItem(this.notificationTableContext.Table, entity, Constants.NotificationTableName, addToCache: false).ConfigureAwait(false);
                return this.Ok(tableResult);
            }
            catch (Exception ex)
            {
                this.loggerComponent.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex);
            }
        }

        /// <summary>
        /// Deletes the configuration.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Deleted entity.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        [HttpDelete]
        [Route(Constants.RouteAction)]
        public async Task<ActionResult> DeleteConfiguration(ConfigurationEntity entity)
        {
            try
            {
                var tableResult = await StorageTableHelper.DeleteTableItem(this.storageTableContext.Table, entity).ConfigureAwait(false);
                return this.Ok(tableResult);
            }
            catch (Exception ex)
            {
                this.loggerComponent.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex);
            }
        }

        /// <summary>
        /// Deletes the notification.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Deleted entity.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        [HttpDelete]
        [Route(Constants.RouteAction)]
        public async Task<ActionResult> DeleteNotification(NotificationEntity entity)
        {
            try
            {
                var tableResult = await StorageTableHelper.DeleteTableItem(this.notificationTableContext.Table, entity).ConfigureAwait(false);
                return this.Ok(tableResult);
            }
            catch (Exception ex)
            {
                this.loggerComponent.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
                return this.BadRequest(ex);
            }
        }
    }
}