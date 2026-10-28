
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using EskaCMS.Infrastructure.Enums;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Modules.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EskaCMS.Search.Services.Repository;

public class ElasticSeacrhRepository<T> : ISearchRepository<T>
{
    private static readonly Fields NavigatableSearchFields = new[]
    {
        "details.name^5",
        "details.normalizedName^5",
        "details.slug^4",
        "details.keywords^3",
        "details.shortDescription^2",
        "details.metaDescription^2",
        "details.addtionalMetaTags^2",
        "details.description",
        "details.specification"
    };

    private readonly ElasticsearchClient _elasticsearchClient;
    private readonly IConfiguration _configuration;
    private readonly string _dataBaseName;
    private readonly string _indexName;
    public ElasticSeacrhRepository(ElasticsearchClient elasticsearchClient, IConfiguration configuration)
    {
        _elasticsearchClient = elasticsearchClient;
        _configuration = configuration;
        _dataBaseName = DatabaseNameHelper.GetDataBaseName(_configuration);
        _indexName = $"{typeof(T).Name.ToLower()}-{_dataBaseName}";


    }
    public async Task<Infrastructure.Modules.Repositories.SearchResponse<T>> FullTextSearch(string search, SearchRequestConfig searchRequestConfig)
    {
        try
        {
            var queryList = new List<Query>();
            var queryListEType = new List<Query>();
            foreach (var option in searchRequestConfig.Options)
            {
                if (option.Key == "entityType.keyword")
                {
                    queryListEType.Add(new TermQuery(option.Key)
                    {
                        Field = option.Key,
                        Value = (dynamic)option.Value
                    });
                }
                else
                {
                    queryList.Add(new TermQuery(option.Key)
                    {
                        Field = option.Key,
                        Value = (dynamic)option.Value
                    });
                }

            }
            var searchResponse = await _elasticsearchClient.SearchAsync<T>(s => s
                .Index(_indexName)
                .Query(q => q.Bool(b =>
                {
                    b.Must(m => m.MultiMatch(mm => mm
                        .Query(search)
                        .Fields(NavigatableSearchFields)
                        .Type(TextQueryType.PhrasePrefix)
                        .MaxExpansions(25)));

                    if (queryList.Count > 0)
                    {
                        b.Filter(queryList.ToArray());
                    }

                    if (queryListEType.Count > 0)
                    {
                        b.Should(queryListEType.ToArray())
                         .MinimumShouldMatch(1);
                    }
                }))
                .From((searchRequestConfig.PageNumber - 1) * searchRequestConfig.PageSize)
                .Size(searchRequestConfig.PageSize));

            var response = new Infrastructure.Modules.Repositories.SearchResponse<T>();
            if (!searchResponse.IsValidResponse)
            {
                response.Count = 0;
                response.Result = Enumerable.Empty<T>();

                return response;
            }
            response.Count = searchResponse.Total;
            response.Result = searchResponse.Documents.ToList();

            return response;
        }
        catch (Exception)
        {

            throw;
        }

    }

    public async Task<Infrastructure.Modules.Repositories.SearchResponse<T>> ProductSearch(string search, ProductSearchRequestConfig searchRequestConfig)
    {
        try
        {

            var queryList = new List<Query>();
            var matcAllQuey = new MatchAllQuery();
            var searchQuery = new MultiMatchQuery()
            {
                Query = search,
                Fields = "*",
                Type = TextQueryType.PhrasePrefix,
                MaxExpansions = 50

            };
            var searchRequest = new SearchRequest();
            if (string.IsNullOrEmpty(search))
            {
                searchRequest.Query = matcAllQuey;
            }
            else
            {
                searchRequest.Query = searchQuery;
            }
            foreach (var option in searchRequestConfig.Options)
            {

                queryList.Add(new TermQuery(option.Key)
                {
                    Field = option.Key,
                    Value = (dynamic)option.Value,
                });

            }

            var searchResponse = await _elasticsearchClient.SearchAsync<T>(s => s
                    .Index(_indexName)

                    .Query(
                       searchRequest.Query)
                    .PostFilter(f =>
                    f.Bool(b =>
                    b.Must(queryList.ToArray())
                    .Should(queryList.ToArray())
                    .MinimumShouldMatch(1)
                    )
                    )
                    .From((searchRequestConfig.PageNumber - 1) * searchRequestConfig.PageSize)
                    .Size(searchRequestConfig.PageSize)
                    );



            var response = new Infrastructure.Modules.Repositories.SearchResponse<T>();
            if (!searchResponse.IsValidResponse)
            {
                response.Count = 0;
                response.Result = Enumerable.Empty<T>();

                return response;
            }
            response.Count = searchResponse.Total;
            response.Result = searchResponse.Documents.ToList();

            return response;
        }
        catch (Exception ex)
        {

            throw;
        }
    }
    public async Task DeleteDocuemnt(string id, string type = null)
    {
        try
        {
            await _elasticsearchClient.DeleteAsync(index: _indexName, id);
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    public async Task DeleteIndex(string type = null)
    {
        try
        {
            var rsponse = await _elasticsearchClient.Indices.DeleteAsync(_indexName);

        }
        catch (Exception ex)
        {

            throw;
        }
    }

 
}


