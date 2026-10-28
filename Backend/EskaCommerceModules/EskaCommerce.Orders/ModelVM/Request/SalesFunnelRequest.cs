using Elastic.Clients.Elasticsearch.MachineLearning;
using EskaCommerce.Orders.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.ModelVM.Request
{
    public class SalesFunnelRequest
    {
        public TimeRequest timeRequest {  get; set; }
        public filterType filter {  get; set; }
    }
}
