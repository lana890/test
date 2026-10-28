using EskaCMS.Infrastructure.Modules.Repositories;
using EskaCMS.Search.Enum;
using Meilisearch;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Search.Model;
using EskaCMS.Search.Services.Interfaces;
using EskaCMS.Entities.Entity.Core;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Entities.Helpers;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.Catalog;

public class MeilisearchRepository<T> : ISearchRepository<T>
{
    private readonly MeilisearchClient _meilisearchClient;
    private readonly IConfiguration _configuration;
    private readonly string _dataBaseName;
    private  string _indexName;

    public MeilisearchRepository(MeilisearchClient meilisearchClient, IConfiguration configuration)
    {
        _meilisearchClient = meilisearchClient;
        _configuration = configuration;
        _dataBaseName = DatabaseNameHelper.GetDataBaseName(_configuration);
        
    }



    ///Full text search with filters and entity type support
    public async Task<SearchResponse<T>> FullTextSearch(
        string search,
        SearchRequestConfig searchRequestConfig)
    {
        try
        {
            _indexName = $"{searchRequestConfig.EntityType.ToString().ToLower()}-{_dataBaseName}".ToLower();
            var _index = searchRequestConfig.EntityType;
            var filters = new List<string>();
            var entityTypeFilters = new List<string>();
            foreach (var option in searchRequestConfig.Options)
            {
                if (option.Key == "entityType.keyword" || option.Key == "entityType")
                {
                    entityTypeFilters.Add($"entityType = '{option.Value}'");
                }
                else
                {
                    var filterValue = option.Value is string ? $"'{option.Value}'" : option.Value.ToString();

                    filters.Add($"{option.Key} = {filterValue}");
                }
            }

            var filterString = BuildFilterString(filters, entityTypeFilters);

            var offset = (searchRequestConfig.PageNumber - 1) * searchRequestConfig.PageSize;

            var searchQuery = new SearchQuery
            {
                Q = search,
                Limit = searchRequestConfig.PageSize,
                Offset = offset,
                Filter = filterString,
                AttributesToRetrieve = new[] { "*" },
                AttributesToHighlight = new[] { "*" },
                MatchingStrategy = EMatchingStratagy.all.ToString()
            };

            // Execute search
            var index = _meilisearchClient.Index(_indexName);
            var searchResponse = await index.SearchAsync<T>(search, searchQuery);

            var response = new SearchResponse<T>
            {
                Count = searchResponse.Hits.Count(),
                Result = searchResponse.Hits?.ToList() ?? new List<T>()
            };

            return response;
        }
        catch (Exception ex)
        {
            throw new Exception($"FullTextSearch failed: {ex.Message}", ex);
        }
    }

    /// Product specific search with advanced filtering
    public async Task<SearchResponse<T>> ProductSearch(
        string search,
        ProductSearchRequestConfig searchRequestConfig)
    {
        try
        {
            var filters = new List<string>();

            foreach (var option in searchRequestConfig.Options)
            {
                var filterValue = option.Value is string
                    ? $"'{option.Value}'"
                    : option.Value.ToString();

                filters.Add($"{option.Key} = {filterValue}");
            }

            // Calculate pagination
            var offset = (searchRequestConfig.PageNumber - 1) * searchRequestConfig.PageSize;

            var searchQuery = new SearchQuery
            {
                Limit = searchRequestConfig.PageSize,
                MatchingStrategy= EMatchingStratagy.last.ToString(),
                Offset = offset,
                AttributesToRetrieve = new[] { "*" },
            };

            if (filters.Any())
            {
                searchQuery.Filter = string.Join(" AND ", filters);
            }

            if (string.IsNullOrEmpty(search))
            {
                searchQuery.Q = ""; // Empty query returns all documents
            }
            else
            {
                searchQuery.Q = search;
                searchQuery.MatchingStrategy = EMatchingStratagy.all.ToString();
            }

            var index = _meilisearchClient.Index(_indexName);
            var searchResponse = await index.SearchAsync<T>(searchQuery.Q, searchQuery);

            var response = new SearchResponse<T>
            {
                Count = searchResponse.Hits.Count(),
                Result = searchResponse.Hits?.ToList() ?? new List<T>()
            };

            return response;
        }
        catch (Exception ex)
        {
            throw new Exception($"ProductSearch failed: {ex.Message}", ex);
        }
    }


    public async Task DeleteDocuemnt(string id,string type)
    {
        try
        {
            
            _indexName = $"{type.ToLower()}-{_dataBaseName}".ToLower();
            var index = _meilisearchClient.Index(_indexName);
       
            var task = await index.DeleteOneDocumentAsync(id);
            await _meilisearchClient.WaitForTaskAsync(task.TaskUid);
        }
        catch (Exception ex)
        {
            throw new Exception($"DeleteDocument failed for ID {id}: {ex.Message}", ex);
        }
    }

    
    public async Task DeleteIndex(string type)
    {
        try
        {
            _indexName = $"{type.ToLower()}-{_dataBaseName}".ToLower();
            var task = await _meilisearchClient.DeleteIndexAsync(_indexName);
            await _meilisearchClient.WaitForTaskAsync(task.TaskUid);
        }
        catch (Exception ex)
        {
            throw new Exception($"DeleteIndex failed for index {_indexName}: {ex.Message}", ex);
        }
    }
    public async Task<bool> AddDocument(EskaCMS.Infrastructure.Enums.EEntity type,dynamic doc)
    {
        try
        {
            _indexName = $"{type.ToString().ToLower()}-{_dataBaseName}".ToLower();
            var index = await _meilisearchClient.GetIndexAsync(_indexName);
            TaskInfo task = null;
            switch (type){
                case EskaCMS.Infrastructure.Enums.EEntity.Page:
                    var docPage =NavigatableSearchModelHelper.GetPageNaviagtableModel(doc);
                     task = await index.AddDocumentsAsync(docPage);
                    break;
                case EskaCMS.Infrastructure.Enums.EEntity.Post:
                    var docPosts = NavigatableSearchModelHelper.GetPostNaviagtableModel(doc);
                     task = await index.AddDocumentsAsync(docPosts);

                    break;
                    case EskaCMS.Infrastructure.Enums.EEntity.Category:
                   var docCategory = NavigatableSearchModelHelper.GetCategoryNaviagtableModel(doc);
                     task = await index.AddDocumentsAsync(docCategory);

                    break;
                case EskaCMS.Infrastructure.Enums.EEntity.Brand:
                    var docBrand =  NavigatableSearchModelHelper.GetBrandsNaviagtableModel(doc);
                     task = await index.AddDocumentsAsync(docBrand);

                    break;

                }
           
            await _meilisearchClient.WaitForTaskAsync(task.TaskUid);
            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    //public async Task<bool> UpdateDocument<T>(long id, string type, IEnumerable<T> doc)
    //{
    //    try
    //    {
    //        _indexName = $"{type.ToLower()}-{_dataBaseName}".ToLower();
    //        var MeiliId=$"{id}-{type}";
    //        var index = await _meilisearchClient.GetIndexAsync(_indexName);
    //        var task=index.UpdateAsync(MeiliId);
    //    }
    //    catch (Exception ex)
    //    {
    //        throw;
    //    }
    //}
    public async Task<EskaCMS.Infrastructure.Models.MeiliSearchVM.IndexStats> GetIndexStats(EEntity type)
    {
        try
        {
            _indexName = $"{type.ToString().ToLower()}-{_dataBaseName}".ToLower();
            var index = await _meilisearchClient.GetIndexAsync(_indexName);
            var stats = await index.GetStatsAsync();
            return new EskaCMS.Infrastructure.Models.MeiliSearchVM.IndexStats
            {
                IndexName = _indexName,
                NumberOfDocuments = stats.NumberOfDocuments,
                IsIndexing = stats.IsIndexing,
                PrimaryKey = index.PrimaryKey
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"GetIndexStats failed: {ex.Message}", ex);
        }
    }

    #region Helper Methods

    // Build filter string combining must (AND) and should (OR) filters
    private string BuildFilterString(List<string> mustFilters, List<string> shouldFilters)
    {
        var filterParts = new List<string>();
        if (mustFilters.Any())
        {
            filterParts.Add(string.Join(" AND ", mustFilters));
        }

        if (shouldFilters.Any())
        {
            if (shouldFilters.Count == 1)
            {
                filterParts.Add(shouldFilters[0]);
            }
            else
            {
                filterParts.Add($"({string.Join(" OR ", shouldFilters)})");
            }
        }

        // Combine with AND if both exist
        if (filterParts.Count > 1)
        {
            return string.Join(" AND ", filterParts);
        }
        else if (filterParts.Count == 1)
        {
            return filterParts[0];
        }

        return null;
    }










    #endregion
}



