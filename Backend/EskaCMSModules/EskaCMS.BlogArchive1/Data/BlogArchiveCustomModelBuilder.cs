using EskaCMS.Infrastructure.Data;
using EskaCMS.BlogArchive.Models;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.BlogArchive;

namespace EskaCommerce.Module.ShippingFree.Data
{
    public class BlogArchiveCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {

            //modelBuilder.Entity<Posts>().ToTable("BlogArchive_Posts");
            //modelBuilder.Entity<Posts>().HasKey(p => new { p.Id });
            //modelBuilder.Entity<Posts>().HasIndex(p => new { p.CultureId, p.SiteId, p.SlugInfoId }).IsUnique();
            //modelBuilder.Entity<Posts>().HasIndex(p => p.CultureId);

        }
    }
}
