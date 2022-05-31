// <copyright file="ChangeProcessor.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.ChangeProcessor.FunctionApp
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.Cosmos.Table;
    using Newtonsoft.Json;
    using Shell.SPOCPI.Common;

    /// <summary>
    /// Change Processor Function App.
    /// </summary>
    public class ChangeProcessor
    {
        /// <summary>
        /// The logger component instance.
        /// </summary>
        private readonly ILoggerComponent logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeProcessor" /> class.
        /// </summary>
        /// <param name="loggerComponent">logger instance.</param>
        public ChangeProcessor(ILoggerComponent loggerComponent)
        {
            this.logger = loggerComponent;
        }

        /// <summary>
        /// Runs the specified notification message.
        /// </summary>
        /// <param name="notificationMessage">The notification message.</param>
        /// <param name="notificationTable">The notification table.</param>
        /// <exception cref="Exception">Error in Change Processing Function App.</exception>
        /// <returns>Task status.</returns>
        [FunctionName(Constants.ChangeProcessorFunctionName)]
        public async Task Run(
            [ServiceBusTrigger(Constants.WebHooksQueueName, Connection = Constants.ChangeProcessorServiceBusConnectionName)] string notificationMessage,
            [Table(Constants.NotificationStorageTableName)] CloudTable notificationTable)
        {
            try
            {
                this.logger.LogInformation(Resource.FunctionAppTrigger, Constants.SPOCPI, nameof(LogCategory.ChangeProcessorFunctionApp));
                var notification = JsonConvert.DeserializeObject<Notification>(notificationMessage);
                var result = await this.ProcessQueueMessage(notification, notificationTable).ConfigureAwait(true);
                if (result)
                {
                    return;
                }
                else
                {
                    this.logger.LogWarning(Resource.FunctionAppError, Constants.SPOCPI, nameof(LogCategory.ChangeProcessorFunctionApp));
                    throw new Exception(Resource.FunctionAppError);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Resource.FunctionAppException, Constants.SPOCPI, nameof(LogCategory.ChangeProcessorFunctionApp));
                throw;
            }
        }

        /// <summary>
        /// Determines whether [is transient error] [the specified result].
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>
        ///   <c>true</c> if [is transient error] [the specified result]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsTransientError(TableResult result)
        {
            if (result.HttpStatusCode == (int)HttpStatusCode.RequestTimeout || result.HttpStatusCode == (int)HttpStatusCode.Conflict)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified result is success.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>
        ///   <c>true</c> if the specified result is success; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsSuccess(TableResult result)
        {
            if (result.HttpStatusCode == (int)HttpStatusCode.OK || result.HttpStatusCode == (int)HttpStatusCode.NoContent)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Processes the queue message.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="notificationTable">The notification table.</param>
        /// <returns>A flag indicating whether the processing of queue message is successful or not.</returns>
        private async Task<bool> ProcessQueueMessage(Notification notification, CloudTable notificationTable)
        {
            bool isSuccess = false;

            try
            {
                TableResult result;
                int attempt = 1;
                while (attempt < 3)
                {
                    var entity = new NotificationEntity()
                    {
                        PartitionKey = Constants.SPOCPI,
                        RowKey = string.Format(CultureInfo.InvariantCulture, "{0:D19}", DateTime.UtcNow.Ticks),
                        MessageJson = JsonConvert.SerializeObject(notification),
                        AttemptsCount = 0,
                        Status = Constants.NotificationStatusIdle,
                        ReceivedTime = DateTime.UtcNow,
                    };

                    result = await StorageTableHelper.AddTableItem(notificationTable, entity).ConfigureAwait(true);
                    if (ChangeProcessor.IsTransientError(result))
                    {
                        // Transient error. Can re-try to push to storage.
                        attempt++;
                        await Task.Delay(100).ConfigureAwait(true);
                    }
                    else if (ChangeProcessor.IsSuccess(result))
                    {
                        // Success
                        isSuccess = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.ChangeProcessorFunctionApp));
                throw;
            }

            return isSuccess;
        }
    }
}