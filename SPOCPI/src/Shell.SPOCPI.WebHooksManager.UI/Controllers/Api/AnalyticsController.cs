// -----------------------------------------------------------------------
// <copyright file="AnalyticsController.cs" company="Microsoft Corporation">
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
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.ServiceBus;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Shell.SPOCPI.Common;
    using Shell.SPOCPI.Common.Helpers;
    using Shell.SPOCPI.WebHooksManager.UI.Entities;
    using Shell.SPOCPI.WebHooksManager.UI.Models;
    using Constants = Shell.SPOCPI.Common.Constants;
    using Resource = Shell.SPOCPI.WebHooksManager.UI.Resource;

    /// <summary>
    /// Subscriptions controller.
    /// </summary>
    /// <seealso cref="ControllerBase" />
    [Route(Constants.RouteApiControllerAction)]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        /// <summary>
        /// The azure common metrics search client.
        /// </summary>
        private readonly SearchHelperClient azureCommonMetricsSearchHelperClient;

        /// <summary>
        /// The azure library metrics search client.
        /// </summary>
        private readonly SearchHelperClient azureMetricsSearchHelperClient;

        /// <summary>
        /// The sharepoint metrics search client.
        /// </summary>
        private readonly SearchHelperClient spMetricsSearchHelperClient;

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
        /// The search update key - used to perform read and write operations to search index.
        /// </summary>
        private readonly string searchUpdateKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalyticsController" /> class.
        /// </summary>
        /// <param name="config">The configuration instance.</param>
        /// <param name="logger">The logger component.</param>
        public AnalyticsController(IConfiguration config, ILoggerComponent logger)
        {
            this.logger = logger;
            this.config = config;
            this.storageTableContext = new StorageTableContext(
                KeyVaultHelper.GetSecret(config?.GetConfigValue(Common.Resource.ConfigWebHooksStoreConnectionString))?.Result?.ToString(),
                Constants.SubscriptionsTableName);
            this.searchUpdateKey = KeyVaultHelper.GetSecret(config?.GetConfigValue(Common.Resource.ConfigSearchUpdateKey))?.Result?.ToString();
            this.azureCommonMetricsSearchHelperClient = new SearchHelperClient(
                config?.GetConfigValue(Common.Resource.ConfigSearchServiceName),
                "azurecommonmetrics-index",
                this.searchUpdateKey);
            this.azureMetricsSearchHelperClient = new SearchHelperClient(
                config?.GetConfigValue(Common.Resource.ConfigSearchServiceName),
                "azurelibrarymetrics-index",
                this.searchUpdateKey);
            this.spMetricsSearchHelperClient = new SearchHelperClient(
                config?.GetConfigValue(Common.Resource.ConfigSearchServiceName),
                "splibrarymetrics-index",
                this.searchUpdateKey);
        }

        /// <summary>
        /// Gets the analytics response.
        /// </summary>
        /// <param name="analyticsRequest">The analytics request of type <see cref="AnalyticsRequest"/>.</param>
        /// <returns>Action Result.</returns>
        [HttpPost]
        [Route("")]
        public async Task<ActionResult<AnalyticsResponse>> Get([FromBody] AnalyticsRequest analyticsRequest)
        {
            try
            {
                string queryText;
                string filterQuery = string.Empty;
                if (analyticsRequest?.SiteUrls?.Count > 0)
                {
                    foreach (string siteUrl in analyticsRequest.SiteUrls)
                    {
                        filterQuery += $"PartitionKey eq '{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(siteUrl))}'|";
                    }
                }

                if (analyticsRequest?.LibraryUrls?.Count > 0)
                {
                    foreach (string libraryUrl in analyticsRequest.LibraryUrls)
                    {
                        filterQuery += $"RowKey eq '{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(libraryUrl))}'|";
                    }
                }

                if (string.IsNullOrEmpty(filterQuery))
                {
                    queryText = "*";
                    filterQuery = null;
                }
                else
                {
                    queryText = null;
                    filterQuery = filterQuery.TrimEnd('|').Replace("|", " or ");
                }

                AnalyticsResponse analyticsResponse = new AnalyticsResponse();

                if (analyticsRequest.FetchList.Contains("az", StringComparer.OrdinalIgnoreCase))
                {
                    var azureMetrics = await SearchHelper.Search<AzureLibraryMetricsEntity>(this.azureMetricsSearchHelperClient.SearchClient, queryText, null, filterQuery, skip: 0, top: 100).ConfigureAwait(true);
                    analyticsResponse.Azure = azureMetrics.GetResults().OrderBy(a => a.Document.LibraryUrl).ToList();
                }

                if (analyticsRequest.FetchList.Contains("sp", StringComparer.OrdinalIgnoreCase))
                {
                    var spMetrics = await SearchHelper.Search<SharePointLibraryMetricsEntity>(this.spMetricsSearchHelperClient.SearchClient, queryText, null, filterQuery, skip: 0, top: 100).ConfigureAwait(true);
                    analyticsResponse.SharePoint = spMetrics.GetResults().OrderBy(a => a.Document.LibraryUrl).ToList();
                }

                if (analyticsRequest.FetchList.Contains("sites", StringComparer.OrdinalIgnoreCase))
                {
                    var sites = await SearchHelper.Search<AzureLibraryMetricsEntity>(this.azureMetricsSearchHelperClient.SearchClient, "*", new List<string> { "SiteUrl", "LibraryUrl" }, null, skip: 0, top: 1000).ConfigureAwait(true);
                    analyticsResponse.Sites = JsonConvert.SerializeObject(this.ReturnSitesArray(sites.GetResults()));
                }

                if (analyticsRequest.FetchList.Contains("common", StringComparer.OrdinalIgnoreCase))
                {
                    var commonMetrics = await SearchHelper.Search<AzureCommonMetricsEntity>(this.azureCommonMetricsSearchHelperClient.SearchClient, "*", null, null, skip: 0, top: 100).ConfigureAwait(true);
                    analyticsResponse.Common = commonMetrics.GetResults();
                }

                return analyticsResponse;
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
        /// Returns gijgo tree compatible json array.
        /// </summary>
        /// <param name="sitesResponse">The azure search response.</param>
        /// <returns>The gijgo tree compatible json array.</returns>
        private JArray ReturnSitesArray(Azure.Pageable<SearchResult<AzureLibraryMetricsEntity>> sitesResponse)
        {
            List<SiteAndLibraryModel> siteAndLibraryList = new List<SiteAndLibraryModel>();
            foreach (var siteResponse in sitesResponse)
            {
                siteAndLibraryList.Add(new SiteAndLibraryModel()
                {
                    SiteUrl = siteResponse.Document.SiteUrl,
                    LibraryUrl = siteResponse.Document.LibraryUrl,
                });
            }

            var sitesGroup = siteAndLibraryList.GroupBy(a => a.SiteUrl);

            JArray sitesArray = new JArray();
            JObject level1Object = this.GetTreeNode("All", "1", true, true);
            sitesArray.Add(level1Object);

            foreach (var siteGroup in sitesGroup)
            {
                JObject level2Object = this.GetTreeNode(siteGroup.Key, "2", true, true);

                foreach (var siteGroupValue in siteGroup)
                {
                    JObject level3Object = this.GetTreeNode(siteGroupValue.LibraryUrl, "3", false, true);
                    (level2Object.SelectToken("children") as JArray).Add(level3Object);
                }

                (level1Object.SelectToken("children") as JArray).Add(level2Object);
            }

            return sitesArray;
        }

        /// <summary>
        /// Get the tree node object.
        /// </summary>
        /// <param name="nodeName">The node name.</param>
        /// <param name="nodeLevel">The node level.</param>
        /// <param name="hasChildren"><true>if node has children</true><false>otherwise.</false></param>
        /// <param name="isChecked"><true>if node is checked by default</true><false>otherwise.</false></param>
        /// <returns>The node object of type <see cref="JObject"/>.</returns>
        private JObject GetTreeNode(string nodeName, string nodeLevel, bool hasChildren, bool isChecked)
        {
            JObject treeNodeObject = new JObject()
                                            {
                                               new JProperty("text", nodeName),
                                               new JProperty("hasChildren", hasChildren),
                                               new JProperty("checked", isChecked),
                                               new JProperty("level", nodeLevel),
                                            };

            if (hasChildren)
            {
                treeNodeObject.Add("children", new JArray());
            }

            return treeNodeObject;
        }
    }
}