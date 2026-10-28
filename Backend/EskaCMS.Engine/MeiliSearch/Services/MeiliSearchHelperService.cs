using EskaCMS.Engine.MeiliSearch.Services.Intefaces;
using EskaCMS.Infrastructure.Helpers;
using Meilisearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static MimeDetective.Definitions.Default.FileTypes;

namespace EskaCMS.Engine.MeiliSearch.Services
{
    // Implementation
    public class MeilisearchHelperService : IMeiliSearchHelperService
    {
        private readonly MeilisearchClient _client;
        private readonly ILogger<MeilisearchHelperService> _logger;
        private readonly IConfiguration _configuration;

        public MeilisearchHelperService(
            IConfiguration configuration,
            ILogger<MeilisearchHelperService> logger,
            MeilisearchClient client

            )
        {
            _configuration = configuration;
            _logger = logger;

            _client = client;

         
        }

        // Initialize an index with settings
        public async Task InitializeIndex(string indexName, string primaryKey = "MeiliId")
            
        {
            try
            {
            
                try
                {

                    await _client.GetIndexAsync(indexName);

                    _logger.LogInformation($"Index `{indexName}` already exists");
                }
                catch (MeilisearchApiError ex) when (ex.Code == "index_not_found")
                {
                    await _client.CreateIndexAsync(indexName, primaryKey);

                    _logger.LogInformation($"Created index `{indexName}`");
                }
                await ConfigureIndexSettings(indexName);
            }catch(Exception ex)
            {
                throw;
            }
        }

        // Configure searchable, filterable, and sortable attributes for each index
        private async Task ConfigureIndexSettings(string indexName)
        {
            var index = _client.Index(indexName);

            var commonFilterableAttributes = new[]
            {
        "MeiliId",
        "id",
        "entityType",
        "siteId",

        // details.*
        "details.name",
        "details.normalizedName",
        "details.shortDescription",
        "details.description",
        "details.specification",
        "details.metaDescription",
        "details.slug",
        "details.keywords",
        "details.thumbnailImageUrl",
        "details.cultureId",
        "details.id",
        "details.additionalMetaTags"
    };

            var productOnlyAttributes = new[]
            {
        "details.branchId",
        "details.countryId"
    };

            var settings = new Settings
            {
                FilterableAttributes = indexName == "products"
                    ? commonFilterableAttributes.Concat(productOnlyAttributes).ToArray()
                    : commonFilterableAttributes
            };

            await index.UpdateSettingsAsync(settings);

            _logger.LogInformation($"Configured settings for index: {indexName}");
        }

        public async Task IndexDocuments<T>(EEntity type, List<T> documents) where T : class
        {
            var dataBaseName = DatabaseNameHelper.GetDataBaseName(_configuration);
            var indexName = $"{type.ToString().ToLower()}-{dataBaseName}";
            if (documents == null || !documents.Any())
            {
                _logger.LogWarning($"No documents to index for {indexName}");
                return;
            }

            try
            {
                // Ensure index exists
                await InitializeIndex(indexName);
                var index = _client.Index(indexName);
                var task = await index.AddDocumentsAsync(documents);
                var taskInfo = await _client.WaitForTaskAsync(task.TaskUid);
                if (taskInfo.Status == TaskInfoStatus.Succeeded)
                {
                    _logger.LogInformation($"Successfully indexed {documents.Count} documents to {indexName}");
                }
                else if (taskInfo.Status == TaskInfoStatus.Failed)
                {
                    _logger.LogError($"Failed to index documents to {indexName}: {taskInfo.Error}");
                    throw new Exception($"Indexing failed: {taskInfo.Error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error indexing documents to {indexName}: {ex}");
                throw;
            }
        }

        // Search documents in an index
        public async Task<SearchResult<T>> Search<T>(string indexName, string query, SearchQuery searchQuery = null)
        {
            try
            {
                var index = _client.Index(indexName);
                if (searchQuery == null)
                {
                    searchQuery = new SearchQuery
                    {
                        Limit = 20,
                        AttributesToRetrieve = new[] { "*" }
                    };
                }
                var results = await index.SearchAsync<T>(query, searchQuery);
                return (SearchResult<T>)results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching in index {indexName}: {ex}");
                throw;
            }
        }

        // Delete a single document
        public async Task DeleteDocument(string indexName, string documentId)
        {
            try
            {
                var index = _client.Index(indexName);
                var task = await index.DeleteOneDocumentAsync(documentId);

                await _client.WaitForTaskAsync(task.TaskUid);
                _logger.LogInformation($"Deleted document {documentId} from {indexName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting document from {indexName}: {ex}");
                throw;
            }
        }

        // Delete an entire index
        public async Task DeleteIndex(string indexName)
        {
            try
            {
                var task = await _client.DeleteIndexAsync(indexName);
                await _client.WaitForTaskAsync(task.TaskUid);

                _logger.LogInformation($"Deleted index: {indexName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting index {indexName}: {ex}");
                throw;
            }
        }
        //private string GetDataBaseName()
        //{
        //    var connectionString = _configuration.GetConnectionString("DefaultConnection");
        //    var parameters = connectionString.Split(";");
        //    foreach (var parameter in parameters)
        //    {
        //        if (parameter.Trim().StartsWith("database=", StringComparison.OrdinalIgnoreCase))
        //        {
        //            return parameter.Split('=')[1];
        //        }
        //    }

        //    return string.Empty;
        //}
    }
}
