using Elastic.Clients.Elasticsearch.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Engine.Elastic.Services.Interfaces;

public interface IElasticHelperService
{
    Task AddUpdateBulk<T>(IEnumerable<T> documents);
    Task EnsureIndexExists<T>();

}
