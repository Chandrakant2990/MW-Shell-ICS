// -----------------------------------------------------------------------
// <copyright file="KeyVaultHelper.cs" company="Microsoft Corporation">
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
    using System.Threading.Tasks;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;

    /// <summary>
    /// KeyVault Helper - used to get secrets from KeyVault
    /// </summary>
    public static class KeyVaultHelper
    {
        /// <summary>
        /// Get secret value from KeyVault
        /// </summary>
        /// <param name="secretUrl">KeyVault Secret Url</param>
        /// <returns>Secret value</returns>
        public static async Task<string> GetSecret(string secretUrl)
        {
            string secretValue = null;

            if (!string.IsNullOrEmpty(secretUrl))
            {
                await AzureRetryHelper.OperationWithBasicRetryAsync(async () =>
                {
                    Uri secretUri = new Uri(secretUrl);
                    string keyVaultUrl = secretUri.Scheme + "://" + secretUri.Host;
                    string secretName = secretUri.Segments[2].TrimEnd('/');
                    var secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
                    KeyVaultSecret keyVaultSecret = secretClient.GetSecret(secretName);
                    secretValue = keyVaultSecret.Value;
                }).ConfigureAwait(false);
            }

            return secretValue;
        }
    }
}
