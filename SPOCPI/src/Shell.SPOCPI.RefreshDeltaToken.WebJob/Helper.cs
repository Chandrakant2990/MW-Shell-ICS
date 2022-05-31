// <copyright file="Helper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.RefreshDeltaToken.WebJob
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Graph;
    using Shell.SPOCPI.Common;
    using Constants = Shell.SPOCPI.Common.Constants;

    /// <summary>
    /// The Refresh Token helper class.
    /// </summary>
    internal class Helper
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
        /// The drive delta table instance.
        /// </summary>
        private readonly CloudTable driveDeltaTable;

        /// <summary>
        /// The drive delta transaction table instance.
        /// </summary>
        private readonly CloudTable driveDeltaTransactionTable;

        /// <summary>
        /// The subscription table instance.
        /// </summary>
        private readonly CloudTable subscriptionTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="Helper"/> class.
        /// </summary>
        /// <param name="loggerComponent">The logger.</param>
        /// <param name="configuration">The configuration.</param>
        internal Helper(ILoggerComponent loggerComponent, IConfiguration configuration)
        {
            this.logger = loggerComponent;
            this.config = configuration;
            this.driveDeltaTable = StorageTableHelper.GetTableEntity(KeyVaultHelper.GetSecret(this.config.GetConfigValue(Common.Resource.ConfigCPDriveDeltaStoreConnString))?.Result?.ToString(), this.config.GetConfigValue(Common.Resource.ConfigCPDriveDeltaTableName));
            this.driveDeltaTransactionTable = StorageTableHelper.GetTableEntity(KeyVaultHelper.GetSecret(this.config.GetConfigValue(Common.Resource.ConfigCPDriveDeltaStoreConnString))?.Result?.ToString(), this.config.GetConfigValue(Common.Resource.ConfigCPDriveDeltaTransTableName));
            this.subscriptionTable = StorageTableHelper.GetTableEntity(KeyVaultHelper.GetSecret(this.config.GetConfigValue(Common.Resource.ConfigWebHooksStoreConnectionString))?.Result?.ToString(), this.config.GetConfigValue(Constants.SubscriptionsTableName));
        }

        /// <summary>
        /// Processes the queue messages.
        /// </summary>
        /// <returns>The task status.</returns>
        internal async Task ProcessDeltaTokens()
        {
            var gclient = this.GetGraphClient();

            try
            {
                var driveDeltaEntities = await this.GetDeltaTokensForProcessing().ConfigureAwait(false);
                var itemsToProcess = driveDeltaEntities.Where(d => d.InProgress == false);
                foreach (var driveDelta in itemsToProcess)
                {
                    string driveId = await this.GetDriveId(driveDelta).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(driveId))
                    {
                        this.logger.LogWarning(string.Format(CultureInfo.InvariantCulture, Resources.DriveIdNotFoundMessage, driveDelta.RowKey), Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));
                        continue;
                    }

                    try
                    {
                        bool refreshRequired = await this.HasDeltaTokenExpired(gclient, driveId, driveDelta).ConfigureAwait(false);
                        if (refreshRequired)
                        {
                            var deltaUrl = await GraphHelper.GetLatestDeltaUrl(gclient, driveId).ConfigureAwait(false);
                            if (!string.IsNullOrEmpty(deltaUrl))
                            {
                                var deltaToken = GraphHelper.GetTokenFromDeltaUrl(deltaUrl);
                                TableResult result = null;
                                if (!deltaToken.Equals(driveDelta.Token, StringComparison.Ordinal))
                                {
                                    await AzureRetryHelper.OperationWithBasicRetryAsync(async () =>
                                    {
                                        result = await this.UpdateDeltaToken(driveDelta, deltaUrl, deltaToken).ConfigureAwait(false);
                                    }).ConfigureAwait(false);
                                }
                            }
                        }
                        else
                        {
                            this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resources.RefreshTokenNotRequiredMessage, driveId, driveDelta.RowKey), Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));
            }
        }

        /// <summary>
        /// Determines whether [has delta token expired] [the specified graph client].
        /// </summary>
        /// <param name="gclient">The graph client.</param>
        /// <param name="driveId">The drive identifier.</param>
        /// <param name="driveDelta">The drive delta.</param>
        /// <returns>
        ///   <c>true</c> if [delta token has expired] [for the specified graphc client]; otherwise, <c>false</c>.
        /// </returns>
        private async Task<bool> HasDeltaTokenExpired(GraphServiceClient gclient, string driveId, DriveDeltaEntity driveDelta)
        {
            int retryAttempts = 0;
            int backoffInterval = 2000;
            int maxRetryAttempts = Convert.ToInt32(this.config.GetConfigValue(Common.Resource.ConfigCPMaxRetryAttempts)) < 1 ? 5 : Convert.ToInt32(this.config.GetConfigValue(Common.Resource.ConfigCPMaxRetryAttempts));
            string fieldsToSelect = string.IsNullOrEmpty(this.config.GetConfigValue(Common.Resource.ConfigCPQueryFields)) ? Constants.DefaultFields : this.config.GetConfigValue(Common.Resource.ConfigCPQueryFields);

            // Do while retry attempt is less than retry count
            while (retryAttempts < maxRetryAttempts)
            {
                try
                {
                    var response = await GraphHelper.GetDriveItems(gclient, driveId, driveDelta.Token, fieldsToSelect).ConfigureAwait(false);
                    return false;
                }
                catch (ServiceException ex)
                {
                    this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));
                    if (GraphHelper.IsTransientException(ex))
                    {
                        if (GraphHelper.IsResyncRequired(ex))
                        {
                            return true;
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
                    this.logger.LogError(ex, Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));
                    throw;
                }
            }

            this.logger.LogWarning(string.Format(CultureInfo.InvariantCulture, Resources.MaximumRetryAttemptWarning, retryAttempts), Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));
            throw new Exception(string.Format(CultureInfo.InvariantCulture, Resources.MaximumRetryAttemptWarning, retryAttempts));
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
        /// Gets the message from storage.
        /// </summary>
        /// <returns>The list of notification entities from the storage.</returns>
        private async Task<List<DriveDeltaEntity>> GetDeltaTokensForProcessing()
        {
            if (this.driveDeltaTable != null)
            {
                int expiryDays = Convert.ToInt32(this.config.GetConfigValue(Common.Resource.ConfigCPDeltaTokenExpiryDays), CultureInfo.InvariantCulture) * -1;
                var filterQuery = TableQuery.GenerateFilterConditionForDate(Constants.Timestamp, QueryComparisons.LessThan, new DateTimeOffset(DateTime.UtcNow.AddDays(expiryDays)));
                var results = await StorageTableHelper.GetTableResultBasedOnQueryFilter<DriveDeltaEntity>(this.driveDeltaTable, filterQuery).ConfigureAwait(false);
                return results;
            }

            return default;
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
            this.logger.LogInformation(Resources.UpdateDeltaTokenStart, Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));
            if (string.IsNullOrEmpty(deltatUrl))
            {
                this.logger.LogInformation(Resources.UpdateDeltaTokenNoDeltaUrl, Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));
            }

            var driveDeltaTransactionEntity = new DriveDeltaTransactionEntity()
            {
                PartitionKey = driveDelta.RowKey,
                RowKey = Guid.NewGuid().ToString(),
                Agent = nameof(LogCategory.RefreshDeltaTokenWebJob),
                OldDeltaUrl = driveDelta.DeltaUrl,
                OldToken = driveDelta.Token,
                OldTokenTimestamp = driveDelta.Timestamp,
                NewDeltaUrl = deltatUrl,
                NewToken = deltaToken,
            };

            if (this.driveDeltaTable == null || this.driveDeltaTransactionTable == null)
            {
                this.logger.LogWarning(Resources.DriveDeltaTableIsNullMessage, Common.Constants.SPOCPI, nameof(LogCategory.RefreshDeltaTokenWebJob));
                return default;
            }

            var result = await StorageTableHelper.AddTableItem(this.driveDeltaTransactionTable, driveDeltaTransactionEntity);
            if (result.HttpStatusCode == (int)HttpStatusCode.OK || result.HttpStatusCode == (int)HttpStatusCode.NoContent)
            {
                driveDelta.DeltaUrl = deltatUrl;
                driveDelta.Token = deltaToken;
                driveDelta.DeltaTokenRefreshed = new DateTimeOffset(DateTime.UtcNow);

                if (this.driveDeltaTable != null)
                {
                    // Perform another check to see if CP Job has not picked the item already for processing.
                    var item = await StorageTableHelper.GetTableResult<DriveDeltaEntity>(this.driveDeltaTable, driveDelta.PartitionKey, driveDelta.RowKey).ConfigureAwait(false);
                    if (item != null && !item.InProgress)
                    {
                        return await StorageTableHelper.UpdateItem(this.driveDeltaTable, driveDelta);
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Gets the drive identifier.
        /// </summary>
        /// <param name="driveDelta">The drive delta.</param>
        /// <returns>The DriveId.</returns>
        private async Task<string> GetDriveId(DriveDeltaEntity driveDelta)
        {
            string subscriptionId = driveDelta.RowKey;
            if (this.subscriptionTable != null && driveDelta.DeltaUrl != null)
            {
                var filterQuery = TableQuery.GenerateFilterCondition(Constants.RowKey, QueryComparisons.Equal, subscriptionId);
                var results = await StorageTableHelper.GetTopTableResultBasedOnQueryFilter<SubscriptionEntity>(this.subscriptionTable, 1000, filterQuery).ConfigureAwait(false);
                var result = results.SingleOrDefault(o => driveDelta.DeltaUrl.Contains(o.DriveId, StringComparison.Ordinal));
                return result?.DriveId;
            }

            return default;
        }
    }
}
