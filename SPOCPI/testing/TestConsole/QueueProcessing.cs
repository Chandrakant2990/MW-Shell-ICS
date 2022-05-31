namespace TestConsole
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Azure.ServiceBus.Management;

    class QueueProcessing
    {
        // Connection String for the namespace can be obtained from the Azure portal under the 
        // 'Shared Access policies' section.
        const string ServiceBusConnectionString = "[[Replace with Service Bus Connection String]]";
        const string QueueName = "webhooksnotificationqueue";
        static IQueueClient queueClient;

        public static void AddItems(string[] messages)
        {
            MainAsync(messages).GetAwaiter().GetResult();
        }

        public static void ReadItemsAsync()
        {
            var managementClient = new ManagementClient(ServiceBusConnectionString);
            var queue = managementClient.GetQueueRuntimeInfoAsync(QueueName).GetAwaiter().GetResult();
            var messageCount = queue.MessageCountDetails.ActiveMessageCount;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Active Messages Count =" + messageCount);
            PeekMessagesAsync(ServiceBusConnectionString, QueueName).GetAwaiter().GetResult();
        }

        static async Task PeekMessagesAsync(string connectionString, string queueName)
        {
            var receiver = new MessageReceiver(connectionString, queueName, ReceiveMode.PeekLock);

            if (File.Exists("C:/FilesToUpload/log.txt"))
            {
                // If file found, delete it    
                File.Delete("C:/FilesToUpload/log.txt");
            }

            Console.WriteLine("Browsing messages from Queue...");
            while (true)
            {
                try
                {
                    // Browse messages from queue
                    var message = await receiver.PeekAsync();
                    // If the returned message value is null, we have reached the bottom of the log
                    if (message != null)
                    {
                        // print the message
                        var body = Encoding.UTF8.GetString(message.Body);
                        lock (Console.Out)
                        {
                            using (StreamWriter w = File.AppendText("C:/FilesToUpload/log.txt"))
                            {
                                w.WriteLine(
                                    "\t\t\t\tMessage peeked: \n\t\t\t\t\t\tMessageId = {0}, \n\t\t\t\t\t\tSequenceNumber = {1}, \n\t\t\t\t\t\tEnqueuedTimeUtc = {2}," +
                                    "\n\t\t\t\t\t\tExpiresAtUtc = {5}, \n\t\t\t\t\t\tContentType = \"{3}\", \n\t\t\t\t\t\tSize = {4}, " +
                                    "  \n\t\t\t\t\t\tContent: [ {6} ]",
                                    message.MessageId,
                                    message.SystemProperties.SequenceNumber,
                                    message.SystemProperties.EnqueuedTimeUtc,
                                    message.ContentType,
                                    message.Size,
                                    message.ExpiresAtUtc,
                                    body);
                            }
                            //Console.ResetColor();
                        }
                    }
                    else
                    {
                        // We have reached the end of the log.
                        break;
                    }
                }
                catch (ServiceBusException e)
                {
                    if (!e.IsTransient)
                    {
                        Console.WriteLine(e.Message);
                        throw;
                    }
                }
            }
            await receiver.CloseAsync();
        }


        static async Task MainAsync(string[] messages)
        {
            queueClient = new QueueClient(ServiceBusConnectionString, QueueName);

            Console.WriteLine("======================================================");
            Console.WriteLine("Press ENTER key to exit after sending all the messages.");
            Console.WriteLine("======================================================");

            // Send Messages
            await SendMessagesAsync(messages);

            Console.ReadKey();

            await queueClient.CloseAsync();
        }

        static async Task SendMessagesAsync(string[] messages)
        {
            try
            {
                for (int i = 0; i < messages.Length; i++)
                {
                    // Create a new message to send to the queue
                    string messageBody = messages[i];
                    var message = new Message(Encoding.UTF8.GetBytes(messageBody));

                    // Write the body of the message to the console
                    Console.WriteLine($"Sending message: {messageBody}");

                    // Send the message to the queue
                    await queueClient.SendAsync(message);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }
    }
}