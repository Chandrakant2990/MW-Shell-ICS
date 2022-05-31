using Shell.SPOCPI.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Threading.Tasks;

namespace FunctionAppCloudTable2
{
    public class LogEntity : TableEntity
    {
        public string OriginalName { get; set; }
    }
    public class CloudTableDemo
    {
        static void Main(string[] args)
        {
            CreateEntry().Wait();
        }
        static private async Task CreateEntry()
        {
            CloudStorageAccount storageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials("spocpiwebhookstoredev", "[[Replace with Spocpi Webhook key]]"), true);
            CloudTable table = storageAccount.CreateCloudTableClient().GetTableReference("Configuration");
            TableQuery<ConfigurationEntity> rangeQuery = new TableQuery<ConfigurationEntity>().Where(
               TableQuery.CombineFilters(
                   TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                       "SPOCPI"),
                   TableOperators.And,
                   TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.NotEqual,
                       "t")));

            // Execute the query and loop through the results
            foreach (ConfigurationEntity entity in
                await table.ExecuteQuerySegmentedAsync(rangeQuery, null))
            {
                Console.WriteLine($"{entity.PartitionKey}\t{entity.RowKey}\t{entity.Timestamp}");
            }
        }
    }
}