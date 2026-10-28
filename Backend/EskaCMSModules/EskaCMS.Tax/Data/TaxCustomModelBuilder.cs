using EskaCMS.Infrastructure.Data;
//using EskaCMS.Entities.Entity;
//using EskaCMS.Entities.Entity;
//using EskaCMS.Entities.Entity;
//using EskaCMS.Entities.Entity.Core;
//using EskaCMS.Entities.Entity.Tax;
//using Microsoft.EntityFrameworkCore;

//namespace EskaCommerce.Module.Tax.Data
//{
//    public class TaxCustomModelBuilder : EskaCMS.Infrastructure.Data.ICustomModelBuilder
//    {
//        public void Build(ModelBuilder modelBuilder)
//        {
//           modelBuilder.Entity<TaxClass>().HasSeedData(new TaxClass(1) { Name = "Standard VAT", SiteId = 1L });

//            modelBuilder.Entity<SiteSettings>().HasSeedData(
//                new SiteSettings("Tax.DefaultTaxClassId") { Module = "Tax", IsVisibleInCommonSettingPage = true, Value = "1", SiteId = 1L }
//            );
//        }
//    }
//}


