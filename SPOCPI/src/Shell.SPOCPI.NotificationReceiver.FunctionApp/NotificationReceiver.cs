// -----------------------------------------------------------------------
// <copyright file="NotificationReceiver.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------
namespace Shell.SPOCPI.NotificationReceiver.FunctionApp
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Shell.SPOCPI.Common;

    /// <summary>
    /// The NotificationReceiver Class to receive the notifications and push it into a queue.
    /// </summary>
    public static class NotificationReceiver
    {
        /// <summary>
        /// The logger component.
        /// </summary>
        private static readonly ILoggerComponent LoggerComponent = new LoggerComponent(new SPOCPIConfiguration());

        /// <summary>
        /// Receives the notification and enqueue it.
        /// </summary>
        /// <param name="req">The request.</param>
        /// <param name="incomingNotification">The incoming notification.</param>
        /// <param name="log">The Out of the Box logger.</param>
        /// <returns>Task Status.</returns>
        [FunctionName(Constants.NotificationReceiverFunctionName)]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, Constants.Get, Constants.Post, Route = null)] HttpRequest req,
            [ServiceBus(Constants.WebHooksQueueName, Connection = Constants.NotificationReceiverServiceBusConnectionName)] ICollector<Message> incomingNotification,
            ILogger log)
        {
            try
            {
                log.LogInformation(Resource.RequestProcessed);

                // Response to Microsoft Graph including the validation token
                if (req != null && req.Query.ContainsKey(Constants.ValidationToken))
                {
                    var validationToken = req.Query[Constants.ValidationToken].ToString();
                    log.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.ValidationTokenReceived, validationToken));
                    return new OkObjectResult(validationToken);
                }
                else
                {
                    LoggerComponent.LogInformation(Resource.DocumentUpdateNotification, Constants.SPOCPI, nameof(LogCategory.NotificationReceiverFunctionApp));

                    string requestBody = string.Empty;
                    if (req != null)
                    {
                        using (StreamReader sr = new StreamReader(req.Body))
                        {
                            requestBody = await sr.ReadToEndAsync().ConfigureAwait(false);
                        }
                    }

                    var notifications = JsonConvert.DeserializeObject<GraphResponse<Notification>>(requestBody).Value;
                    if (notifications.Count > 0)
                    {
                        foreach (Notification res in notifications)
                        {
                            Message notif = new Message(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(res)))
                            {
                                MessageId = res.SubscriptionId + "/" + res.Resource,
                                ContentType = Constants.ContentType,
                            };

                            LoggerComponent.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.MessageCreated, notif.MessageId), Constants.SPOCPI, nameof(LogCategory.NotificationReceiverFunctionApp));
                            if (incomingNotification != null)
                            {
                                incomingNotification.Add(notif);
                            }
                        }
                    }

                    return new AcceptedResult();
                }
            }
            catch (Exception ex)
            {
                LoggerComponent.LogError(ex, Resource.Exception, Constants.SPOCPI, nameof(LogCategory.NotificationReceiverFunctionApp));
                throw;
            }
        }
    }
}