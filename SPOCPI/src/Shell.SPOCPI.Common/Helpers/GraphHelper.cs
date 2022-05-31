// -----------------------------------------------------------------------
// <copyright file="GraphHelper.cs" company="Microsoft Corporation">
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
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Newtonsoft.Json;

    /// <summary>
    /// The Graph Helper.
    /// </summary>
    public static class GraphHelper
    {
        /// <summary>
        /// Gets the access token.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="appClientId">The application client identifier.</param>
        /// <param name="appClientSecret">The application client secret.</param>
        /// <returns>The authentication result.</returns>
        public static async Task<AuthenticationResult> GetAccessToken(string tenantId, string appClientId, string appClientSecret)
        {
            string authority = string.Format(CultureInfo.InvariantCulture, Constants.AuthorityUrl, tenantId);
            ClientCredential clientCredential = new ClientCredential(appClientId, appClientSecret);
            AuthenticationContext context = new AuthenticationContext(authority);
            AuthenticationResult authenticationResult = await context.AcquireTokenAsync(Constants.GraphResourceUrl, clientCredential).ConfigureAwait(false);

            return authenticationResult;
        }

        /// <summary>
        /// Gets the graph client.
        /// </summary>
        /// <param name="tenantName">Name of the tenant.</param>
        /// <param name="appClientId">The application client identifier.</param>
        /// <param name="appClientSecret">The application client secret.</param>
        /// <returns>The Graph Service Client.</returns>
        public static GraphServiceClient GetGraphClient(string tenantName, string appClientId, string appClientSecret)
        {
            var authenticationProvider = new GraphAuthProvider(tenantName, appClientId, appClientSecret);
            var graphServiceClient = new GraphServiceClient(authenticationProvider);

            return graphServiceClient;
        }

        [SuppressMessage("Microsoft.Design", "CA1054:UriPropertiesShouldNotBeStrings", Justification = "Reviewed.")]

        /// <summary>
        /// Calls the graph API asynchronous.
        /// </summary>
        /// <typeparam name="T">Generic Parameter.</typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="method">The method.</param>
        /// <param name="requestBodyObject">The request body object.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns>The JSON object.</returns>
        public static async Task<object> CallGraphAPIAsync<T>(string url, HttpMethod method, object requestBodyObject, string contentType, string accessToken)
            where T : class
        {
            // Initialize an HttpWebRequest for the current URL.
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            if (method != null)
            {
                webReq.Method = method.Method;
            }

            if (!string.IsNullOrEmpty(contentType))
            {
                webReq.ContentType = Constants.ContentType;
            }

            webReq.Headers[Resource.RequestHeaderAuthorization] = Resource.RequestHeaderAuthorizationType + accessToken;

            if (requestBodyObject != null)
            {
                var requestBody = JsonConvert.SerializeObject(requestBodyObject);
                webReq.ContentLength = requestBody.Length;
                var streamWriter = new StreamWriter(webReq.GetRequestStream());
                streamWriter.Write(requestBody);
                streamWriter.Close();
            }

            var response = await webReq.GetResponseAsync().ConfigureAwait(false);
            if (response == null)
            {
                return default;
            }

            var responseContent = string.Empty;
            StreamReader streamReader = null;
            using (streamReader = new StreamReader(response.GetResponseStream()))
            {
                responseContent = streamReader.ReadToEnd().Trim();
            }

            var jsonObject = JsonConvert.DeserializeObject<T>(responseContent);

            return jsonObject;
        }

        /// <summary>
        /// Gets the drive items.
        /// </summary>
        /// <param name="graphClient">The graph client.</param>
        /// <param name="driveId">The drive identifier.</param>
        /// <param name="deltaToken">The delta token.</param>
        /// <param name="selectFields">The select fields.</param>
        /// <returns>The drive item collection.</returns>
        public static async Task<IDriveItemDeltaCollectionPage> GetDriveItems(GraphServiceClient graphClient, string driveId, string deltaToken, string selectFields)
        {
            if (graphClient == null)
            {
                throw new ArgumentNullException(nameof(graphClient));
            }

            IDriveItemDeltaCollectionPage response;
            try
            {
                if (!string.IsNullOrEmpty(deltaToken))
                {
                    response = await graphClient.Drives[driveId].Root.Delta(deltaToken)
                        .Request()
                        .Select(selectFields)
                        .GetAsync().ConfigureAwait(false);
                }
                else
                {
                    response = await graphClient.Drives[driveId].Root.Delta()
                        .Request()
                        .Select(selectFields)
                        .GetAsync().ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                throw;
            }

            return response;
        }

        /// <summary>
        /// Gets the next page link.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <returns>The next page link url.</returns>
        public static string GetNextPageLink(IDriveItemDeltaCollectionPage page)
        {
            if (page != null && page.AdditionalData.ContainsKey(Resource.DataNextLink))
            {
                return page.AdditionalData[Resource.DataNextLink].ToString();
            }

            return null;
        }

        /// <summary>
        /// Gets the delta link.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <returns>The delta link url.</returns>
        public static string GetDeltaLink(IDriveItemDeltaCollectionPage page)
        {
            if (page != null && page.AdditionalData.ContainsKey(Resource.DataDeltaLink))
            {
                return page.AdditionalData[Resource.DataDeltaLink].ToString();
            }

            return null;
        }

        [SuppressMessage("Microsoft.Design", "CA1055:UriPropertiesShouldNotBeStrings", Justification = "Reviewed.")]

        /// <summary>
        /// Gets the token from delta URL.
        /// </summary>
        /// <param name="deltaurl">The delta url.</param>
        /// <returns>The delta token.</returns>
        public static string GetTokenFromDeltaUrl(string deltaurl)
        {
            string token = string.Empty;
            if (!string.IsNullOrEmpty(deltaurl))
            {
                var tokens = deltaurl.Split(new string[] { Resource.Token1 }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens != null && tokens.Length > 1)
                {
                    string[] tokens2 = tokens[1].Split(new string[] { Resource.Token3 }, StringSplitOptions.RemoveEmptyEntries);
                    token = tokens2[0].Trim();
                }
                else
                {
                    tokens = deltaurl.Split(new string[] { Resource.Token2 }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens != null && tokens.Length > 1)
                    {
                        token = tokens[1];
                    }
                }
            }

            return token;
        }

        /// <summary>
        /// Gets the latest token for the specified Drive ID.
        /// </summary>
        /// <param name="graphClient">The graph client.</param>
        /// <param name="driveId">The drive identifier.</param>
        /// <returns>The latest drive delta token.</returns>
        /// <exception cref="ArgumentNullException">graphClient is null.</exception>
        public static async Task<string> GetLatestToken(GraphServiceClient graphClient, string driveId)
        {
            if (graphClient == null)
            {
                throw new ArgumentNullException(nameof(graphClient));
            }

            IDriveItemDeltaCollectionPage response;
            string deltaLink = string.Empty;

            try
            {
                var queryOptions = new List<QueryOption>()
                {
                    new QueryOption(Resource.QueryOptionTokenKey, Resource.QueryOptionTokenLatestValue),
                };

                response = await graphClient.Drives[driveId].Root.Delta()
                        .Request(queryOptions)
                        .GetAsync().ConfigureAwait(false);

                if (response != null)
                {
                    if ((bool)response.AdditionalData?.ContainsKey(Resource.DataDeltaLink))
                    {
                        deltaLink = GetTokenFromDeltaUrl(response.AdditionalData[Resource.DataDeltaLink].ToString());
                    }
                }

                return deltaLink;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets the latest delta URL for the specified Drive ID.
        /// </summary>
        /// <param name="graphClient">The graph client.</param>
        /// <param name="driveId">The drive identifier.</param>
        /// <returns>The latest drive delta token.</returns>
        /// <exception cref="ArgumentNullException">graphClient is null.</exception>
        public static async Task<string> GetLatestDeltaUrl(GraphServiceClient graphClient, string driveId)
        {
            if (graphClient == null)
            {
                throw new ArgumentNullException(nameof(graphClient));
            }

            IDriveItemDeltaCollectionPage response;
            string deltaLink = string.Empty;

            try
            {
                var queryOptions = new List<QueryOption>()
                {
                    new QueryOption(Resource.QueryOptionTokenKey, Resource.QueryOptionTokenLatestValue),
                };

                response = await graphClient.Drives[driveId].Root.Delta()
                        .Request(queryOptions)
                        .GetAsync().ConfigureAwait(false);

                if (response != null)
                {
                    if ((bool)response.AdditionalData?.ContainsKey(Resource.DataDeltaLink))
                    {
                        deltaLink = response.AdditionalData[Resource.DataDeltaLink].ToString();
                    }
                }

                return deltaLink;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Determines whether [is transient Exception] [the specified ex].
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns>
        ///   <c>true</c> if [is transient exception] [the specified ex]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsTransientException(Exception ex)
        {
            var serviceEx = ex as ServiceException;
            if (serviceEx != null)
            {
                if (serviceEx.Error.Code == Resource.ActivityLimitError || serviceEx.Error.Code == Resource.ServiceNotAvailableError || serviceEx.Error.Code == Resource.QuotaLimitError || serviceEx.Error.Code == Resource.ResyncRequiredError)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether [is re-sync Exception] [the specified ex].
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns>
        ///   <c>true</c> if [is transient exception] [the specified ex]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsResyncRequired(Exception ex)
        {
            return ex is ServiceException serviceEx && serviceEx.Error.Code.Equals(Resource.ResyncRequiredError, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Function to get WebUrl.
        /// </summary>
        /// <param name="gclient">gclient.</param>
        /// <param name="listId">listId.</param>
        /// <param name="listItemId">listItemId.</param>
        /// <param name="siteId">siteId.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<string> GetWebUrl(GraphServiceClient gclient, string listId, string listItemId, string siteId)
        {
            var response = await gclient.Sites[siteId].Lists[listId].Items[listItemId].Request().Select("webUrl").GetAsync().ConfigureAwait(false);
            return response.WebUrl;
        }
    }
}