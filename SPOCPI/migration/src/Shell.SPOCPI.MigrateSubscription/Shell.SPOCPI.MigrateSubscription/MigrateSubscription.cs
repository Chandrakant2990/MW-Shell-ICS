// -----------------------------------------------------------------------
// <copyright file="MigrateSubscription.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------

namespace Shell.SPOCPI.MigrateSubscription
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Microsoft.Graph;
    using Shell.SPOCPI.Common;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Updates SPO subscription to a new notification url.
    /// </summary>
    public static class MigrateSubscription
    {
        [FunctionName("MigrateSubscription")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = string.Empty;

            using (var reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            Response mr = new Response();
            if (requestBody != null)
            {
                Request requestData = JsonConvert.DeserializeObject<Request>(requestBody);
                mr = await SwitchSubscription(requestData).ConfigureAwait(false);
            }

            return new OkObjectResult(mr);
        }

        /// <summary>
        /// Switch subscription based on request data passed to the function.
        /// </summary>
        /// <param name="requestData"><see cref="Request"/></param>
        /// <returns><see cref="Response"/></returns>
        private async static Task<Response> SwitchSubscription(Request requestData)
        {
            string tenantName = Environment.GetEnvironmentVariable("tenantName");
            string appClientId = Environment.GetEnvironmentVariable("aadClientId");
            string appClientSecret = Environment.GetEnvironmentVariable("aadClientSecret");
            Response response = new Response();
            Subscription newSubscription = null;
            SubscriptionEntity sourceSubscriptionEntity = null;

            try
            {
                GraphServiceClient graphClient = GraphHelper.GetGraphClient(tenantName, appClientId, appClientSecret);
                var partitionKey = requestData.SubscriptionPartitionKey;
                var rowKey = requestData.SubscriptionRowKey;
                string spoSubscriptionId = requestData.SPOSubscriptionId;

                CloudTable sourceSubscriptionTable = StorageTableHelper.GetTableEntity(
                    Environment.GetEnvironmentVariable("ConnectionStrings:ConfigurationConnectionString"), "Subscriptions");

                sourceSubscriptionEntity = await StorageTableHelper.GetTableResult<SubscriptionEntity>(
                    sourceSubscriptionTable,
                    partitionKey,
                    rowKey).
                    ConfigureAwait(false);

                await SharePointHelper.DeleteSubscription(graphClient, spoSubscriptionId).ConfigureAwait(false);

                var expirationDateTime = GetExpirationDateTime(sourceSubscriptionEntity.ExpirationDateTime);

                Subscription subscription = new Subscription()
                {
                    ChangeType = Resource.ChangeTypeUpdated,
                    ClientState = sourceSubscriptionEntity.RowKey,
                    ExpirationDateTime = expirationDateTime,
                    NotificationUrl = Environment.GetEnvironmentVariable("notificationUrl"),
                    Resource = $"{Shell.SPOCPI.Common.Constants.Drive}/{sourceSubscriptionEntity.DriveId}/{Shell.SPOCPI.Common.Constants.Root}",
                };

                newSubscription = await SharePointHelper.CreateSubscription(graphClient, subscription).ConfigureAwait(false);
                sourceSubscriptionEntity.SPOSubscriptionId = newSubscription.Id;

                CloudTable destSubscriptionTable = StorageTableHelper.GetTableEntity(
                    Environment.GetEnvironmentVariable("destinationWebHooksConnectionString"), "Subscriptions");

                await StorageTableHelper.AddOrReplaceTableItem(destSubscriptionTable, sourceSubscriptionEntity, Shell.SPOCPI.Common.Constants.SubscriptionsTableName, true).ConfigureAwait(false);

                response.Message = "Success";
                response.Status = "Success";
            }
            catch (Exception ex)
            {
                response.Message = ex.StackTrace;
                response.Status = "Failed";
            }


            // Update the Storage table
            MigrationEntity migrationEntity = new MigrationEntity()
            {
                Status = response.Status
            };

            CloudTable migrationTable = StorageTableHelper.GetTableEntity(
                    Environment.GetEnvironmentVariable("ConnectionStrings:SDUDataStorageConnectionString"), "MigrationTracker");
            await StorageTableHelper.AddOrReplaceTableItem(migrationTable, migrationEntity, "Migration", true).ConfigureAwait(false);

            return response;
        }

        /// <summary>
        /// GetExpirationDateTime is used to return an DateTime object based on the number of days. Here we are fixing it to 21 days maximum.
        /// </summary>
        /// <param name="expirationDateTime">The ExpirationDateTime returned from the Azure Table Storage.</param>
        /// <param name="days">Number of days to be added to the ExpirationTimeDate. Make sure it is not more than 23 days.</param>
        /// <returns>The ExpirationDateTime as a DateTime object.</returns>
        private static DateTime? GetExpirationDateTime(DateTime? expirationDateTime, int days = 21)
        {
            try
            {
                if (expirationDateTime is null || expirationDateTime == DateTime.MinValue)
                {
                    expirationDateTime = DateTime.UtcNow.AddDays(days);
                }

                if (expirationDateTime > DateTime.UtcNow.AddDays(days))
                {
                    expirationDateTime = DateTime.UtcNow.AddDays(days);
                }
            }
            catch (Exception)
            {
                expirationDateTime = DateTime.UtcNow.AddDays(days);
            }

            return expirationDateTime;
        }

        /// <summary>
        /// Convert driveId to base64.
        /// </summary>
        /// <param name="driveId">DriveId text.</param>
        /// <returns>Base64 text.</returns>
        public static string Base64Encode(string driveId)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(driveId);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
