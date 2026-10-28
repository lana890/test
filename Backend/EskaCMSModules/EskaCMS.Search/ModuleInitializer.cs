using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using EskaCMS.Core.Events;
using EskaCMS.Infrastructure.Modules;
using EskaCMS.Infrastructure.Modules.Repositories;
using EskaCMS.Search.Events;
using EskaCMS.Search.Services;
using EskaCMS.Search.Services.Interfaces;
using EskaCMS.Search.Services.Repository;
using MediatR;
using Meilisearch;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace SimplCommerce.Module.Search
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(sp =>
            {
                var configration = sp.GetRequiredService<IConfiguration>();
                var elsasticSearchUri = configration.GetSection("ElasticSearch:Url").Value;
                var elsasticSearchUsername = configration.GetSection("ElasticSearch:Username").Value;
                var elsasticSearchPassword = configration.GetSection("ElasticSearch:Password").Value;

                var elasticsearchClientSettings = new ElasticsearchClientSettings(new Uri(elsasticSearchUri))
                            .Authentication(new BasicAuthentication(elsasticSearchUsername, elsasticSearchPassword));

                var client = new ElasticsearchClient(elasticsearchClientSettings);

                return client;

            });
            //serviceCollection.AddSingleton<IOCRService>(sp =>
            //{
            //    var config = sp.GetRequiredService<IConfiguration>();
            //    var logger = sp.GetRequiredService<ILogger<OCRService>>();
            //    return new OCRService(config, logger);
            //});
            serviceCollection.AddTransient(typeof(ISearchRepository<>), typeof(ElasticSeacrhRepository<>));

            serviceCollection.AddTransient<ISearchService, SearchService>();
             RegisterMeiliSearchAsync(serviceCollection);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
        private void RegisterMeiliSearchAsync(IServiceCollection serviceCollection)
        {
            var ServiceProvider = serviceCollection.BuildServiceProvider();
            var configration = ServiceProvider.GetRequiredService<IConfiguration>();
            var MeiliSection = configration.GetSection("MeiliSearch");
            var enabled = configration.GetValue<bool>("MeiliSearch:Enabled");
            if (MeiliSection.Exists() && enabled)
            {
                var url = configration["MeiliSearch:Url"];
                var apiKey = configration["MeiliSearch:ApiKey"];
                var meiliClient = new MeilisearchClient(url, apiKey);
                try
                {
                    var health =  meiliClient.HealthAsync().GetAwaiter().GetResult();
                    if (health!=null&&health.Status == "available")
                    {
                        serviceCollection.AddTransient<IMeiliSearchService, MeiliSearchServices>();
                        serviceCollection.AddTransient(typeof(ISearchRepository<>), typeof(MeilisearchRepository<>));
                        serviceCollection.AddSingleton<MeilisearchClient>(sp => meiliClient);

                    }
                    else configration["MeiliSearch:Enabled"] = "false";
                }
                catch (Exception ex)
                {
                      configration["MeiliSearch:Enabled"] = "false";
                }
            }
        }
    }
}
