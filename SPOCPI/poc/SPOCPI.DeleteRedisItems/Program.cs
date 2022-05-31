using Azure.Search.Documents.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Shell.SPOCPI.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using CloudStorageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount;

namespace SPOCPI.DeleteRedisItems
{
    class Program
    {
        private static string searchUpdateKey;
        private static string spocpiSearchServiceName;
        private static string DocumentTrackingIndexName;
        private static string RedisConnectionString;
        private static string PartitionKey;
        private static string Time;
        private static string appInsightsInstrumentationKey;
        private static TelemetryConfiguration telemetryConfig;
        private static TelemetryClient telemetryClient;

        /// <summary>
        /// Main method of Program
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            InitConfig();
            Lazy<ConnectionMultiplexer> lazyConnection = null;
            lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                return ConnectionMultiplexer.Connect(RedisConnectionString);
            });
            IDatabase cache = lazyConnection.Value.GetDatabase();

            LogInformation("Started Deletion of Redis Data", Constants.ApplicationNameValue, Constants.ModuleNameValue);

            Task.Run(async () =>
            {
                await DeleteRedisItems(cache).ConfigureAwait(false);

            }).GetAwaiter().GetResult();
            Console.ReadLine();

            LogInformation("Finished Deletion of Redis Data", Constants.ApplicationNameValue, Constants.ModuleNameValue);

        }

        #region "Private Methods"        

        /// <summary>
        /// Identify all failed documents from Async tracking table
        /// </summary>
        /// <returns></returns>
        private static async Task DeleteRedisItems(IDatabase cache)
        {
            try
            {
                SearchHelperClient searchHelperClient = new SearchHelperClient(spocpiSearchServiceName, DocumentTrackingIndexName, searchUpdateKey);
                var listOfRecords = new List<SearchResult<DocumentEntity>>();
                int index = 0;
                int processedItems = 0;

                do
                {
                    if (index < 100000)
                    {
                        int draw = 1;
                        int start = index;
                        int length = 500;
                        string orderColumn = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Constants.Timestamp, "desc");
                        string filter = string.Format("PartitionKey eq '{0}' and Timestamp lt {1}", PartitionKey, Time);
                        string queryText = "*";
                        IList<string> selectProperties = new[] { Constants.PartitionKey, Constants.RowKey, Constants.Timestamp};

                        var searchResults = await SearchHelper.Search<DocumentEntity>(searchHelperClient.SearchClient, queryText, select: selectProperties, orderBy: orderColumn, skip: start, top: length, filter: filter).ConfigureAwait(true);
                        var resultObject = new DataTableObject<DocumentEntity>(searchResults.GetResults(), draw, (int)searchResults.TotalCount, (int)searchResults.TotalCount);

                        if (resultObject != null && resultObject.Data != null && resultObject.Data.Any())
                        {
                            listOfRecords = resultObject.Data.ToList();
                            index = index + length;

                            foreach (SearchResult<DocumentEntity> record in listOfRecords)
                            {
                                try
                                {
                                    DeleteFromRedis(record.Document, cache);
                                    processedItems++;
                                    Console.WriteLine("ProcessedItems= " + processedItems + "TimeStamp= "+record.Document.Timestamp);
                                    if (processedItems != 0 && processedItems % 500 == 0)
                                    {
                                        LogMetric("Deleted Items from Redis", processedItems);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogError(ex, string.Format("Exception occurred in ReprocessFailedOCRItems for record of PartitionKey: {0}, RowKey: {1}", record.Document.PartitionKey, record.Document.RowKey), Constants.ApplicationNameValue, Constants.ModuleNameValue);
                                }
                            }
                        }
                    }
                }
                while (listOfRecords.Count == 500);
            }
            catch (Exception ex)
            {
                LogError(ex, string.Format("Exception occurred in ReprocessFailedOCRItems(), Error Message: {0}", ex.Message), Constants.ApplicationNameValue, Constants.ModuleNameValue);
            }
        }

        /// <summary>
        /// Builds Cache key.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <returns>Cache key.</returns>
        public static string BuildCacheKey(string workload, string key)
        {
            return workload + "|" + key;
        }

        /// <summary>
        /// Reprocess document for image extraction
        /// </summary>
        /// <param name="document"></param>
        private static void DeleteFromRedis(DocumentEntity document, IDatabase cache)
        {
            try
            {
                cache.KeyDelete(BuildCacheKey("SPOCPI", $"{Constants.DocumentTrackingTableName}-{document.PartitionKey}-{document.RowKey}"));
            }
            catch
            {

            }
        }


        /// <summary>
        /// Log the metric information into App insights.
        /// </summary>
        /// <param name="metricName">The metric name.</param>
        /// <param name="metricValue">The metric value.</param>
        private static void LogMetric(string metricName, int metricValue)
        {
            var metricTelemetry = new MetricTelemetry()
            {
                Name = metricName,
                Count = metricValue
            };

            telemetryClient.TrackMetric(metricTelemetry);
        }

        /// <summary>
        /// Initilyze Configuration
        /// </summary>
        private static void InitConfig()
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();
            var configurationSettings = new ConfigurationSettings();
            configuration.GetSection("ConfigurationSettings").Bind(configurationSettings);

            // SPOCPI search service configuration
            searchUpdateKey = configurationSettings.SearchUpdateKey;
            spocpiSearchServiceName = configurationSettings.SPOCPISearchServiceName;
            DocumentTrackingIndexName = configurationSettings.DocumentTrackingIndexName;
            RedisConnectionString = configurationSettings.RedisConnectionString;
            PartitionKey = configurationSettings.PartitionKey;
            Time = configurationSettings.Time;

            // application insights for logging
            appInsightsInstrumentationKey = configurationSettings.AppInsightsInstrumentationKey;
            telemetryConfig = TelemetryConfiguration.CreateDefault();
            telemetryClient = new TelemetryClient(telemetryConfig);
            if (string.IsNullOrEmpty(telemetryClient.InstrumentationKey))
            {
                telemetryClient.InstrumentationKey = appInsightsInstrumentationKey;
            }
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="ex">Logs error with exception.</param>
        /// <param name="message">Logs error with Message.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        public static void LogError(Exception ex, string message, string applicationName, string moduleName)
        {
            Dictionary<string, string> telemetricProps = new Dictionary<string, string>()
            {
                [Constants.AppInsightsMessage] = message,
                [Constants.AppInsightsCategory] = $"{applicationName}-{moduleName}",
                [Constants.ApplicationName] = applicationName,
                [Constants.ModuleName] = moduleName,
            };

            telemetryClient.TrackException(ex, telemetricProps);
            telemetryClient.TrackTrace(message, SeverityLevel.Error, telemetricProps);
        }

        /// <summary>
        /// Logs the information.
        /// </summary>
        /// <param name="message">Logs information message.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        public static void LogInformation(string message, string applicationName, string moduleName)
        {
            Dictionary<string, string> telemetricProps = new Dictionary<string, string>()
            {
                [Constants.AppInsightsCategory] = $"{applicationName}-{moduleName}",
                [Constants.ApplicationName] = applicationName,
                [Constants.ModuleName] = moduleName,
            };

            telemetryClient.TrackTrace(message, SeverityLevel.Information, telemetricProps);
            FlushTelemetryClient();
        }

        /// <summary>
        /// Flushes the telemetry client.
        /// </summary>
        public static void FlushTelemetryClient()
        {
            telemetryClient.Flush();
        }

        #endregion
    }
}
