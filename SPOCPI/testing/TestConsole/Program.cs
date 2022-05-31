using Shell.SPOCPI.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using System;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics.Tracing;
using Common.Core.Entities;
using System.Runtime.InteropServices;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Uri uri = new Uri("https://www.bing.com");

                var requestBody = "{\"value\":[{\"subscriptionId\":\"5615bc84-7d7d-4a85-8798-33f0c51b9a03\",\"clientState\":\"21f5f690-54b5-4cd9-af68-798eb211c58f\",\"resource\":\"drive/b!Rw7nP7zzYEidCG7orV4Un1k7CxKzaXNAqBsTVEGd71lX041Kf_z7RpMJxCiJBNgO/root\",\"fullCrawl\":\"true\"}]}";
                var notifications = JsonConvert.DeserializeObject<GraphResponse<Notification>>(requestBody).Value;
            }
            catch (Exception ex)
            {
                throw;
            }
            GraphServiceClient client = GraphHelper.GetGraphClient("76775f48-19b3-4386-b7db-ce8acf08c644", "ebdaa46d-c34f-4721-a917-d3b44ad75ae4", "toN*-+EPB9]X25WQKQvyH02b/?PBz*fg");
            //var a = client.Users.Request().GetAsync().Result;
            //CreateEntry().Wait();

            //SearchDocuments();

            #region Deleting items from Document Tracking table based on the partition key value. 
            var partitionKey = "YiFSdzduUDd6ellFaWRDRzdvclY0VW4xazdDeEt6YVhOQXFCc1RWRUdkNzFsWDA0MUtmX3o3UnBNSnhDaUpCTmdP";

            GetAndDeleteItemsFromDocumentTrackingTable(partitionKey);
            #endregion
        }

        private static void SearchDocuments()
        {
            // Get index
            var indexName = "subscriptionstable-index"; // documenttrackingtable-index

            try
            {
                using (SearchIndexClient indexClient = new SearchIndexClient("spocpisearch", indexName, new SearchCredentials("67D686EB38B284358ACFF0EDC6C98995")))
                {
                    // Query all
                    SearchContinuationToken continuationToken = null;
                    do
                    {
                        DocumentSearchResult<Document> searchResult;
                        try
                        {
                            searchResult = indexClient.Documents.Search<Document>(string.Empty);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Error during AzureSearch");
                        }

                        List<string> azureDocsToDelete =
                        searchResult
                            .Results
                            //.Where(a => a.Document["RowKey"].ToString() == "01MW4CZNN4NPFY55PDWNFZMQDKMBJGIPWC")
                            .Select(r => r.Document["Key"].ToString())
                            .ToList();

                        // Delete all
                        try
                        {
                            if (azureDocsToDelete != null && azureDocsToDelete.Count > 0)
                            {
                                var batch = IndexBatch.Delete("Key", azureDocsToDelete);
                                indexClient.Documents.Index(batch);
                            }
                        }
                        catch (IndexBatchException ex)
                        {
                            throw new Exception($"Failed to delete documents: {string.Join(", ", ex.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key))}");
                        }
                        continuationToken = searchResult.ContinuationToken;
                    }
                    while (continuationToken != null);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static private async Task CreateTableEntry()
        {
            // Create a new customer entity.
            ConfigurationEntity se = new ConfigurationEntity()
            {
                PartitionKey = "SPOCPI",
                RowKey = "Abhi",
                ConfigKey = "CK1",
                ConfigValue = "CV1"
            };
            CloudStorageAccount storageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials("spocpiwebhookstoredev", "[[Replace with SPOCPI webhook Key]]"), true);
            CloudTable table = storageAccount.CreateCloudTableClient().GetTableReference("Configuration");
            TableOperation insertOperation = TableOperation.Insert(se);

            // Execute the insert operation.
            var a = await table.ExecuteAsync(insertOperation);
            Console.WriteLine(a.Result);
        }

        private void SubscriptionHelper()
        {
            var builder = new ConfigurationBuilder()
        .SetBasePath(System.IO.Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            Console.WriteLine(configuration.GetSection("isLoggingEnabled").Value);

            //Add items to queue 
            //QueueProcessing.AddItems(string[] messages);

            //Read items from Queue
            //QueueProcessing.ReadItemsAsync();

            //MockChange.RunAsync().GetAwaiter().GetResult();


            /*

            Subscription subscription = new Subscription
            {
                ChangeType = "updated",
                ClientState = "SampleClientState1",
                ExpirationDateTime = DateTime.UtcNow.AddDays(2),
                NotificationUrl = "[[Replace with Notification URL with Key]]",
                Resource = "drives/b!Rw7nP7zzYEidCG7orV4Un1k7CxKzaXNAqBsTVEGd71mF_15EhMDKT5Vz2hXR9kdl/root"
            };

            //Console.WriteLine(GraphHelper.GetAccessToken().Result);
            //GraphServiceClient client = GraphHelper.GetGraphClient();
            //var a = client.Users.Request().GetAsync().Result;
            //var b = client.Sites.Root.Lists.Request().GetAsync().Result;
            //var subscriptionId = ((Subscription)GraphHelper.CallGraphAPIAsync<Subscription>("https://graph.microsoft.com/v1.0/subscriptions", HttpMethod.Post, subscription, Common.Core.Constants.contentTypeApplicationJson).Result).Id;
            //GraphHelper.CallGraphAPIAsync<Subscription>($"https://graph.microsoft.com/v1.0/subscriptions/{subscriptionId}", HttpMethod.Delete, null, null).Wait();
            */
        }

        private static async Task GetAndDeleteItemsFromDocumentTrackingTable(string partitionKey)
        {
            int count = 0;
            CloudTable table = null;
            var documentTrackingTable = "DocumentTracking";
            string connectionString = string.Empty;
            //connectionString = "[[Replace with Connection String]]"; // TEST
            connectionString = "[[Replace with Connection String]]"; // DEV
            if (!string.IsNullOrWhiteSpace(documentTrackingTable) && !string.IsNullOrWhiteSpace(connectionString))
            {
                table = GetTableEntity(connectionString, documentTrackingTable);
            }

            var deleteStatusFilter = CreateFilterQueryString(Common.Core.Constants.PartitionKey, "equal", partitionKey);

            var itemsToBeDeleted = await GetTableResultBasedOnQueryFilter<DriveItemEntity>(table, deleteStatusFilter).ConfigureAwait(false);

            if (itemsToBeDeleted != null && itemsToBeDeleted.Count > 0)
            {
                Console.WriteLine($"Total items found are {itemsToBeDeleted.Count}");

                await BatchEntities(table, itemsToBeDeleted).ConfigureAwait(false);

                Console.ReadLine();
            }
        }

        /// <summary>
        /// DeleteAllSubscriptionForDrive is used to delete all the subscription for a specific drive (document library). This method is currently not in use and probably will be deleted from PROD.
        /// </summary>
        /// <param name="client">The GraphServiceClient object.</param>
        /// <param name="driveId">The drive id of the subscription.</param>
        /// <param name="libraryUrl">The document library url.</param>
        /// <param name="subscriptionId">The subscription id for the library.</param>
        /// <returns></returns>
        private static async Task DeleteAllSubscriptionForDrive(GraphServiceClient client, string driveId, string libraryUrl, string subscriptionId)
        {
            try
            {
                var subscriptions = await client.Subscriptions.Request().GetAsync().ConfigureAwait(false);
                if (subscriptions != null && subscriptions.CurrentPage != null && subscriptions.CurrentPage.Count > 0)
                {
                    foreach (var subscription in subscriptions.CurrentPage)
                    {
                        if (subscription.Resource.Trim() == driveId.Trim() && subscription.Id.Trim() == subscriptionId.Trim())
                        {
                            Console.WriteLine($"Deleting subscription with Id : {subscription.Id} for the library : {libraryUrl}");
                            //var isItemDeleted = await SharePointHelper.DeleteSubscription(client, subscription.Id).ConfigureAwait(false);
                            //if (isItemDeleted)
                            //{
                            //    Console.WriteLine($"Subscription with ID : {subscription.Id} deleted for the library : {libraryUrl}");
                            //}
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static async Task GetAllSubscriptionForDrive(GraphServiceClient client, string driveId, string subscriptionId)
        {
            try
            {
                var subscriptions = await client.Subscriptions.Request().GetAsync().ConfigureAwait(true);
                if (subscriptions != null && subscriptions.CurrentPage != null && subscriptions.CurrentPage.Count > 0)
                {
                    foreach (var subscription in subscriptions.CurrentPage)
                    {


                        if (subscription.Resource.Trim() == driveId.Trim())
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine($"Subscription id is : {subscription.Id}");
                            Console.WriteLine($"Notification Url is : {subscription.NotificationUrl}");
                            Console.WriteLine($"Resource id is : {subscription.Resource}");
                            if (subscription.Id.Trim() == subscriptionId.Trim())
                            {
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                                Console.WriteLine($"SubscriptionId found : {subscriptionId}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static async Task<List<T>> GetTableResultBasedOnQueryFilter<T>(CloudTable cloudTable, string filter)
            where T : TableEntity, new()
        {
            List<T> entities = new List<T>();
            try
            {
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    var query = new TableQuery<T>().Where(filter);
                    TableContinuationToken continuationToken = null;
                    do
                    {
                        TableQuerySegment<T> tableQuerySegment = null;
                        if (cloudTable != null)
                        {
                            tableQuerySegment = await cloudTable.ExecuteQuerySegmentedAsync<T>(query, continuationToken).ConfigureAwait(false);
                        }

                        if (tableQuerySegment != null && tableQuerySegment.Results != null)
                        {
                            foreach (var entity in tableQuerySegment.Results)
                            {
                                entities.Add(entity);
                            }
                        }

                        continuationToken = tableQuerySegment.ContinuationToken;
                    }
                    while (continuationToken != null);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

            return entities;
        }

        public static CloudTable GetTableEntity(string connectionString, string tableName)
        {
            CloudTable table = null;
            if (!string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(tableName))
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                table = tableClient.GetTableReference(tableName);
            }

            return table;
        }

        public static string CreateFilterQueryString(string variable, string queryComparisonOperator, string value)
        {
            var filter = string.Empty;
            if (!string.IsNullOrWhiteSpace(queryComparisonOperator))
            {
                // switch can be extended further by adding additional case statements
                switch (queryComparisonOperator.ToUpperInvariant())
                {
                    case "EQUAL":
                        filter = TableQuery.GenerateFilterCondition(variable, QueryComparisons.Equal, value);
                        break;
                    case "NOTEQUAL":
                        filter = TableQuery.GenerateFilterCondition(variable, QueryComparisons.NotEqual, value);
                        break;
                    default:
                        break;
                }
            }

            return filter;
        }

        public static async Task<TableResult> DeleteTableItem<T>(CloudTable cloudTable, TableEntity entity)
            where T : TableEntity
        {
            entity.ETag = "*";
            TableOperation delete = TableOperation.Delete(entity);
            var tableResult = new TableResult();
            if (cloudTable != null)
            {
                tableResult = await cloudTable.ExecuteAsync(delete).ConfigureAwait(false);
            }

            return tableResult;
        }

        /// <summary>
        /// This method is used to batch entities in a number of 100 items.
        /// </summary>
        /// <typeparam name="T">Generic entity object.</typeparam>
        /// <param name="table">The CloudTable object.</param>
        /// <param name="entities">The TableEntity object collection.</param>
        /// <returns>Returns a task object.</returns>
        public static async Task BatchEntities<T>(CloudTable table, List<T> entities)
            where T : TableEntity
        {

            var maximumBatchSize = 100;
            if (table != null && entities != null && entities.Count > 0)
            {
                try
                {
                    for (int i = 0; i < entities.Count; i += maximumBatchSize)
                    {
                        List<T> batchEntities = new List<T>();
                        var batchItems = entities.Skip(i).Take(maximumBatchSize).ToList();
                        Console.WriteLine($"batchItems size is {batchItems.Count}");
                        foreach (var item in batchItems)
                        {
                            //Console.WriteLine($"Partitionkey is => {item.PartitionKey} & Rowkey is => {item.RowKey}");
                            batchEntities.Add(item);
                        }
                        await DeleteEntitiesInBatch(table, batchEntities).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// This method is used to delete items from the Azure Storage table as a batch of 100 items.
        /// </summary>
        /// <typeparam name="T">Generic entity object.</typeparam>
        /// <param name="table">The CloudTable object.</param>
        /// <param name="entitiesToDelete">The TableEntity object collection.</param>
        /// <returns>Returns a task object.</returns>
        public static async Task DeleteEntitiesInBatch<T>(CloudTable table, List<T> entitiesToDelete)
            where T : TableEntity
        {
            TableBatchOperation tableBatchOperation = new TableBatchOperation();
            try
            {
                if (table != null && entitiesToDelete != null && entitiesToDelete.Count > 0)
                {
                    Console.WriteLine($"{entitiesToDelete.Count} items are going to be deleted in a batch");
                    Parallel.ForEach(entitiesToDelete, entity =>
                    {
                        tableBatchOperation.Add(TableOperation.Delete(entity));
                    });

                    //foreach (T entity in entitiesToDelete)
                    //{
                    //    tableBatchOperation.Add(TableOperation.Delete(entity));
                    //}

                    await table.ExecuteBatchAsync(tableBatchOperation).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

    }
}
