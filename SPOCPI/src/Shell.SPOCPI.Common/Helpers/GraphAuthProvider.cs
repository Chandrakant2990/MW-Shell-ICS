// -----------------------------------------------------------------------
// <copyright file="GraphAuthProvider.cs" company="Microsoft Corporation">
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
    using System.Globalization;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// Provides Authorization for Graph API.
    /// </summary>
    public class GraphAuthProvider : IAuthenticationProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphAuthProvider"/> class.
        /// </summary>
        /// <param name="tenantName">Name of the tenant.</param>
        /// <param name="appClientId">The application client identifier.</param>
        /// <param name="appClientSecret">The application client secret.</param>
        public GraphAuthProvider(string tenantName, string appClientId, string appClientSecret)
        {
            this.TenantName = tenantName;
            this.AppClientId = appClientId;
            this.AppClientSecret = appClientSecret;
        }

        /// <summary>
        /// Gets or sets the name of the tenant.
        /// </summary>
        /// <value>
        /// The name of the tenant.
        /// </value>
        private string TenantName { get; set; }

        /// <summary>
        /// Gets or sets the AAD App Client Id
        /// </summary>
        private string AppClientId { get; set; }

        /// <summary>
        /// Gets or sets the AAD App Client Secret
        /// </summary>
        private string AppClientSecret { get; set; }

        /// <summary>
        /// Authenticates the specified request message.
        /// </summary>
        /// <param name="request">The <see cref="System.Net.Http.HttpRequestMessage" /> to authenticate.</param>
        /// <returns>the task.</returns>
        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            string authority = string.Format(CultureInfo.InvariantCulture, Constants.AuthorityUrl, this.TenantName);
            var authContext = new AuthenticationContext(authority);
            var creds = new ClientCredential(this.AppClientId, this.AppClientSecret);

            AuthenticationResult authResult = await authContext.AcquireTokenAsync(Constants.GraphResourceUrl, creds).ConfigureAwait(false);

            if (request != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(Resource.AuthorizationTokenType, authResult.AccessToken);
            }
        }
    }
}
