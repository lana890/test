using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection.Emit;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Data
{
    public class PagesCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<FavouritePages>()
            //   .HasIndex(p => new { p.PageId, p.UserId })
            //   .IsUnique();
            //modelBuilder.Entity<BackgroundServiceSettings>().HasSeedData(

            //new BackgroundServiceSettings
            //{
            //    Id = "PagePreviewCodeBackgroundService",
            //    Name = "PagePreviewCodeBackgroundService",
            //    CreationDate = DateTimeOffset.Now,
            //    ModificationDate = DateTimeOffset.Now,
            //    CreatedById = 1,
            //    TimeSpan = TimeSpan.FromMinutes(30),
            //    IntervalType = IntervalType.Period,
            //    IsActive = true,
            //    PeriodType = BGPeriodType.ByTime,
            //    SiteId = 1
            //}

            //);

            // modelBuilder.Entity<Page>().ToTable("Pages_Page");
            // modelBuilder.Entity<Page>().HasKey(p => new { p.Id });
            // modelBuilder.Entity<Page>().HasIndex(p => new { p.CultureId, p.SiteId,p.SlugInfoId}).IsUnique();//.HasName("LocalizaionIndex");
            // modelBuilder.Entity<Page>().HasIndex(p =>  p.CultureId);

            //// modelBuilder.Entity<Page>().Property(s => s.Slug).HasMaxLength(1000);



            // modelBuilder.Entity<Page>()
            //     .HasMany(e => e.PageVersions)
            //     .WithOne(e => e.Page)
            //     .HasForeignKey(e => e.PageId);
            // //.OnDelete(DeleteBehavior.Cascade);



            // modelBuilder.Entity<Page>()
            //     .HasOne(e => e.CurrentVersion)
            //     .WithOne()
            //     .HasForeignKey<Page>(e => e.CurrentVersionId);
            // //.OnDelete(DeleteBehavior.Restrict);

            // modelBuilder.Entity<PageTemplates>()
            //     .HasMany(e => e.PageTemplateVersions)
            //     .WithOne(e => e.Template)
            //     .HasForeignKey(e => e.TemplateId);
            // //.OnDelete(DeleteBehavior.Cascade);

            // modelBuilder.Entity<PageTemplates>()
            //     .HasOne(e => e.CurrentVersion)
            //     .WithOne()
            //     .HasForeignKey<PageTemplates>(e => e.CurrentVersionId);
            //.OnDelete(DeleteBehavior.Restrict);
        }


    }
}

