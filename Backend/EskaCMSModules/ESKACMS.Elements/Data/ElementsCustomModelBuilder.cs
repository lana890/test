using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Elements;


namespace EskaCMS.Elements.Data
{
    public class ElementsCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {

         //   modelBuilder.Entity<ElementTypes>().HasSeedData(
         //    new ElementTypes("row")
         //    {
         //        CreationDate = System.DateTimeOffset.Now,
         //        Desciption = "this is row element",
         //        CreatedById = 1L,
         //        SiteId = 1L
         //    }
         //);

         //   modelBuilder.Entity<ElementTypes>().HasSeedData(
         //      new ElementTypes("col")
         //      {
         //          CreationDate = System.DateTimeOffset.Now,
         //          Desciption = "this is column element",
         //          CreatedById = 1L,
         //          SiteId = 1L
         //      }
         //  );


         //   modelBuilder.Entity<ElementTypes>().HasSeedData(
         //      new ElementTypes("container")
         //      {
         //          CreationDate = System.DateTimeOffset.Now,
         //          Desciption = "this is container element",
         //          CreatedById = 1L,
         //          SiteId = 1L
         //      }
         //  );

         //   modelBuilder.Entity<ElementTypes>().HasSeedData(
         //      new ElementTypes("Component")
         //      {
         //          CreationDate = System.DateTimeOffset.Now,
         //          Desciption = "this is Component element",
         //          CreatedById = 1L,
         //          SiteId = 1L
         //      }
         //  );

         //   modelBuilder.Entity<ShareableElement>().ToTable("Elements_ShareableElements");
         //   modelBuilder.Entity<ShareableElement>().HasKey(p => new { p.Id });
         //   modelBuilder.Entity<ShareableElement>().HasIndex(p => new { p.CultureId, p.SiteId, p.IdentifierCode }).IsUnique();


        }
    }
}

