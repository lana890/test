using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductSerial : EntityBase
    {
        public ProductSerial()
        {

        }
        public ProductSerial(string Code)
        {
            this.Code = Code;
        }
        public string Code { get; set; }
        public long ProductId { get; set; }
        public virtual Product Product { get; set; }
        public EProductSerialStatus Status { get; set; }
        public string BatchNumber { get; set; }
        public string ControlNo { get; set; }
        public string Balance { get; set; }
        public string Expiry { get; set; }

        // relations 

        public long? OrderId { get; set; } = null;
        public Order Order { get; set; }

        // loging
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public long? ModifiedById { get; set; }
        public User ModifiedBy { get; set; }

    }

}
