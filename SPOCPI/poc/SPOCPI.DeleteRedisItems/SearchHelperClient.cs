using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using DocumentFormat.OpenXml.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPOCPI.DeleteRedisItems
{
    /// <summary>
    /// Search helper client class.
    /// </summary>
    public class SearchHelperClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchHelperClient"/> class.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="queryKey">The query key.</param>
        public SearchHelperClient(string serviceName, string indexName, string queryKey)
        {
            SearchIndexClient searchIndexClient = new SearchIndexClient(new Uri($"https://{serviceName}.search.windows.net/"), new AzureKeyCredential(queryKey));
            this.SearchClient = searchIndexClient.GetSearchClient(indexName);
        }

        /// <summary>
        /// Gets or sets the search client.
        /// </summary>
        /// <value>
        /// The search client.
        /// </value>
        public SearchClient SearchClient { get; set; }

        [SuppressMessage("Microsoft.Design", "CA2201:DoNotRaiseReservedExceptionTypes", Justification = "Reviewed.")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Reviewed.")]

        /// <summary>
        /// Deletes documents from search index.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="queryKey">The query key.</param>
        /// <param name="columnName">The column Name.</param>
        /// <param name="columnValue">The column value.</param>
        /// <returns><see cref="true"/> if document is deleted, else returns <see cref="false"/>.</returns>
        public static Task<bool> DeleteDocumentsFromIndex(string serviceName, string indexName, string queryKey, string columnName, string columnValue)
        {
            bool isDeleted = true;
            try
            {
                SearchIndexClient searchIndexClient = new SearchIndexClient(new Uri($"https://{serviceName}.search.windows.net/"), new AzureKeyCredential(queryKey));
                SearchClient trackingTableClient = searchIndexClient.GetSearchClient(indexName);

                SearchOptions searchOptions = new SearchOptions()
                {
                    IncludeTotalCount = true,
                    SearchMode = SearchMode.All,
                    QueryType = SearchQueryType.Full,
                };

                searchOptions.Select.Add(columnName);
                searchOptions.Select.Add("Key");

                SearchResults<SearchDocument> searchResults = trackingTableClient.Search<SearchDocument>(columnValue, searchOptions);
                List<string> documentsToDelete = searchResults.GetResults()
                                                              .Where(a => a.Document[columnName].Equals(columnValue))
                                                              .Select(r => r.Document["Key"].ToString()).ToList();
                try
                {
                    trackingTableClient.DeleteDocuments("Key", documentsToDelete);
                }
                catch (RequestFailedException ex)
                {
                    throw new SystemException(string.Format(CultureInfo.InvariantCulture, "DeletionFailed", string.Join(", ", ex.Data.Values)));
                }
            }
            catch
            {
                isDeleted = false;
            }

            return Task.FromResult(isDeleted);
        }

        /// <summary>
        /// Deletes the index of the documents from.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="queryKey">The query key.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="columnName1">The column name1.</param>
        /// <param name="columnName2">The column name2.</param>
        /// <returns><see cref="true"/> if document is deleted, else returns <see cref="false"/>.</returns>
        [SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Reviewed.")]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Reviewed.")]
        public static Task<bool> DeleteDocumentsFromIndex(string serviceName, string indexName, string queryKey, string filter, string columnName1, string columnName2)
        {
            bool isDeleted = true;
            try
            {
                SearchIndexClient searchIndexClient = new SearchIndexClient(new Uri($"https://{serviceName}.search.windows.net/"), new AzureKeyCredential(queryKey));
                SearchClient trackingTableClient = searchIndexClient.GetSearchClient(indexName);

                SearchOptions searchOptions = new SearchOptions()
                {
                    IncludeTotalCount = true,
                    SearchMode = SearchMode.All,
                    Filter = filter,
                    QueryType = SearchQueryType.Full,
                };

                searchOptions.Select.Add(columnName1);
                searchOptions.Select.Add(columnName2);
                searchOptions.Select.Add("Key");

                SearchResults<SearchDocument> searchResults = trackingTableClient.Search<SearchDocument>(Constants.WildCardSearch, searchOptions);
                List<string> documentsToDelete = searchResults.GetResults()
                                                              .Select(r => r.Document["Key"].ToString()).ToList();
                try
                {
                    trackingTableClient.DeleteDocuments(documentsToDelete);
                }
                catch (RequestFailedException ex)
                {
                    throw new SystemException(string.Format(CultureInfo.InvariantCulture, "DeletionFailed", string.Join(", ", ex.Data.Values)));
                }
            }
            catch
            {
                isDeleted = false;
            }

            return Task.FromResult(isDeleted);
        }

        /// <summary>
        /// This method is used to run the indexer.
        /// </summary>
        /// <param name="serviceName">The search service name.</param>
        /// <param name="queryKey">The query key.</param>
        /// <param name="indexerName">The search indexer name.</param>
        /// <returns>The Task object.</returns>
        public static async Task RunSearchIndexer(string serviceName, string queryKey, string indexerName)
        {
            SearchIndexerClient searchIndexerClient = new SearchIndexerClient(new Uri($"https://{serviceName}.search.windows.net/"), new AzureKeyCredential(queryKey));
            var indexStatus = await searchIndexerClient.GetIndexerStatusAsync(indexerName);
            if (indexStatus.Value.LastResult.Status != IndexerExecutionStatus.InProgress)
            {
                await searchIndexerClient.RunIndexerAsync(indexerName).ConfigureAwait(false);
            }
        }
    }
}
