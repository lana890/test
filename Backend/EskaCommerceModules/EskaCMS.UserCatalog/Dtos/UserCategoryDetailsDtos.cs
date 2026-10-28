using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class UserCategoryDetailsCreate 
    {
        public long UsersCategoryId { get; set; }
        public long ProductId { get; set; }
        public long ProductPriceHistoryId { get; set; }
        public int Quantity { get; set; }
        public long? AttributeId { get; set; }
        public int Status { get; set; }
    }
    public class UserCategoryDetailsUpdate 
    {

        public long Id { get; set; }
        public long? UsersCategoryId { get; set; }
        public long? ProductId { get; set; }
        public long? ProductPriceHistoryId { get; set; }
        public int? Quantity { get; set; }
        public int? Status { get; set; }

    }
    public class UserCategoryDetailsListInput 
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    } 
    public class UserCategoryDetailsOutput 
    {
        public long UsersCategoryId { get; set; }
        public Product Product { get; set; }
        public long ProductId { get; set; }
        public ProductPriceHistory ProductPriceHistory { get; set; }
        public long  ProductPriceHistoryId { get; set; }
        public int Quantity { get; set; }
        public int Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }
    }
}
