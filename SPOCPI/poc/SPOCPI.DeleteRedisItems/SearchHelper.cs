using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SPOCPI.DeleteRedisItems
{
    /// <summary>
    /// Search Helper class.
    /// </summary>
    public static class SearchHelper
    {
        /// <summary>
        /// Searches the specified search index client.
        /// </summary>
        /// <typeparam name="T">Generic Parameter.</typeparam>
        /// <param name="searchClient">The search client.</param>
        /// <param name="searchText">The search text.</param>
        /// <param name="select">The select.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="facet">The facet.</param>
        /// <param name="orderBy">The order by.</param>
        /// <param name="skip">The skip.</param>
        /// <param name="top">The top.</param>
        /// <returns>Search Results.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Reviewed.")]
        public static async Task<SearchResults<T>> Search<T>(SearchClient searchClient, string searchText, IList<string> select = null, string filter = null, List<string> facet = null, string orderBy = "", int? skip = 0, int? top = 10)
        {
            SearchResults<T> searchResults = null;

            SearchOptions searchOptions = new SearchOptions()
            {
                IncludeTotalCount = true,
                Skip = skip,
                Size = top,
                Filter = filter,
                SearchMode = SearchMode.All,
                QueryType = SearchQueryType.Full,
                OrderBy = { orderBy },
            };

            if (select != null)
            {
                foreach (string selectField in select)
                {
                    searchOptions.Select.Add(selectField);
                }
            }

            if (searchClient != null)
            {
                searchResults = await searchClient.SearchAsync<T>(searchText, searchOptions).ConfigureAwait(false);
            }

            return searchResults;
        }

        /// <summary>
        /// Searches the specified search index client.
        /// </summary>
        /// <typeparam name="T">Generic parameter.</typeparam>
        /// <param name="serviceName">The search service name.</param>
        /// <param name="indexName">The search index name.</param>
        /// <param name="apiKey">The search API key.</param>
        /// <param name="searchText">The search text.</param>
        /// <param name="filter">The search filter.</param>
        /// <param name="select">The select parameters.</param>
        /// <returns>Search Results.</returns>
        public static async Task<SearchResults<T>> Search<T>(string serviceName, string indexName, string apiKey, string searchText, string filter, string[] select)
        {
            SearchIndexClient searchIndexClient = new SearchIndexClient(new Uri($"https://{serviceName}.search.windows.net/"), new AzureKeyCredential(apiKey));
            SearchClient searchClient = searchIndexClient.GetSearchClient(indexName);
            SearchOptions searchOptions = new SearchOptions()
            {
                Filter = filter,
            };

            if (select != null)
            {
                foreach (string selectField in select)
                {
                    searchOptions.Select.Add(selectField);
                }
            }

            return await searchClient.SearchAsync<T>(searchText, searchOptions).ConfigureAwait(false);
        }

        /// <summary>
        /// Refresh search index.
        /// </summary>
        /// <param name="searchServiceName">Search service name.</param>
        /// <param name="searchAdminKey">Search admin key.</param>
        /// <param name="indexerName">Search indexer name.</param>
        /// <returns>Task status.</returns>
        public static async Task RefreshSearchIndex(string searchServiceName, string searchAdminKey, string indexerName)
        {
            await SearchHelperClient.RunSearchIndexer(searchServiceName, searchAdminKey, indexerName).ConfigureAwait(false);
        }
    }
}
