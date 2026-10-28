using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.EskaBilling.Models.response
{
    public class CustomerSimsResponseViewModel 
    {
        public long Id { get; set; }
        public string CustomerName { get; set; }
        public long ProductId { get; set; }
        public long AccountId { get; set; }
        public long OrderId { get; set; }

        public string ProductName { get; set; }
        public EIOTSIMStatus Status { get; set; }
        public long? ICCID { get; set; }
        public long? MSISDN { get; set; }
        public long? IMSI { get; set; }
        public decimal ProductPrice { get; set; }
       

        public string SIMName { get; set; }
        public string SIMTag { get; set; }
        public string AssignIP { get; set; }
        public string APNType { get; set; }
        public string City { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public string FirstActivationDate { get; set; }
    }
}
