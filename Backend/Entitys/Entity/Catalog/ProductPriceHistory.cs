using System;
using System.ComponentModel.DataAnnotations.Schema;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;

using EskaCMS.Entities.Entity.Core;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductPriceHistory : EntityBase
    {
        public ProductPriceHistory()
        {
            CreatedOn = DateTimeOffset.Now;
        }


        public ProductPriceHistory(Product originalProduct,long userId, EHistoryAction action )
        {
            this.CreatedById = userId;
            this.ForeignPrice = originalProduct.ForeignPrice;
            this.IsAllowForeign = originalProduct.IsAllowForeign;
            this.SpecialPrice = originalProduct.SpecialPrice;
            this.SpecialForignPrice = originalProduct.SpecialForignPrice;
            this.SpecialPriceStart = originalProduct.SpecialPriceStart;
            this.SpecialPriceEnd = originalProduct.SpecialPriceEnd;
            this.Price = originalProduct.Price;
            this.ProductId = originalProduct.Id;
            this.Action = action;
            this.CreatedOn = DateTimeOffset.Now;

        }



        public long CreatedById { get; set; }

        public User CreatedBy { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public decimal? Price { get; set; }

        public decimal? ForeignPrice { get; set; }

        public bool IsAllowForeign { get; set; }

        public decimal? SpecialForignPrice { get; set; }

        public decimal? SpecialPrice { get; set; }

        public DateTimeOffset? SpecialPriceStart { get; set; }
        public DateTimeOffset? SpecialPriceEnd { get; set; }
        public EHistoryAction Action { get; set; }
        public long ProductId { get; set; }
        public Product Product { get; set; }

        [NotMapped]
        public bool IsPriceChange { get; set; }
    }
}
