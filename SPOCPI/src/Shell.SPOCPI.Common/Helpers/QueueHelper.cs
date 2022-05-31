// <copyright file="QueueHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Newtonsoft.Json;

    /// <summary>
    /// The azure service bus queue helper class.
    /// </summary>
    public class QueueHelper
    {
        /// <summary>
        /// The sender.
        /// </summary>
        private static IMessageSender sender;

        /// <summary>
        /// The receiver.
        /// </summary>
        private static IMessageReceiver receiver;

        /// <summary>
        /// The logger.
        /// </summary>
        private ILoggerComponent logger;

        /// <summary>
        /// The configuration.
        /// </summary>
        private IConfiguration config;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueHelper" /> class.
        /// </summary>
        /// <param name="loggerComponent">The logger.</param>
        /// <param name="configuration">The configuration.</param>
        public QueueHelper(ILoggerComponent loggerComponent, IConfiguration configuration)
        {
            this.logger = loggerComponent ?? throw new ArgumentNullException(nameof(loggerComponent));
            this.config = configuration ?? throw new ArgumentNullException(nameof(configuration));

            this.InitializeQueueClient(
                                       KeyVaultHelper.GetSecret(this.config.GetConfigValue(Common.Resource.ConfigCPServiceBusConnectionString))?.Result?.ToString(),
                                       this.config.GetConfigValue(Common.Resource.ConfigCPOutputQueueName),
                                       KeyVaultHelper.GetSecret(this.config.GetConfigValue(Common.Resource.ConfigCPServiceBusConnectionString))?.Result?.ToString(),
                                       this.config.GetConfigValue(Common.Resource.ConfigCPInputQueueName));
        }

        /// <summary>
        /// Pushes the message into Azure Service Bus Queue.
        /// </summary>
        /// <typeparam name="T">The type of the item to be pushed into the queue. The type must be JSON serializable.</typeparam>
        /// <param name="item">The item.</param>
        /// <returns>The task status.</returns>
        /// <exception cref="System.ArgumentNullException">The argument is null - item.</exception>
        /// <exception cref="System.ArgumentException">The type is not serializable - item.</exception>
        public async Task PushMessageIntoQueue<T>(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!item.GetType().IsSerializable)
            {
                throw new ArgumentException(Resource.QHTypeNotSerializable, nameof(item));
            }

            try
            {
                Message message = new Message();
                message.ContentType = Constants.ContentType;
                message.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
                await sender.SendAsync(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.Common));
                throw;
            }
        }

        /// <summary>
        /// Pushes the message into Azure Service Bus Queue.
        /// </summary>
        /// <typeparam name="T">The type of the item to be pushed into the queue. The type must be JSON serializable.</typeparam>
        /// <param name="items">The list of items.</param>
        /// <param name="messageMaxItems">The maximum number of items in a Service Bus queue message.</param>
        /// <returns>The task status.</returns>
        /// <exception cref="ArgumentNullException">The argument is null - item.</exception>
        /// <exception cref="ArgumentException">The type is not serializable - items.</exception>
        /// <remarks>If the number of items in a message is high, the service bus message send might fail due to maximum size limit.</remarks>
        public async Task PushMessagesIntoQueue<T>(IList<T> items, int messageMaxItems = 10)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (items.Count < 1)
            {
                return;
            }

            try
            {
                int maxSize = 150000;
                IList<Message> messages = new List<Message>();
                List<T> listItems = new List<T>(messageMaxItems);
                int count = 0;
                foreach (var item in items)
                {
                    if (count == messageMaxItems)
                    {
                        this.AddMessage(messages, listItems, maxSize);
                        listItems.Clear();
                        count = 0;
                    }

                    listItems.Add(item);
                    count++;
                }

                if (listItems.Count > 0)
                {
                    this.AddMessage(messages, listItems, maxSize);
                }

                if (messages.Count > 0)
                {
                    foreach (var item in messages)
                    {
                        await sender.SendAsync(item).ConfigureAwait(false);
                    }
                }
            }
            catch (MessageSizeExceededException ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.Common));
                throw;
            }
            catch (ServiceBusException ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.Common));
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.Common));
                throw;
            }
        }

        /// <summary>Pushes the message into Azure Service Bus Queue.</summary>
        /// <typeparam name="T">The type of the item to be pushed into the queue. The type must be JSON serializable.</typeparam>
        /// <param name="items">The list of items.</param>
        /// <returns>The task status.</returns>
        /// <exception cref="ArgumentNullException">The argument is null - items.</exception>
        public async Task PushMessagesIntoQueue<T>(IList<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (items.Count < 1)
            {
                return;
            }

            try
            {
                IList<Message> messages = new List<Message>();
                Message message;
                int size = 0;
                int maxSize = 150000;
                int count = 0;
                foreach (var item in items)
                {
                    if (size > maxSize || count == 100)
                    {
                        await sender.SendAsync(messages).ConfigureAwait(false);
                        size = 0;
                        count = 0;
                        messages.Clear();
                    }

                    message = new Message();
                    message.ContentType = Constants.ContentType;
                    message.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
                    size += message.Body.Length;
                    count++;
                    messages.Add(message);
                }

                if (messages.Count > 0)
                {
                    await sender.SendAsync(messages).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.Common));
                throw;
            }
        }

        /// <summary>Gets the entity from messages.</summary>
        /// <typeparam name="T">The type of the item to be retrieved from the queue message. The type must be JSON serializable.</typeparam>
        /// <param name="message">The message.</param>
        /// <returns>The item of type specified in the type parameter.</returns>
        /// <exception cref="ArgumentNullException">The argument is null - message.</exception>
        public T GetEntityFromMessages<T>(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            try
            {
                if (message.Body != null)
                {
                    T entity = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body));
                    return entity;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.Common));
                throw;
            }

            return default;
        }

        /// <summary>Gets the messages from queue.</summary>
        /// <typeparam name="T">The type of the item to be received from the queue message. The type must be JSON serializable.</typeparam>
        /// <param name="numberOfMessages">The number of messages.</param>
        /// <returns>The list of queue messages.</returns>
        public async Task<IList<Message>> GetMessagesFromQueue<T>(int numberOfMessages = 1)
        {
            try
            {
                // Browse messages from queue
                var messages = await receiver.ReceiveAsync(numberOfMessages).ConfigureAwait(false);
                if (messages != null)
                {
                    return messages;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.Common));
                throw;
            }

            return default;
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Reviewed.")]

        /// <summary>Closes the receiver connection asynchronous.</summary>
        /// <returns>The task status.</returns>
        public async Task CloseReceiverConnectionAsync()
        {
            await receiver.CloseAsync().ConfigureAwait(false);
        }

        /// <summary>Completes the message asynchronous.</summary>
        /// <param name="message">The message.</param>
        /// <returns>The task status.</returns>
        /// <exception cref="ArgumentNullException">The argument is null - message.</exception>
        public async Task CompleteMessageAsync(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            try
            {
                await receiver.CompleteAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, Constants.SPOCPI, nameof(LogCategory.Common));
                throw;
            }
        }

        /// <summary>
        /// Initializes the queue client.
        /// </summary>
        /// <param name="connectionStringSender">The connection string sender.</param>
        /// <param name="sendQueueName">Name of the send queue.</param>
        /// <param name="connectionStringReceiver">The connection string receiver.</param>
        /// <param name="receiveQueueName">Name of the receive queue.</param>
        private void InitializeQueueClient(string connectionStringSender, string sendQueueName, string connectionStringReceiver, string receiveQueueName)
        {
            ////var connectionString = "[[Replace with Connection String]]";
            ////var sendQueueName = "changeprocessoutputqueue";
            ////var receiveQueueName = "webhooksnotificationqueue";
            receiver = new MessageReceiver(connectionStringReceiver, receiveQueueName, ReceiveMode.PeekLock);
            sender = new MessageSender(connectionStringSender, sendQueueName, RetryPolicy.Default);
        }

        /// <summary>
        /// Adds the message.
        /// </summary>
        /// <typeparam name="T">The type of the item to be pushed into the queue. The type must be JSON serializable.</typeparam>
        /// <param name="messages">The messages.</param>
        /// <param name="listItems">The list items.</param>
        /// <param name="maxSize">The maximum size.</param>
        /// <exception cref="Exception">The message has exceeded maximum size of {maxSize} per message. Please try with lesser number of messageMaxItems.</exception>
        private void AddMessage<T>(IList<Message> messages, List<T> listItems, int maxSize)
        {
            var message = new Message();
            message.ContentType = Constants.ContentType;
            message.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(listItems));
            if (message.Body.Length > maxSize)
            {
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, Resource.MessageSizeExceeded, maxSize));
            }

            messages.Add(message);
        }

        /// <summary>Exceptions the received handler.</summary>
        /// <param name="exceptionReceivedEventArgs">The <see cref="ExceptionReceivedEventArgs"/> instance containing the event data.</param>
        /// <returns>The Task status.</returns>
        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.ExceptionContext, context.ClientId), Constants.SPOCPI, nameof(LogCategory.Common));
            this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.ContextEndpoint, context.Endpoint), Constants.SPOCPI, nameof(LogCategory.Common));
            this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.ContextEntityPath, context.EntityPath), Constants.SPOCPI, nameof(LogCategory.Common));
            this.logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resource.ContextAction, context.Action), Constants.SPOCPI, nameof(LogCategory.Common));
            return Task.CompletedTask;
        }
    }
}
