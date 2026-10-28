using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure;

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Localization;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Localization.Data
{
    public class LocalizationCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {

          //  modelBuilder.Entity<Culture>().HasSeedData(
          //   new Culture(GlobalConfiguration.DefaultCulture) { Name = "English (US)" }
          //);

            ////  modelBuilder.Entity<Culture>().ToTable("Culture");

            //  //modelBuilder.Entity<CurrencyRates>().ToTable("CurrencyRates");
            //  modelBuilder.Entity<ResourcesKey>().Property(x => x.Key).IsRequired();
            //  //modelBuilder.Entity<ResourcesValue>().HasIndex(p => p.CultureId);
            //  modelBuilder.Entity<ResourcesValue>().HasIndex(x => new { x.CultureId, x.KeyId,x.SlugInfoId}).IsUnique(true);
            //  modelBuilder.Entity<ResourcesKey>().HasIndex(p => p.SiteId);
            //  modelBuilder.Entity<ResourcesKey>().HasIndex(x => new { x.Component, x.Key,x.SiteId,x.IsShared}).IsUnique(true);
            //  modelBuilder.Entity<CurrencyRates>().HasKey(ba => new { ba.Id });
            //  modelBuilder.Entity<CurrencyRates>().HasIndex(ba => new { ba.CultureCode, ba.SiteId }).IsUnique();
            //  modelBuilder.Entity<ResourceLocalization>().ToTable("Resource");
        }
    }
}

