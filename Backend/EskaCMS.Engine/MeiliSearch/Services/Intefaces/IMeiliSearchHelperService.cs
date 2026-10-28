using Meilisearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Engine.MeiliSearch.Services.Intefaces
{
    public interface IMeiliSearchHelperService
    {
        Task IndexDocuments<T>(EEntity type, List<T> documents) where T : class;
        Task<SearchResult<T>> Search<T>(string indexName, string query, SearchQuery searchQuery = null);
        Task DeleteDocument(string indexName, string documentId);
        Task DeleteIndex(string indexName);
        Task InitializeIndex(string indexName, string primaryKey = "MeiliId");
    }
}
