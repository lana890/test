using System;
using EskaCMS.Core.Entities;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity.Catalog;
using Microsoft.EntityFrameworkCore;
using System.Xml;

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using static EskaCMS.Entities.Enum.GeneralEnums;


namespace EskaCommerce.Catalog.Data.CatalogData
{
    public class CatalogCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
           // modelBuilder.Entity<ProductSerial>().ToTable("Catalog_ProductSerial");
           // modelBuilder.Entity<ProductSerial>().HasIndex(p => new { p.Code, p.ProductId }).IsUnique();



           // modelBuilder.Entity<ProductLink>()
           //     .HasOne(x => x.Product)
           //     .WithMany(p => p.ProductLinks)
           //     .HasForeignKey(x => x.ProductId)
           //     .OnDelete(DeleteBehavior.Restrict);

           // modelBuilder.Entity<ProductLink>()
           //     .HasOne(x => x.LinkedProduct)
           //     .WithMany(p => p.LinkedProductLinks)
           //     .HasForeignKey(x => x.LinkedProductId)
           //     .OnDelete(DeleteBehavior.Restrict);


           // modelBuilder.Entity<ProductTemplateAttribute>()
           //     .HasKey(t => new { t.ProductTemplateId, t.ProductAttributeId });
           ////modelBuilder.Entity<ProductTemplateAttribute>()
           ////     .HasOne(pt => pt.ProductTemplate)
           ////     .WithMany(p => p.ProductAttributes)
           ////     .HasForeignKey(pt => pt.ProductTemplateId)

           ////    .OnDelete(DeleteBehavior.Cascade);
           // modelBuilder.Entity<ProductTemplateAttribute>()
           //     .HasOne(pt => pt.ProductAttribute)
           //     .WithMany(t => t.ProductTemplates)
           //     .HasForeignKey(pt => pt.ProductAttributeId)
           //     .OnDelete(DeleteBehavior.Cascade);
           // modelBuilder.Entity<ProductDetails>().HasIndex(p => new { p.SlugInfoId, p.CultureId }).IsUnique();
           // modelBuilder.Entity<Product>().Property(e => e.Price).HasPrecision(18, 7); // Added By Israa For Aywa (GO) 

           // modelBuilder.Entity<ProductCategory>().HasIndex(p => new { p.CategoryId, p.ProductId }).IsUnique();
           // modelBuilder.Entity<ProductCategory>().HasIndex(p => p.CategoryId);
           // modelBuilder.Entity<BrandDetails>().HasIndex(b => new { b.CultureId, b.SlugInfoId }).IsUnique();
           // modelBuilder.Entity<Brand>().HasIndex(b => new { b.SiteId });


           // modelBuilder.Entity<SiteSettings>().HasSeedData(
           //     new SiteSettings("Catalog.ProductPageSize") { Module = "Catalog", IsVisibleInCommonSettingPage = true, Value = "10", SiteId = 1L },
           //     new SiteSettings("Catalog.IsProductPriceIncludeTax") { Module = "Catalog", IsVisibleInCommonSettingPage = true, Value = "true", SiteId = 1L },
           //     new SiteSettings("Catalog.IsPriceVisible") { Module = "Catalog", IsVisibleInCommonSettingPage = true, Value = "true", SiteId = 1L },
           //     new SiteSettings("Catalog.IsRatingVisible") { Module = "Catalog", IsVisibleInCommonSettingPage = true, Value = "true", SiteId = 1L }
           // );

           // modelBuilder.Entity<EntityType>().HasSeedData(
           //     new EntityType("Category") { AreaName = "Catalog", RoutingController = "Category", RoutingAction = "CategoryDetail", IsMenuable = true },
           //     new EntityType("Brand") { AreaName = "Catalog", RoutingController = "Brand", RoutingAction = "BrandDetail", IsMenuable = true },
           //     new EntityType("Product") { AreaName = "Catalog", RoutingController = "Product", RoutingAction = "ProductDetail", IsMenuable = false },
           //     new EntityType("Booking") { AreaName = "Booking", RoutingController = "UserBooking", RoutingAction = "UserBookingDetails", IsMenuable = false }
           // );


           // modelBuilder.Entity<SiteSettings>().HasSeedData(
           //     new SiteSettings("StockEmail") { Value = "00962788799632", IsVisibleInCommonSettingPage = true, Module = "StockNotification", SiteId = 1 },
           //     new SiteSettings("StockEmailTemplate") { Value = "StockTemplate", IsVisibleInCommonSettingPage = true, Module = "StockNotification", SiteId = 1 }

           // );
           // modelBuilder.Entity<BackgroundServiceSettings>().HasSeedData(

           //     new BackgroundServiceSettings
           //     {
           //         SiteId = 1,
           //         TimeSpan = new TimeSpan(0, 24, 0, 0, 0),
           //         DayOfWeek = DayOfWeek.Sunday,
           //         CreatedById = 1,
           //         CreationDate = DateTimeOffset.Now,
           //         ModificationDate = DateTimeOffset.Now,
           //         IntervalDate = DateTimeOffset.Now,
           //         IntervalType = IntervalType.Period,
           //         Name = "StockBackgroundService",
           //         Id = "StockBackgroundService",
           //         MonthOfYear = 12,
           //         WeekOfMonth = 4,
           //         IsActive = true,
           //         PeriodType = BGPeriodType.Daily
           //     }

           //     );
            ////modelBuilder.Entity<Widget>().HasSeedData(
            ////    new Widget("CategoryWidget") { Name = "Category Widget", ViewComponentName = "CategoryWidget", CreateUrl = "widget-category-create", EditUrl = "widget-category-edit", CreatedOn = new DateTimeOffset(new DateTime(2018, 5, 29, 4, 33, 39, 160, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)) },
            ////    new Widget("ProductWidget") { Name = "Product Widget", ViewComponentName = "ProductWidget", CreateUrl = "widget-product-create", EditUrl = "widget-product-edit", CreatedOn = new DateTimeOffset(new DateTime(2018, 5, 29, 4, 33, 39, 163, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)) },
            ////    new Widget("EskaeProductWidget") { Name = "Eskae Product Widget", ViewComponentName = "EskaeProductWidget", CreateUrl = "widget-Eskae-product-create", EditUrl = "widget-Eskae-product-edit", CreatedOn = new DateTimeOffset(new DateTime(2018, 5, 29, 4, 33, 39, 163, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)) }
            ////);
        }
    }
}

