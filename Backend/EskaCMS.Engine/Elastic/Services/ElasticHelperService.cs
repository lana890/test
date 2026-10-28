using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using EskaCMS.Engine.Elastic.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Helpers;
namespace EskaCMS.Engine.Elastic.Services;

internal class ElasticHelperService : IElasticHelperService
{
    private readonly IConfiguration _configuration;
    private readonly ElasticsearchClient _elasticsearchClient;
    public ElasticHelperService(ElasticsearchClient elasticsearchClient, IConfiguration configuration)
    {
        _elasticsearchClient = elasticsearchClient;
        _configuration = configuration;
    }
    public async Task AddUpdateBulk<T>(IEnumerable<T> documents)
    {
        try
        {
            await EnsureIndexExists<T>();
            var dataBaseName = DatabaseNameHelper.GetDataBaseName(_configuration);
            var indexName = $"{typeof(T).Name.ToLower()}-{dataBaseName}";
            var elasticResponce = await _elasticsearchClient.BulkAsync(b =>
                                 b.IndexMany(documents, (idx, doc) => idx
                                  .Index(indexName)
                                 .Id(typeof(T).GetProperty("ElasticId")?.GetValue(doc).ToString())
                                 )
                    );
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    public async Task EnsureIndexExists<T>()
    {
        try
        {
            var dataBaseName = DatabaseNameHelper.GetDataBaseName(_configuration);
            var indexName = $"{typeof(T).Name.ToLower()}-{dataBaseName}";
            var indexExistsRsponse = await _elasticsearchClient.Indices.ExistsAsync(indexName);
            if (!indexExistsRsponse.Exists)
            {
                var prop = GetGenericMappings(typeof(T));
                await _elasticsearchClient.Indices.CreateAsync(indexName, c =>
                c.Mappings(m =>
                m.Properties(
                    prop
                    )


                ));
            }
        }
        catch (Exception ex)
        {

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

    private PropertiesDescriptor<object> GetGenericMappings(Type type)
    {
        try
        {
            var propertiesDescriptor = new PropertiesDescriptor<object>();
            var properties = type.GetProperties();
            foreach (var propertiy in properties)
            {
                if (propertiy.PropertyType.IsGenericType && propertiy.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    System.Type itemType = propertiy.PropertyType.GetGenericArguments()[0];
                    var nestedMappings = GetGenericMappings(itemType);

                    propertiesDescriptor.Nested(propertiy.Name, n => n
                    .Properties(nestedMappings));

                }
                else if (isComplex(propertiy.PropertyType))
                {
                    var nestedMappings = GetGenericMappings(propertiy.PropertyType);
                    propertiesDescriptor.Object(propertiy.Name, n => n
                  .Properties(nestedMappings));
                }
            }
            return propertiesDescriptor;
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    private bool isComplex(System.Type type)
    {
        return type.IsClass && type != typeof(string) && !typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }
}
