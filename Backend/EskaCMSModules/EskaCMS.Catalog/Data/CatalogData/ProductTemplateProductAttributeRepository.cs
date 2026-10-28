using EskaCMS.Core.Data;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.DataAccess.Data;

namespace EskaCommerce.Catalog.Data.CatalogData
{
    // TODO This is just a temporary workaround because EF Core 1.0 hasn't support many-many without an entity class to represent the join table
    public class ProductTemplateProductAttributeRepository : IProductTemplateProductAttributeRepository
    {
        private readonly DbContext dbContext;

        public ProductTemplateProductAttributeRepository(EskaDCMSDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        //public ProductTemplateProductAttributeRepository(EskaCMSMySqlDbContext mySqlContext)
        //{
        //    this.dbContext = mySqlContext;
        //}
        public void Remove(ProductTemplateAttribute item)
        {
            dbContext.Set<ProductTemplateAttribute>().Remove(item);
        }
    }
}
