using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Catalog;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCMS.Entities.Entity.ShoppingCart
{
    public class CartItem : EntityBase
    {
        public DateTimeOffset CreatedOn { get; set; }

        public long ProductId { get; set; }

        public Product Product { get; set; }

        public int Quantity { get; set; }
        public decimal? Price { get; set; }

        public long CartId { get; set; }

        public string AdditionalDetails { get; set; }
        public Cart Cart { get; set; }

    }

}
