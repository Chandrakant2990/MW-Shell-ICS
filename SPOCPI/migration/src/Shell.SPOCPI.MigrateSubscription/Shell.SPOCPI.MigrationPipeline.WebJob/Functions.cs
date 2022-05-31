// <copyright file="Functions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.MigrationPipeline.WebJob
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.Management.DataFactory;
    using Microsoft.Azure.Management.DataFactory.Models;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Rest;
    using Shell.Common.Core;

    [Singleton]
    public class Functions
    {
        /// <summary>
        /// The logger component.
        /// </summary>
        private readonly ILoggerComponent logger;

        /// <summary>
        /// The configuration
        /// </summary>
        private readonly IConfiguration config;

        /// <summary>
        /// Initializes a new instance of the <see cref="Functions" /> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider instance.</param>
        public Functions(IServiceProvider serviceProvider)
        {
            this.logger = serviceProvider.GetService<ILoggerComponent>();
            this.config = serviceProvider.GetRequiredService<IConfiguration>();
        }

        /// <summary>
        /// Processes the job.
        /// </summary>
        /// <param name="timerInfo">The timer information.</param>
        /// <returns>A task representing the output of the function.</returns>
        /// <exception cref="ArgumentNullException">timerInfo is null.</exception>
        public async Task ProcessSubscriptionMigrationAsync([TimerTrigger(typeof(CustomMinuteSchedule))] TimerInfo timerInfo)
        {
            try
            {
                if (timerInfo != null)
                {
                    this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.EnterMethod, nameof(this.ProcessSubscriptionMigrationAsync)), Constants.Migration, nameof(Constants.LogCategory));
                    await this.TriggerMigrationPipeline().ConfigureAwait(false);
                    this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.ExitMethod, nameof(this.ProcessSubscriptionMigrationAsync)), Constants.Migration, nameof(Constants.LogCategory));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Resource.GeneralException, Constants.Migration, nameof(Constants.LogCategory));
            }
        }

        /// <summary>
        /// Trigger migration pipeline.
        /// </summary>
        private async Task TriggerMigrationPipeline()
        {
            try
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync(Constants.AzureManagementUrl).ConfigureAwait(false);
                bool isTrigerringPossible = this.TriggeringPossible(accessToken);
                if (isTrigerringPossible)
                {
                    List<MigrationEntity> items = await GetMigrationEntity().ConfigureAwait(false);
                    if (items != null)
                    {
                        await this.TriggerPipelineAndUpdateTableAsync(items, accessToken).ConfigureAwait(false);
                    }
                    else
                    {
                        this.logger.LogInformation(Resource.NoItemsToProcess, Constants.Migration, nameof(Constants.LogCategory));
                    }
                }
                else
                {
                    this.logger.LogInformation(Resource.PipelineQueued, Constants.Migration, nameof(Constants.LogCategory));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, string.Format(CultureInfo.InvariantCulture, Resource.MigrationJobException, ex.Message), Constants.Migration, nameof(Constants.LogCategory));
            }

        }

        /// <summary>
        /// Gets the migration entity to kick off the pipeline.
        /// </summary>
        /// <returns>Subscription entry.</returns>
        private async Task<List<MigrationEntity>> GetMigrationEntity()
        {
            int batchSize = ConfigHelper.IntegerReader("MigrationPipelineSize", 1);
            string filterQuery = TableQuery.GenerateFilterCondition(nameof(MigrationEntity.Status), QueryComparisons.Equal, "Pending");
            CloudTable batchLocationsTable = StorageTableHelper.GetTableEntity(ConfigHelper.StringReader(Constants.MigrationStorageConnectionString, default(string)), "MigrationTracker");
            var items = await StorageTableHelper.GetTopTableResultBasedOnQueryFilter<MigrationEntity>(batchLocationsTable, batchSize, filterQuery).ConfigureAwait(false);
            return items.Count > 0 ? items : null;
        }

        /// <summary>
        /// Check whether If triggering pipeline is possible or not based on the queued status.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <returns>true or false depending on if triggering is possible or not.</returns>
        private bool TriggeringPossible(string accessToken)
        {
            bool isPossible = false;
            string resourceGroupName = this.config?.GetConfigValue(Constants.ResourceGroupName);
            string azureDataFactoryName = this.config?.GetConfigValue(Constants.DataFactoryName);
            string subscriptionId = this.config?.GetConfigValue(Constants.AzureSubscriptionId);
            try
            {
                ServiceClientCredentials cred = new TokenCredentials(accessToken);
                using (var client = new DataFactoryManagementClient(cred) { SubscriptionId = subscriptionId })
                {
                    // Queued staus of batch update pipeline
                    RunQueryFilter filter1 = new RunQueryFilter(Resource.PipelineName, Resource.EqualsOperator, new List<string> { Constants.MigrationPipelineName });
                    RunQueryFilter filter2 = new RunQueryFilter(Resource.Status, Resource.EqualsOperator, new List<string> { Resource.Queued });
                    DateTime before = DateTime.UtcNow;
                    DateTime after = before.AddHours(-ConfigHelper.IntegerReader(Resource.PipelineQueuedGap, 4));
                    RunFilterParameters param = new RunFilterParameters(after, before, null, new List<RunQueryFilter> { filter1, filter2 }, null);
                    PipelineRunsQueryResponse pipelineResponse = client.PipelineRuns.QueryByFactory(resourceGroupName, azureDataFactoryName, param);
                    int? batchQueuedPipelines = pipelineResponse?.Value?.Count;
                    this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.NumberOfMigrationPipelinesQueued, batchQueuedPipelines), Constants.Migration, nameof(Constants.LogCategory));

                    if (batchQueuedPipelines < int.Parse(Constants.MigrationQueuedPipelinesLimit))
                    {
                        isPossible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, string.Format(CultureInfo.InvariantCulture, Resource.MigrationJobException, ex.Message), Constants.Migration, nameof(Constants.LogCategory));
            }

            return isPossible;
        }

        /// <summary>
        /// Triggers the pipeline and update table asynchronous.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns>The Task status.</returns>
        private async Task TriggerPipelineAndUpdateTableAsync(List<MigrationEntity> migrationEntities, string accessToken)
        {
            foreach (MigrationEntity migrationEntity in migrationEntities)
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("Base64DriveId", migrationEntity.Base64DriveId);
                parameters.Add("SubscriptionPartitionKey", migrationEntity.PartitionKey);
                parameters.Add("SubscriptionRowKey", migrationEntity.RowKey);
                parameters.Add("SPOSubscriptionId", migrationEntity.SPOSubscriptionId);

                migrationEntity.PipelineRunId = await this.GetPipelineRunIdAsync(accessToken, parameters, Constants.MigrationPipelineName).ConfigureAwait(false);
                migrationEntity.Status = "InProgress";
            }

            CloudTable migrationTrackerTable = StorageTableHelper.GetTableEntity(ConfigHelper.StringReader(Constants.MigrationStorageConnectionString, default(string)), "MigrationTracker");
            await AzureRetryHelper.OperationWithBasicRetryAsync(async () =>
            {
                await AddOrMergeMultipleTableItems(migrationTrackerTable, migrationEntities.ToArray()).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the pipeline run identifier asynchronous.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="pipelineBatch">The pipeline batch.</param>
        /// <param name="pipelineName">Name of the pipeline.</param>
        /// <param name="spoSubscriptionId">The SPO subscription identifier.</param>
        /// <returns>Pipeline Id.</returns>
        private async Task<string> GetPipelineRunIdAsync(string accessToken, Dictionary<string, object> parameters, string pipelineName)
        {
            string resourceGroupName = this.config?.GetConfigValue(Constants.ResourceGroupName);
            string azureDataFactoryName = this.config?.GetConfigValue(Constants.DataFactoryName);
            string subscriptionId = this.config?.GetConfigValue(Constants.AzureSubscriptionId);
            string pipelineRunResponse = string.Empty;
            try
            {
                ServiceClientCredentials cred = new TokenCredentials(accessToken);

                using (var client = new DataFactoryManagementClient(cred) { SubscriptionId = subscriptionId })
                {
                    await AzureRetryHelper.OperationWithBasicRetryAsync(async () =>
                    {
                        pipelineRunResponse = (await client.Pipelines.CreateRunWithHttpMessagesAsync(
                        resourceGroupName, azureDataFactoryName, pipelineName, parameters: parameters).ConfigureAwait(false)).Body.RunId;
                    }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, string.Format(CultureInfo.InvariantCulture, Resource.MigrationJobException, ex.Message), Constants.Migration, nameof(Constants.LogCategory));
            }

            return pipelineRunResponse;
        }

        /// <summary>
        /// Adds the or replace multiple table items.
        /// </summary>
        /// <param name="cloudTable">The cloud table.</param>
        /// <param name="tableEntities">The table entities.</param>
        /// <returns>Table Result.</returns>
        public static async Task<IList<TableResult>> AddOrMergeMultipleTableItems(CloudTable cloudTable, TableEntity[] tableEntities)
        {
            IList<TableResult> tableResult = null;
            if (tableEntities != null && tableEntities.Length > 0)
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (var tableEntity in tableEntities)
                {
                    batchOperation.InsertOrMerge(tableEntity);
                }

                if (cloudTable != null)
                {
                    tableResult = await cloudTable.ExecuteBatchAsync(batchOperation).ConfigureAwait(false);
                }
            }

            return tableResult;
        }
    }
}