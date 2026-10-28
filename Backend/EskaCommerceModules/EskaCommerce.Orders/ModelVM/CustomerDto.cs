using Elastic.Clients.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Orders.ModelVM
{
    public class CustomerDto
    {
        public string name {  get; set; }
        public string email {  get; set; }
        public string mobile {  get; set; }
        public long ordersCount {  get; set; }
        public decimal totalAmount {  get; set; }
        public DateTimeOffset regitstrationDate { get; set; }
        public EStatus status { get; set; }
    }


}
