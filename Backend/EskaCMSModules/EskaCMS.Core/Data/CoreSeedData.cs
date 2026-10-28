using System;
using System.Collections.Generic;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure.Data;

namespace EskaCMS.Core.Data
{
    public static class CoreSeedData
    {
        public static void SeedData(ModelBuilder builder)
        {
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var seedDateOffset = new DateTimeOffset(seedDate);
            builder.Entity<Option>()
                    .HasOne<Category>(s => s.Category)
                    .WithMany(g => g.Options)
                    .HasForeignKey(s => s.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EmailsSettings>().HasSeedData(
                                    new EmailsSettings { Id = 1L, Name = "Default SMTP Settings", IsDefault = true, RequiresAuthentication = true, SmtpSenderEmail = "mail@eskadenia.com", SmtpPassword = "R@DT!me#123", SmtpPort = 25, SmtpUsername = "RADTime@eskadenia.com", SmtpSenderEmailAdmin = "mail@eskadenia.com", Status = EStatus.Published, SiteId = 1, SmtpServer = "mail@eskadenia.com" }
);
            builder.Entity<SitesCultures>().HasSeedData(
               new SitesCultures() { Id = 1, SiteId = 1, CreatedById = 1, CreationDate = seedDateOffset, Status = EStatus.Published, CultureType = 0, CultureId = "en-US" },
                    new SitesCultures() { Id = 2, SiteId = 1, CreatedById = 1, CreationDate = seedDateOffset, Status = EStatus.Published, CultureType = 0, CultureId = "ar-JO" }
                 );
            builder.Entity<BackgroundServiceSettings>().HasSeedData(

                new BackgroundServiceSettings
                {
                    SiteId = 1,
                    TimeSpan = new TimeSpan(0, 2, 0, 0, 0),
                    DayOfWeek = DayOfWeek.Sunday,
                    CreatedById = 1,
                    CreationDate = seedDateOffset,
                    ModificationDate = seedDateOffset,
                    IntervalDate = seedDateOffset,
                    IntervalType = IntervalType.Period,
                    Name = "OrderCancellationBackgroundService",
                    Id = "OrderCancellationBackgroundService",
                    MonthOfYear = 12,
                    WeekOfMonth = 4,
                    IsActive = true,
                    PeriodType = BGPeriodType.Daily
                }

                );
            builder.Entity<SitesSecurityPolicy>().ToTable("Core_SitesSecurityPolicy");

            builder.Entity<SitesSecurityPolicy>().HasSeedData(
              new SitesSecurityPolicy { Id = 100, RegularExpression = null, StatusDate = seedDate, NoRepeatedPassword = 2, MinUpperCaseLetters = 1, MinSpecialCharacters = 0, MinLength = 6, MinLowerCaseLetters = 1, Status = EStatus.Published, SiteId = 1, MinDigits = 1, CreationDate = seedDateOffset, CreatedById = 1L }

               );


            builder.Entity<BackgroundServiceSettings>().HasSeedData(

             new BackgroundServiceSettings
             {
                 Id = "OtpBackgroundService",
                 Name = "OtpBackgroundService",
                 CreationDate = seedDateOffset,
                 ModificationDate = seedDateOffset,
                 CreatedById = 1,
                 TimeSpan = TimeSpan.FromMinutes(120),
                 IntervalType = IntervalType.Period,
                 IsActive = true,
                 PeriodType = BGPeriodType.ByTime,
                 SiteId = 1
             }

             );


            builder.Entity<BackgroundServiceSettings>().HasSeedData(

              new BackgroundServiceSettings
              {
                  Id = "PagePreviewCodeBackgroundService",
                  Name = "PagePreviewCodeBackgroundService",
                  CreationDate = seedDateOffset,
                  ModificationDate = seedDateOffset,
                  CreatedById = 1,
                  TimeSpan = TimeSpan.FromMinutes(120),
                  IntervalType = IntervalType.Period,
                  IsActive = true,
                  PeriodType = BGPeriodType.ByTime,
                  SiteId = 1
              }

              );

            builder.Entity<BackgroundServiceSettings>().HasSeedData(

             new BackgroundServiceSettings
             {
                 Id = "ContentPublishBackgroundService",
                 Name = "ContentPublishBackgroundService",
                 CreationDate = seedDateOffset,
                 ModificationDate = seedDateOffset,
                 CreatedById = 1,
                 TimeSpan = TimeSpan.FromMinutes(30),
                 IntervalType = IntervalType.Period,
                 IsActive = true,
                 PeriodType = BGPeriodType.ByTime,
                 SiteId = 1
             }

             );



            builder.Entity<Category>().HasSeedData(
       new Category((int)ECategory.CommonComponents) { DisplayOrder = 1, IncludeInMenu = true,  CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.Page },
        new Category((int)ECategory.Ecommerce) { DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.Page },
        new Category((int)ECategory.SiteOptions) { DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.PageOptions) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.ComponentOptions) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.LayoutComponents) {  DisplayOrder = 1, IncludeInMenu = true,  CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.Component },
        new Category((int)ECategory.SystemComponents) { DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.Component },
        new Category((int)ECategory.ContentComponents) { DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.Component },
        new Category((int)ECategory.WebArtComponents) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.Component },

        new Category((int)ECategory.SharedElements) { DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.Component },

        new Category((int)ECategory.TemplateOptions) { DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.MediaManagerOptions) { DisplayOrder = 1, IncludeInMenu = true,  CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.GraphicProfileOptions) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.MenusOptions) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.DCMSAdminMenu) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.ViewsOptions) { DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.ContentOptions) { DisplayOrder = 1, IncludeInMenu = true , CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.SystemOptions) {  DisplayOrder = 1, IncludeInMenu = true , CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.UsersAndPermissionsOptions) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },

        new Category((int)ECategory.PostsOptions) {  DisplayOrder = 1, IncludeInMenu = true,CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.StaticMenuOptions) {  DisplayOrder = 1, IncludeInMenu = true,CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.MainMenuOptions) { DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.Main) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.Menu },
        new Category((int)ECategory.EcommerceComponents) { DisplayOrder = 1, IncludeInMenu = true  , CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.Component },
        new Category((int)ECategory.DashboardOptions) {  DisplayOrder = 1, IncludeInMenu = true,  CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.Charts) {  DisplayOrder = 1, IncludeInMenu = true,  CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.Gallery) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },

        new Category((int)ECategory.InsuranceComponents) {  DisplayOrder = 1, IncludeInMenu = true,  CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.Component },
        new Category((int)ECategory.EcommerceOptions) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.CatalogOptions) { DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions, ParentId = (int)ECategory.EcommerceOptions },
        new Category((int)ECategory.SalesOptions) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions, ParentId = (int)ECategory.EcommerceOptions },
        new Category((int)ECategory.PromotionsOptions) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions, ParentId = (int)ECategory.EcommerceOptions },
        new Category((int)ECategory.InventoryOptions) {  DisplayOrder = 1, IncludeInMenu = true,  CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions, ParentId = (int)ECategory.EcommerceOptions },
        new Category((int)ECategory.VendorsOptions) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions, ParentId = (int)ECategory.EcommerceOptions },
        new Category((int)ECategory.DiscountsOptions) { DisplayOrder = 1, IncludeInMenu = true,  CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions, ParentId = (int)ECategory.EcommerceOptions },
        new Category((int)ECategory.TaxOptions) {  DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions, ParentId = (int)ECategory.EcommerceOptions },
        new Category((int)ECategory.PlansAndSubscriptionsOptions) { DisplayOrder = 1, IncludeInMenu = true,  CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions, ParentId = (int)ECategory.EcommerceOptions },
        new Category((int)ECategory.ReviewsAndCommentsOptions) {  DisplayOrder = 1, IncludeInMenu = true,  CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions, ParentId = (int)ECategory.EcommerceOptions },
        new Category((int)ECategory.EventsOptions) { DisplayOrder = 1, IncludeInMenu = true,CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.RoleOptions },
        new Category((int)ECategory.Conferences) { DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.Events },
        new Category((int)ECategory.AnnualMeetings) { DisplayOrder = 1, IncludeInMenu = true, CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.Events },
        new Category((int)ECategory.Birthdays) { DisplayOrder = 1, IncludeInMenu = true,CreationDate = seedDateOffset, CreatedById = 1L, Status = EStatus.Active, StatusDate = seedDateOffset, CategoryTypeId = (long)ECategoryTypes.Events }

);
            builder.Entity<CategoryDetails>().HasSeedData(
                
                new CategoryDetails {Id=1L, Name = ECategory.CommonComponents.ToString(), MetaDescription = " type category",MetaKeywords = " category", MetaTitle = "Page", SlugInfoId = null,CategoryId=1 },
                new CategoryDetails { Id = 2L, Name = ECategory.Ecommerce.ToString(), MetaDescription = "Ecommerce type category", MetaKeywords = "Ecommerce type category", MetaTitle = "Ecommerce category", SlugInfoId = null,CategoryId=2 },
                new CategoryDetails { Id = 3L, Name = ECategory.SiteOptions.ToString(), MetaDescription = "Site Options category", MetaKeywords = "SiteOptions category", MetaTitle = "SiteOptions", SlugInfoId = null,CategoryId=3 },
                new CategoryDetails {Id = 4L, Name = ECategory.PageOptions.ToString(), MetaDescription = "Page Options category", MetaKeywords = "PageOptions category", MetaTitle = "PageOptions", SlugInfoId = null,CategoryId=4 },
                new CategoryDetails {Id = 5L, Name = ECategory.ComponentOptions.ToString(), MetaDescription = "Component Options category", MetaKeywords = "ComponentOptions category", MetaTitle = "ComponentOptions", SlugInfoId = null,CategoryId=5 },
                new CategoryDetails {Id = 6L, Name = ECategory.LayoutComponents.ToString(), MetaDescription = "Layout Components category", MetaKeywords = "Layout Components category", MetaTitle = "Layout Components", SlugInfoId = null,CategoryId=6 },
                new CategoryDetails {Id = 7L, Name = ECategory.SystemComponents.ToString(), MetaDescription = "System Components category", MetaKeywords = "System Components category", MetaTitle = "System Components", SlugInfoId = null,CategoryId=7 },
                new CategoryDetails {Id = 8L, Name = ECategory.ContentComponents.ToString(), MetaDescription = "Content Components category", MetaKeywords = "Content Components category", MetaTitle = "Content Components", SlugInfoId = null,CategoryId=8},
                new CategoryDetails {Id = 9L, Name = ECategory.WebArtComponents.ToString(), MetaDescription = "Web Art Components category", MetaKeywords = "Web Art Components category", MetaTitle = "Web Art Components", SlugInfoId = null,CategoryId=9 },
                new CategoryDetails {Id = 43L, Name = ECategory.SharedElements.ToString(), MetaDescription = "Shared Elements category", MetaKeywords = "Shared Elements category", MetaTitle = "Shared Elements", SlugInfoId = null,CategoryId=71 },
                new CategoryDetails {Id = 10L, Name = ECategory.TemplateOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=10 },
                new CategoryDetails {Id = 11L, Name = ECategory.MediaManagerOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=11 },
                new CategoryDetails {Id = 12L, Name = ECategory.GraphicProfileOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=12 },
                new CategoryDetails {Id = 13L, Name = ECategory.MenusOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=13 },
                new CategoryDetails {Id = 14L, Name = ECategory.DCMSAdminMenu.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=14 },
                new CategoryDetails {   Id = 15L, Name = ECategory.ViewsOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=15 },
                new CategoryDetails {Id = 16L, Name = ECategory.ContentOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=16 },
                new CategoryDetails {Id = 17L, Name = ECategory.SystemOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=17 },
                new CategoryDetails {Id=18L,    Name = ECategory.UsersAndPermissionsOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=18 },
                new CategoryDetails {Id = 19L, Name = ECategory.PostsOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=19 },
                new CategoryDetails {Id = 20L, Name = ECategory.StaticMenuOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=20 },
                new CategoryDetails {Id = 21L, Name = ECategory.MainMenuOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=21 },
                new CategoryDetails {Id=22L,    Name = ECategory.Main.ToString(), MetaDescription = "", MetaKeywords = "", MetaTitle = "", SlugInfoId = null, CategoryId = 22 },

                new CategoryDetails { Id = 23L, Name = ECategory.EcommerceComponents.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=23 },
                new CategoryDetails { Id = 24L, Name = ECategory.DashboardOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=24 },
                new CategoryDetails { Id = 25L, Name = ECategory.Charts.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=25 },
                new CategoryDetails { Id = 26L, Name = ECategory.Gallery.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=26 },
                new CategoryDetails { Id = 27L, Name = ECategory.InsuranceComponents.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=27 },
                new CategoryDetails { Id = 28L, Name = ECategory.EcommerceOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=37 },
                new CategoryDetails { Id = 29L, Name = ECategory.CatalogOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=28 },
                new CategoryDetails { Id = 30L, Name = ECategory.SalesOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=29 },
                new CategoryDetails { Id = 31L, Name = ECategory.PromotionsOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=30 },
                new CategoryDetails { Id = 32L, Name = ECategory.InventoryOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=31 },
                new CategoryDetails { Id = 33L, Name = ECategory.VendorsOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=32 },
                new CategoryDetails { Id = 34L, Name = ECategory.DiscountsOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=33 },
                new CategoryDetails { Id = 35L, Name = ECategory.TaxOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=34 },
                new CategoryDetails { Id = 36L, Name = ECategory.PlansAndSubscriptionsOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=35 },
                new CategoryDetails { Id = 37L, Name = ECategory.ReviewsAndCommentsOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=36 },
                new CategoryDetails { Id = 38L, Name = ECategory.EventsOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=67 },
                new CategoryDetails { Id = 39L, Name = ECategory.Conferences.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=69 },
                new CategoryDetails { Id = 40L, Name = ECategory.AnnualMeetings.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=68 },
                new CategoryDetails { Id = 41L, Name = ECategory.ReviewsAndCommentsOptions.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=36 },
                new CategoryDetails { Id = 42L, Name = ECategory.Birthdays.ToString(), MetaDescription = "",MetaKeywords = "", MetaTitle = "", SlugInfoId = null,CategoryId=70 }
                
                
                
                );


builder.Entity<Option>().HasSeedData(
             new Option { Id = (int)EOptions.CATPV, Name = "Can access to Plans Versions", Code = EOptions.CATPV.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PlansAndSubscriptionsOptions },
            new Option { Id = (int)EOptions.CATUB, Name = "Can access to user booking", Code = EOptions.CATUB.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATTM, Name = "Tax menu", Code = EOptions.CATTM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATSM, Name = "Sales menu", Code = EOptions.CATSM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATTC, Name = "Can access to tax class", Code = EOptions.CATTC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.TaxOptions },
            new Option { Id = (int)EOptions.CATSI, Name = "Can access to stocks", Code = EOptions.CATSI.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.InventoryOptions },
            new Option { Id = (int)EOptions.CATPA, Name = "Can access to product attribute", Code = EOptions.CATPA.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.CatalogOptions },
            new Option { Id = (int)EOptions.CATP, Name = "Products", Code = EOptions.CATP.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.CatalogOptions },
            new Option { Id = (int)EOptions.SCSSC, Name = "Show CSS Code", Code = EOptions.SCSSC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.DashboardOptions },
            new Option { Id = (int)EOptions.CDS, Name = "Can delete site", Code = EOptions.CDS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SiteOptions },
            new Option { Id = (int)EOptions.CDRM, Name = "Can delete root menu", Code = EOptions.CDRM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MenusOptions },
            new Option { Id = (int)EOptions.CDMC, Name = "Can delete menu category", Code = EOptions.CDMC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MenusOptions },
            new Option { Id = (int)EOptions.CSSTAD, Name = "Can set site theme as default", Code = EOptions.CSSTAD.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CDCST, Name = "Can delete classes on site theme", Code = EOptions.CDCST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CAFOST, Name = "Can add font on site theme", Code = EOptions.CAFOST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CACOST, Name = "Can add colors on site theme", Code = EOptions.CACOST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CASOPS, Name = "Can add styles on primary style", Code = EOptions.CASOPS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CVFT, Name = "Can view files type", Code = EOptions.CVFT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MediaManagerOptions },
            new Option { Id = (int)EOptions.CUMF, Name = "Can upload multi files", Code = EOptions.CUMF.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MediaManagerOptions },
            new Option { Id = (int)EOptions.CET, Name = "Can edit template", Code = EOptions.CET.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.TemplateOptions },
            new Option { Id = (int)EOptions.CDSM, Name = "Can delete static menu", Code = EOptions.CDSM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.StaticMenuOptions },
            new Option { Id = (int)EOptions.CDBP, Name = "Can delete blog post", Code = EOptions.CDBP.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PostsOptions },
            new Option { Id = (int)EOptions.CVP, Name = "Can view pages", Code = EOptions.CVP.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PageOptions },
            new Option { Id = (int)EOptions.CPUP, Name = "Can publish/unpublish page", Code = EOptions.CPUP.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PageOptions },
            new Option { Id = (int)EOptions.RLE, Name = "Role", Code = EOptions.RLE.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
            new Option { Id = (int)EOptions.USRAPER, Name = "Users & Permissions", Code = EOptions.USRAPER.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SystemOptions },
            new Option { Id = (int)EOptions.BlgsArt, Name = "Blogs & Article", Code = EOptions.BlgsArt.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.ContentOptions },
            new Option { Id = (int)EOptions.CESTYLE, Name = "Edit Style", Code = EOptions.CESTYLE.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SiteOptions },
            new Option { Id = (int)EOptions.CATB, Name = "Can access to brands", Code = EOptions.CATB.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.CatalogOptions },
            new Option { Id = (int)EOptions.CATPS, Name = "Can access to providers schedule", Code = EOptions.CATPS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATDM, Name = "Discounts menu", Code = EOptions.CATDM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATCM, Name = "Catalog menu", Code = EOptions.CATCM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATDIS, Name = "Can access to discounts", Code = EOptions.CATDIS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.DiscountsOptions },
            new Option { Id = (int)EOptions.CATCPRU, Name = "Can access to cart price rule usages", Code = EOptions.CATCPRU.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PromotionsOptions },
            new Option { Id = (int)EOptions.CATPAG, Name = "Can access to product attribute group", Code = EOptions.CATPAG.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.CatalogOptions },
            new Option { Id = (int)EOptions.CAEC, Name = "Can Add Edit Component ", Code = EOptions.CAEC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.DashboardOptions },
            new Option { Id = (int)EOptions.SGVC, Name = "Show guest visitor chart", Code = EOptions.SGVC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.DashboardOptions },
            new Option { Id = (int)EOptions.CANS, Name = "Can add new site", Code = EOptions.CANS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SiteOptions },
            new Option { Id = (int)EOptions.CERM, Name = "Can edit root menu", Code = EOptions.CERM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MenusOptions },
            new Option { Id = (int)EOptions.CDUMC, Name = "Can duplicate menu category", Code = EOptions.CDUMC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MenusOptions },
            new Option { Id = (int)EOptions.CDST, Name = "Can dublicate site theme", Code = EOptions.CDST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CACST, Name = "Can add classes on site theme", Code = EOptions.CACST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CVFOST, Name = "Can view font on site theme", Code = EOptions.CVFOST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CVST, Name = "Can view site theme", Code = EOptions.CVST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CASOST, Name = "Can add styles on site themes", Code = EOptions.CASOST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CVDT, Name = "Can view Docs type", Code = EOptions.CVDT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MediaManagerOptions },
            new Option { Id = (int)EOptions.CUSF, Name = "Can upload single file", Code = EOptions.CUSF.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MediaManagerOptions },
            new Option { Id = (int)EOptions.CDUT, Name = "Can duplicate template", Code = EOptions.CDUT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.TemplateOptions },
            new Option { Id = (int)EOptions.CPUSM, Name = "Can Publish/Unpublish Static Menu", Code = EOptions.CPUSM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.StaticMenuOptions },
            new Option { Id = (int)EOptions.CEBPT, Name = "Can edit blog post template", Code = EOptions.CEBPT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PostsOptions },
            new Option { Id = (int)EOptions.CANP, Name = "Can add new page", Code = EOptions.CANP.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PageOptions },
            new Option { Id = (int)EOptions.CEP, Name = "Can Edit Page", Code = EOptions.CEP.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PageOptions },
            new Option { Id = (int)EOptions.USRS, Name = "Users", Code = EOptions.USRS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SystemOptions },
            new Option { Id = (int)EOptions.THEME, Name = "Graphic Profile ", Code = EOptions.THEME.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.ContentOptions },
            new Option { Id = (int)EOptions.PAGS, Name = "Pages", Code = EOptions.PAGS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.ViewsOptions },
            new Option { Id = (int)EOptions.CES, Name = "Edit site", Code = EOptions.CES.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SiteOptions },
            new Option { Id = (int)EOptions.CATRC, Name = "Can access to reviews & comments", Code = EOptions.CATRC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATUP, Name = "Can access to user plans", Code = EOptions.CATUP.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATVM, Name = "Vendors menu", Code = EOptions.CATVM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATPPV, Name = "Can access to products plan", Code = EOptions.CATPPV.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PlansAndSubscriptionsOptions },
            new Option { Id = (int)EOptions.CATVEN, Name = "Can access to vendors", Code = EOptions.CATVEN.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.VendorsOptions },
            new Option { Id = (int)EOptions.CATCPR, Name = "Can access to cart price rule", Code = EOptions.CATCPR.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PromotionsOptions },
            new Option { Id = (int)EOptions.CATPP, Name = "Product Prices", Code = EOptions.CATPP.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.CatalogOptions },
            new Option { Id = (int)EOptions.CASTU, Name = "Can assign site to user", Code = EOptions.CASTU.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
            new Option { Id = (int)EOptions.SUAC, Name = "Show user\'s activities chart", Code = EOptions.SUAC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.DashboardOptions },
            new Option { Id = (int)EOptions.CDCM, Name = "Can delete child menu", Code = EOptions.CDCM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MenusOptions },
            new Option { Id = (int)EOptions.CPURM, Name = "Can publish / unpublished root menu", Code = EOptions.CPURM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MenusOptions },
            new Option { Id = (int)EOptions.CEMC, Name = "Can edit menu category", Code = EOptions.CEMC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MenusOptions },
            new Option { Id = (int)EOptions.CRST, Name = "Can rename site theme", Code = EOptions.CRST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CVCST, Name = "Can view classes on site theme", Code = EOptions.CVCST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CVCOST, Name = "Can view colors on site theme", Code = EOptions.CVCOST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CANST, Name = "Can add new site theme", Code = EOptions.CANST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CDF, Name = "Can download file", Code = EOptions.CDF.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MediaManagerOptions },
            new Option { Id = (int)EOptions.CVVT, Name = "Can view videos type", Code = EOptions.CVVT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MediaManagerOptions },
            new Option { Id = (int)EOptions.CUF, Name = "Can upload files", Code = EOptions.CUF.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MediaManagerOptions },
            new Option { Id = (int)EOptions.COBT, Name = "Can open builder template", Code = EOptions.COBT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.TemplateOptions },
            new Option { Id = (int)EOptions.CESM, Name = "Can edit static menu", Code = EOptions.CESM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.StaticMenuOptions },
            new Option { Id = (int)EOptions.CEBP, Name = "Can edit blog post", Code = EOptions.CEBP.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PostsOptions },
            new Option { Id = (int)EOptions.CDP, Name = "Can delete page", Code = EOptions.CDP.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PageOptions },
            new Option { Id = (int)EOptions.COPB, Name = "Can Open Page Builder", Code = EOptions.COPB.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PageOptions },
            new Option { Id = (int)EOptions.DTSRC, Name = "Data Source", Code = EOptions.DTSRC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SystemOptions },
            new Option { Id = (int)EOptions.MENUS, Name = "Menus", Code = EOptions.MENUS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.ContentOptions },
            new Option { Id = (int)EOptions.TMPL, Name = "Template", Code = EOptions.TMPL.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.ViewsOptions },
            new Option { Id = (int)EOptions.SUFRMS, Name = "Submitted Forms", Code = EOptions.SUFRMS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.ContentComponents },
            new Option { Id = (int)EOptions.CATDrivers, Name = "Can access to Drivers", Code = EOptions.CATDrivers.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATPTS, Name = "Can access to provider time slots", Code = EOptions.CATPTS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATIM, Name = "Inventory menu", Code = EOptions.CATIM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATPL, Name = "Can access to plan list", Code = EOptions.CATPL.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PlansAndSubscriptionsOptions },
            new Option { Id = (int)EOptions.CATMWPI, Name = "Can access to manage warehouse products", Code = EOptions.CATMWPI.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.InventoryOptions },
            new Option { Id = (int)EOptions.CATS, Name = "Can access to shipments", Code = EOptions.CATS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SalesOptions },
            new Option { Id = (int)EOptions.CATO, Name = "Orders", Code = EOptions.CATO.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SalesOptions },
            new Option { Id = (int)EOptions.SJSONC, Name = "Show Json Code", Code = EOptions.SJSONC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.DashboardOptions },
            new Option { Id = (int)EOptions.CESR, Name = "Can edit security dialog", Code = EOptions.CESR.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MenusOptions },
            new Option { Id = (int)EOptions.CECM, Name = "Can edit child menu", Code = EOptions.CECM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MenusOptions },
            new Option { Id = (int)EOptions.CACM, Name = "Can add child menu", Code = EOptions.CACM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MenusOptions },
            new Option { Id = (int)EOptions.CANMC, Name = "Can add new menu category", Code = EOptions.CANMC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MenusOptions },
            new Option { Id = (int)EOptions.CASST, Name = "Can add style on site theme", Code = EOptions.CASST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CDFOST, Name = "Can delete font on site theme", Code = EOptions.CDFOST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CDCOST, Name = "Can delete colors on site theme", Code = EOptions.CDCOST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CASOCS, Name = "Can add styles on culture style", Code = EOptions.CASOCS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CVIE, Name = "Can view image editor", Code = EOptions.CVIE.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MediaManagerOptions },
            new Option { Id = (int)EOptions.CVIT, Name = "Can view images type", Code = EOptions.CVIT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MediaManagerOptions },
            new Option { Id = (int)EOptions.CANF, Name = "Can add new folder", Code = EOptions.CANF.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MediaManagerOptions },
            new Option { Id = (int)EOptions.CANT, Name = "Can add new template", Code = EOptions.CANT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.TemplateOptions },
            new Option { Id = (int)EOptions.CVSM, Name = "Can view static menu", Code = EOptions.CVSM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.StaticMenuOptions },
            new Option { Id = (int)EOptions.CANBP, Name = "Can add new blog post", Code = EOptions.CANBP.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PostsOptions },
            new Option { Id = (int)EOptions.CDUP, Name = "Can duplicate page", Code = EOptions.CDUP.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PageOptions },
            new Option { Id = (int)EOptions.CSPD, Name = "Set page default", Code = EOptions.CSPD.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PageOptions },
            new Option { Id = (int)EOptions.STS, Name = "Sites", Code = EOptions.STS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SystemOptions },
            new Option { Id = (int)EOptions.MEMGER, Name = "Media Manager", Code = EOptions.MEMGER.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.ContentOptions },
            new Option { Id = (int)EOptions.DASH, Name = "Dashboard", Code = EOptions.DASH.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.DCMSAdminMenu },
            new Option { Id = (int)EOptions.CATD, Name = "Can access to drivers", Code = EOptions.CATD.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PlansAndSubscriptionsOptions },
            new Option { Id = (int)EOptions.CATProv, Name = "Can access to providers", Code = EOptions.CATProv.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATPLM, Name = "Plans menu", Code = EOptions.CATPLM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATPM, Name = "Promotions menu", Code = EOptions.CATPM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CATTR, Name = "Can access to tax rate", Code = EOptions.CATTR.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.TaxOptions },
            new Option { Id = (int)EOptions.CATWI, Name = "Can access to warehouse", Code = EOptions.CATWI.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.InventoryOptions },
            new Option { Id = (int)EOptions.CATPT, Name = "Can access to product template", Code = EOptions.CATPT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.CatalogOptions },
            new Option { Id = (int)EOptions.CATC, Name = "Categories", Code = EOptions.CATC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.CatalogOptions },
            new Option { Id = (int)EOptions.SJSC, Name = "Show Javascript code", Code = EOptions.SJSC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.DashboardOptions },
            new Option { Id = (int)EOptions.CSSAD, Name = "Can set site as default", Code = EOptions.CSSAD.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SiteOptions },
            new Option { Id = (int)EOptions.CPUCM, Name = "Can publish / unpublished child menu", Code = EOptions.CPUCM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MenusOptions },
            new Option { Id = (int)EOptions.CANM, Name = "Can add new menu", Code = EOptions.CANM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MenusOptions },
            new Option { Id = (int)EOptions.CDSTE, Name = "Can delete site theme", Code = EOptions.CDSTE.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CECST, Name = "Can edit classes on site theme", Code = EOptions.CECST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CEFOST, Name = "Can edit font on site theme", Code = EOptions.CEFOST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CECOST, Name = "Can edit colors on site theme", Code = EOptions.CECOST.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CASOTS, Name = "Can add styles on template style", Code = EOptions.CASOTS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.GraphicProfileOptions },
            new Option { Id = (int)EOptions.CCFL, Name = "Can copy file location", Code = EOptions.CCFL.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MediaManagerOptions },
            new Option { Id = (int)EOptions.CVAF, Name = "Can view all files", Code = EOptions.CVAF.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.MediaManagerOptions },
            new Option { Id = (int)EOptions.CDT, Name = "Can delete template", Code = EOptions.CDT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.TemplateOptions },
            new Option { Id = (int)EOptions.CESMT, Name = "Can edit static menu template", Code = EOptions.CESMT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.StaticMenuOptions },
            new Option { Id = (int)EOptions.CANSM, Name = "Can add new static menu", Code = EOptions.CANSM.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.StaticMenuOptions },
            new Option { Id = (int)EOptions.CVBP, Name = "Can view blog post", Code = EOptions.CVBP.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PostsOptions },
            new Option { Id = (int)EOptions.CEPT, Name = "Can edit page template", Code = EOptions.CEPT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PageOptions },
            new Option { Id = (int)EOptions.CALR, Name = "Can access location reservation", Code = EOptions.CALR.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EventsOptions },
            new Option { Id = (int)EOptions.RLSMANGE, Name = "Roles Manager", Code = EOptions.RLSMANGE.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
            new Option { Id = (int)EOptions.RELSMANGR, Name = "Release Manager", Code = EOptions.RELSMANGR.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SystemOptions },
            new Option { Id = (int)EOptions.SRVY, Name = "Survey", Code = EOptions.SRVY.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.ContentOptions },
            new Option { Id = (int)EOptions.AO, Name = "Add Option", Code = EOptions.AO.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SiteOptions },
            new Option { Id = (int)EOptions.CALD, Name = "Can Access To Login Details", Code = EOptions.CALD.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
            new Option { Id = (int)EOptions.CAPS, Name = "Can Access Product Serials", Code = EOptions.CAPS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.CatalogOptions },
            new Option { Id = (int)EOptions.CVS, Name = "Can View Sites", Code = EOptions.CVS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SiteOptions },
            new Option { Id = (int)EOptions.CVB, Name = "Can View Blogs", Code = EOptions.CVB.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.PostsOptions },
            new Option { Id = (int)EOptions.CANCAT, Name = "Can Add Category", Code = EOptions.CANCAT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CANPROD, Name = "Can Add Product", Code = EOptions.CAPS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
            new Option { Id = (int)EOptions.CANINV, Name = "Inventory Menu", Code = EOptions.CANINV.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
             new Option { Id = (int)EOptions.CSER, Name = "Can See Errors", Code = EOptions.CSER.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
             new Option { Id = (int)EOptions.CSCAT, Name = "Can See Catalog", Code = EOptions.CSCAT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.EcommerceOptions },
             new Option { Id = (int)EOptions.CSET, Name = "Can See Email Template ", Code = EOptions.CSET.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.ContentOptions },
             new Option { Id = (int)EOptions.CSDO, Name = "Can See Domains", Code = EOptions.CSDO.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SystemOptions },
             new Option { Id = (int)EOptions.CSNL, Name = "Can See News Letter", Code = EOptions.CSNL.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SystemOptions },
             new Option { Id = (int)EOptions.CSCR, Name = "Can See Currency", Code = EOptions.CSCR.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SystemOptions },
             new Option { Id = (int)EOptions.CSES, Name = "Can See Email Settings", Code = EOptions.CSES.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SystemOptions },
             new Option { Id = (int)EOptions.CSCN, Name = "Can See Countries and states", Code = EOptions.CSCN.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SystemOptions },
             new Option { Id = (int)EOptions.CSCT, Name = "Can See Cultures", Code = EOptions.CSCT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.SystemOptions },
             new Option { Id = (int)EOptions.USRNT, Name = "User Notifications", Code = EOptions.USRNT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.USRSN, Name = "User Session", Code = EOptions.USRSN.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.USRWH, Name = "User Working Hours", Code = EOptions.USRWH.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.USRG, Name = "User Group", Code = EOptions.USRG.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.CRS, Name = "Can Restore Serials", Code = EOptions.CRS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.CRES, Name = "Can Reserve Serials ", Code = EOptions.CRES.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.CASCS, Name = "Can Access Shipping Costs ", Code = EOptions.CASCS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.CADDSCS, Name = "Can Add Shipping Costs", Code = EOptions.CADDSCS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.CEDITSCS, Name = "Can Edit Shipping Costs", Code = EOptions.CEDITSCS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.CDELETESC, Name = "Can Delete Shipping Costs", Code = EOptions.CDELETESC.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.CATLOY, Name = "Can Access To Loyalty", Code = EOptions.CATLOY.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.CATWF, Name = "Can Access To WorkFlow", Code = EOptions.CATWF.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.CDPDT, Name = "Can Delete Predefined Templates", Code = EOptions.CDPDT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.CEPDT, Name = "Can Edit Predefined Templates", Code = EOptions.CEPDT.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.CDNS, Name = "Can Download Settings", Code = EOptions.CDNS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.CUPTS, Name = "Can Update Predefined Template Settings", Code = EOptions.CUPTS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions },
             new Option { Id = (int)EOptions.DFRMS, Name = "Can Access Dynamic Forms", Code = EOptions.DFRMS.ToString(), CreationDate = seedDateOffset, ModificationDate = seedDateOffset, Type = EDefinitionType.SystemDefined, SiteId = 1, CreatedById = 1L, CategoryId = (int)ECategory.UsersAndPermissionsOptions }
);



            builder.Entity<Options_in_Role>().HasSeedData(
          new Options_in_Role { Id = 1, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CES, OptionValue = 1 },
                            new Options_in_Role { Id = 2, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CESTYLE, OptionValue = 1 },
                            new Options_in_Role { Id = 3, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.AO, OptionValue = 1 },
                            new Options_in_Role { Id = 4, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.DASH, OptionValue = 1 },
                            new Options_in_Role { Id = 5, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.TMPL, OptionValue = 1 },
                            new Options_in_Role { Id = 6, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.PAGS, OptionValue = 1 },
                            new Options_in_Role { Id = 7, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.BlgsArt, OptionValue = 1 },
                            new Options_in_Role { Id = 8, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.SRVY, OptionValue = 1 },
                            new Options_in_Role { Id = 9, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.MEMGER, OptionValue = 1 },
                            new Options_in_Role { Id = 10, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.MENUS, OptionValue = 1 },
                            new Options_in_Role { Id = 11, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.THEME, OptionValue = 1 },
                            new Options_in_Role { Id = 12, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.USRAPER, OptionValue = 1 },
                            new Options_in_Role { Id = 13, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.RELSMANGR, OptionValue = 1 },
                            new Options_in_Role { Id = 14, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.STS, OptionValue = 1 },
                            new Options_in_Role { Id = 15, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.DTSRC, OptionValue = 1 },
                            new Options_in_Role { Id = 16, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.USRS, OptionValue = 1 },
                            new Options_in_Role { Id = 17, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.RLE, OptionValue = 1 },
                            new Options_in_Role { Id = 18, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.RLSMANGE, OptionValue = 1 },
                            new Options_in_Role { Id = 19, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CSPD, OptionValue = 1 },
                            new Options_in_Role { Id = 20, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.COPB, OptionValue = 1 },
                            new Options_in_Role { Id = 21, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CEP, OptionValue = 1 },
                            new Options_in_Role { Id = 22, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CPUP, OptionValue = 1 },
                            new Options_in_Role { Id = 23, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CEPT, OptionValue = 1 },
                            new Options_in_Role { Id = 24, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDUP, OptionValue = 1 },
                            new Options_in_Role { Id = 25, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDP, OptionValue = 1 },
                            new Options_in_Role { Id = 26, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CANP, OptionValue = 1 },
                            new Options_in_Role { Id = 27, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CVP, OptionValue = 1 },
                            new Options_in_Role { Id = 28, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CVBP, OptionValue = 1 },
                            new Options_in_Role { Id = 29, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CANBP, OptionValue = 1 },
                            new Options_in_Role { Id = 30, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CEBP, OptionValue = 1 },
                            new Options_in_Role { Id = 31, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CEBPT, OptionValue = 1 },
                            new Options_in_Role { Id = 32, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDBP, OptionValue = 1 },
                            new Options_in_Role { Id = 33, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CANSM, OptionValue = 1 },
                            new Options_in_Role { Id = 34, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CVSM, OptionValue = 1 },
                            new Options_in_Role { Id = 35, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CESM, OptionValue = 1 },
                            new Options_in_Role { Id = 36, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CPUSM, OptionValue = 1 },
                            new Options_in_Role { Id = 37, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDSM, OptionValue = 1 },
                            new Options_in_Role { Id = 38, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CESMT, OptionValue = 1 },
                            new Options_in_Role { Id = 39, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CANT, OptionValue = 1 },
                            new Options_in_Role { Id = 40, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.COBT, OptionValue = 1 },
                            new Options_in_Role { Id = 41, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDUT, OptionValue = 1 },
                            new Options_in_Role { Id = 42, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CET, OptionValue = 1 },
                            new Options_in_Role { Id = 43, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDT, OptionValue = 1 },
                            new Options_in_Role { Id = 44, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CANF, OptionValue = 1 },
                            new Options_in_Role { Id = 45, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CUF, OptionValue = 1 },
                            new Options_in_Role { Id = 46, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CUSF, OptionValue = 1 },
                            new Options_in_Role { Id = 47, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CUMF, OptionValue = 1 },
                            new Options_in_Role { Id = 48, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CVAF, OptionValue = 1 },
                            new Options_in_Role { Id = 49, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CVIT, OptionValue = 1 },
                            new Options_in_Role { Id = 50, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CVVT, OptionValue = 1 },
                            new Options_in_Role { Id = 51, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CVDT, OptionValue = 1 },
                            new Options_in_Role { Id = 52, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CVFT, OptionValue = 1 },
                            new Options_in_Role { Id = 53, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CCFL, OptionValue = 1 },
                            new Options_in_Role { Id = 54, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CVIE, OptionValue = 1 },
                            new Options_in_Role { Id = 55, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDF, OptionValue = 1 },
                            new Options_in_Role { Id = 56, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CASOST, OptionValue = 1 },
                            new Options_in_Role { Id = 57, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CASOPS, OptionValue = 1 },
                            new Options_in_Role { Id = 58, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CASOTS, OptionValue = 1 },
                            new Options_in_Role { Id = 59, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CASOCS, OptionValue = 1 },
                            new Options_in_Role { Id = 60, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CANST, OptionValue = 1 },
                            new Options_in_Role { Id = 61, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CVST, OptionValue = 1 },
                            new Options_in_Role { Id = 62, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CACOST, OptionValue = 1 },
                            new Options_in_Role { Id = 63, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CECOST, OptionValue = 1 },
                            new Options_in_Role { Id = 64, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDCOST, OptionValue = 1 },
                            new Options_in_Role { Id = 65, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CVCOST, OptionValue = 1 },
                            new Options_in_Role { Id = 66, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CVFOST, OptionValue = 1 },
                            new Options_in_Role { Id = 67, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CAFOST, OptionValue = 1 },
                            new Options_in_Role { Id = 68, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CEFOST, OptionValue = 1 },
                            new Options_in_Role { Id = 69, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDFOST, OptionValue = 1 },
                            new Options_in_Role { Id = 70, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CVCST, OptionValue = 1 },
                            new Options_in_Role { Id = 71, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CACST, OptionValue = 1 },
                            new Options_in_Role { Id = 72, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDCST, OptionValue = 1 },
                            new Options_in_Role { Id = 73, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CECST, OptionValue = 1 },
                            new Options_in_Role { Id = 74, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CASST, OptionValue = 1 },
                            new Options_in_Role { Id = 75, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CRST, OptionValue = 1 },
                            new Options_in_Role { Id = 76, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDST, OptionValue = 1 },
                            new Options_in_Role { Id = 77, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CSSTAD, OptionValue = 1 },
                            new Options_in_Role { Id = 78, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDSTE, OptionValue = 1 },
                            new Options_in_Role { Id = 79, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CANMC, OptionValue = 1 },
                            new Options_in_Role { Id = 80, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CEMC, OptionValue = 1 },
                            new Options_in_Role { Id = 81, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDUMC, OptionValue = 1 },
                            new Options_in_Role { Id = 82, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDMC, OptionValue = 1 },
                            new Options_in_Role { Id = 83, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CANM, OptionValue = 1 },
                            new Options_in_Role { Id = 84, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CACM, OptionValue = 1 },
                            new Options_in_Role { Id = 85, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CPURM, OptionValue = 1 },
                            new Options_in_Role { Id = 86, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CERM, OptionValue = 1 },
                            new Options_in_Role { Id = 87, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDRM, OptionValue = 1 },
                            new Options_in_Role { Id = 88, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CPUCM, OptionValue = 1 },
                            new Options_in_Role { Id = 89, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CECM, OptionValue = 1 },
                            new Options_in_Role { Id = 90, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDCM, OptionValue = 1 },
                            new Options_in_Role { Id = 91, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CANS, OptionValue = 1 },
                            new Options_in_Role { Id = 92, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CDS, OptionValue = 1 },
                            new Options_in_Role { Id = 93, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CSSAD, OptionValue = 1 },
                            new Options_in_Role { Id = 94, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CESR, OptionValue = 1 },
                            new Options_in_Role { Id = 95, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.SUAC, OptionValue = 1 },
                            new Options_in_Role { Id = 96, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.SGVC, OptionValue = 1 },
                            new Options_in_Role { Id = 97, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.SCSSC, OptionValue = 1 },
                            new Options_in_Role { Id = 98, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.SJSC, OptionValue = 1 },
                            new Options_in_Role { Id = 99, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.SJSONC, OptionValue = 1 },
                            new Options_in_Role { Id = 100, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CASTU, OptionValue = 1 },
                            new Options_in_Role { Id = 101, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CAEC, OptionValue = 1 },
                            new Options_in_Role { Id = 102, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATP, OptionValue = 1 },
                            new Options_in_Role { Id = 103, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATC, OptionValue = 1 },
                            new Options_in_Role { Id = 104, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATO, OptionValue = 1 },
                            new Options_in_Role { Id = 105, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATPP, OptionValue = 1 },
                            new Options_in_Role { Id = 106, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATPAG, OptionValue = 1 },
                            new Options_in_Role { Id = 107, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATPA, OptionValue = 1 },
                            new Options_in_Role { Id = 108, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATPT, OptionValue = 1 },
                            new Options_in_Role { Id = 109, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATS, OptionValue = 1 },
                            new Options_in_Role { Id = 110, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATCPR, OptionValue = 1 },
                            new Options_in_Role { Id = 111, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATCPRU, OptionValue = 1 },
                            new Options_in_Role { Id = 112, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATSI, OptionValue = 1 },
                            new Options_in_Role { Id = 113, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATWI, OptionValue = 1 },
                            new Options_in_Role { Id = 114, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATMWPI, OptionValue = 1 },
                            new Options_in_Role { Id = 115, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATVEN, OptionValue = 1 },
                            new Options_in_Role { Id = 116, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATDIS, OptionValue = 1 },
                            new Options_in_Role { Id = 117, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATTC, OptionValue = 1 },
                            new Options_in_Role { Id = 118, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATTR, OptionValue = 1 },
                            new Options_in_Role { Id = 119, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATPL, OptionValue = 1 },
                            new Options_in_Role { Id = 120, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATPPV, OptionValue = 1 },
                            new Options_in_Role { Id = 121, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATCM, OptionValue = 1 },
                            new Options_in_Role { Id = 122, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATSM, OptionValue = 1 },
                            new Options_in_Role { Id = 123, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATPM, OptionValue = 1 },
                            new Options_in_Role { Id = 124, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATIM, OptionValue = 1 },
                            new Options_in_Role { Id = 125, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATVM, OptionValue = 1 },
                            new Options_in_Role { Id = 126, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATDM, OptionValue = 1 },
                            new Options_in_Role { Id = 127, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATTM, OptionValue = 1 },
                            new Options_in_Role { Id = 128, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATPLM, OptionValue = 1 },
                            new Options_in_Role { Id = 129, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATPTS, OptionValue = 1 },
                            new Options_in_Role { Id = 130, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATUP, OptionValue = 1 },
                            new Options_in_Role { Id = 131, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATPS, OptionValue = 1 },
                            new Options_in_Role { Id = 132, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATUB, OptionValue = 1 },
                            new Options_in_Role { Id = 133, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATProv, OptionValue = 1 },
                            new Options_in_Role { Id = 134, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATDrivers, OptionValue = 1 },
                            new Options_in_Role { Id = 135, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATRC, OptionValue = 1 },
                            new Options_in_Role { Id = 136, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATB, OptionValue = 1 },
                            new Options_in_Role { Id = 137, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATPV, OptionValue = 1 },
                            new Options_in_Role { Id = 138, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CATD, OptionValue = 1 },
                            new Options_in_Role { Id = 139, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CRS, OptionValue = 1 },
                            new Options_in_Role { Id = 151, CreatedById = 1L, CreationDate = seedDateOffset, ModificationDate = seedDateOffset, RoleId = 1, OptionId = (int)EOptions.CRES, OptionValue = 1 }
           );

            builder.Entity<CategoryTypes>().HasSeedData(
       new CategoryTypes((long)ECategoryTypes.Menu) { Name = ECategoryTypes.Menu.ToString(), Description = "Menu type for web app", CreationDate = seedDateOffset, CreatedById = 1L },
                         new CategoryTypes((long)ECategoryTypes.Product) { Name = ECategoryTypes.Product.ToString(), Description = "Product Category", CreationDate = seedDateOffset, CreatedById = 1L },
                         new CategoryTypes((long)ECategoryTypes.Component) { Name = ECategoryTypes.Component.ToString(), Description = "Component Category", CreationDate = seedDateOffset, CreatedById = 1L },
                         new CategoryTypes((long)ECategoryTypes.AdminMenu) { Name = ECategoryTypes.AdminMenu.ToString(), Description = "Menu type for Admin", CreationDate = seedDateOffset, CreatedById = 1L },
                         new CategoryTypes((long)ECategoryTypes.Page) { Name = ECategoryTypes.Page.ToString(), Description = "Pages Types", CreationDate = seedDateOffset, CreatedById = 1L },
                         new CategoryTypes((long)ECategoryTypes.RoleOptions) { Name = ECategoryTypes.RoleOptions.ToString(), Description = "RoleOption Types", CreationDate = seedDateOffset, CreatedById = 1L },
                         new CategoryTypes((long)ECategoryTypes.Post) { Name = ECategoryTypes.Post.ToString(), Description = "Post Types", CreationDate = seedDateOffset, CreatedById = 1L },
                         new CategoryTypes((long)ECategoryTypes.Events) { Name = ECategoryTypes.Events.ToString(), Description = "Events Types", CreationDate = seedDateOffset, CreatedById = 1L },
                         new CategoryTypes((long)ECategoryTypes.PricingQuotation) { Name = ECategoryTypes.PricingQuotation.ToString(), Description = "PricingQuotation Types", CreationDate = seedDateOffset, CreatedById = 1L },
                         new CategoryTypes((long)ECategoryTypes.ToDoList) { Name = ECategoryTypes.ToDoList.ToString(), Description = "ToDo List Category", CreationDate = seedDateOffset, CreatedById = 1L }
       );

            builder.Entity<UserRole>().HasSeedData(
              new UserRole { UserId = 1L, RoleId = 1L }
          );

            builder.Entity<Role>().HasSeedData(
              new Role { Id = (long)ERoleTypes.Admin, ConcurrencyStamp = "4776a1b2-dbe4-4056-82ec-8bed211d1454", Name = ERoleTypes.Admin.ToString(), NormalizedName = ERoleTypes.Admin.ToString().ToUpper(), Type = ERoleTypes.Admin , IsSystemAdmin=true },
              new Role { Id = (long)ERoleTypes.Designer, ConcurrencyStamp = "00d172be-03a0-4856-8b12-26d63fcf4374", Name = ERoleTypes.Designer.ToString(), NormalizedName = ERoleTypes.Designer.ToString().ToUpper(), Type = ERoleTypes.Admin, IsSystemAdmin = true },
              new Role { Id = (long)ERoleTypes.BlogPosts, ConcurrencyStamp = "d4754388-8355-4018-b728-218018836817", Name = ERoleTypes.BlogPosts.ToString(), NormalizedName = ERoleTypes.BlogPosts.ToString().ToUpper(), Type = ERoleTypes.Admin },
              new Role { Id = (long)ERoleTypes.Guest, ConcurrencyStamp = "71f10604-8c4d-4a7d-ac4a-ffefb11cefeb", Name = ERoleTypes.Guest.ToString(), NormalizedName = ERoleTypes.Guest.ToString().ToUpper(), Type = ERoleTypes.Admin },
              new Role { Id = (long)ERoleTypes.ECommerce, ConcurrencyStamp = "81f10604-8c4d-4a7d-ac4a-ffefb11cefcd", Name = ERoleTypes.ECommerce.ToString(), NormalizedName = ERoleTypes.ECommerce.ToString().ToUpper(), Type = ERoleTypes.Admin , IsSystemAdmin = true },
              new Role { Id = (long)ERoleTypes.Customer, ConcurrencyStamp = "81f10604-8c4d-4a7d-ac4a-ffefb11cefcd", Name = ERoleTypes.Customer.ToString(), NormalizedName = ERoleTypes.Customer.ToString().ToUpper(), Type = ERoleTypes.Admin },
              new Role { Id = (long)ERoleTypes.Auther, ConcurrencyStamp = "81f10604-8c4d-4a7d-ac4a-ffefb11cefcd", Name = ERoleTypes.Auther.ToString(), NormalizedName = ERoleTypes.Auther.ToString().ToUpper(), Type = ERoleTypes.Admin }

);


            builder.Entity<UsersSites>().HasSeedData(
            new UsersSites { Id = 1L, UserId = 1, SiteId = 1, IsDefault = true }
        );

            builder.Entity<Site>().HasSeedData(
             new Site { Id = 1L, Name = "Default Site", CreatedById = 1L, CreationDate = seedDateOffset, Description = "Default Site Description", Status = EStatus.Published, StatusDate = seedDateOffset });

            builder.Entity<User>().HasSeedData(
        new User
        {
            Id = 1L,
            AccessFailedCount = 0,
            ConcurrencyStamp = "05286794-6c65-4c3e-8de2-e6a10b85d050",
            CreationDate = seedDateOffset,
            CreatedById = 1L,
            Email = "f.sirhan@eskadenia.com",
            EmailConfirmed = false,
            FullName = "System User",
            LockoutEnabled = false,
            NormalizedEmail = "F.SIRHAN@ESKADENIA.COM",
            NormalizedUserName = "F.SIRHAN@ESKADENIA.COM",
            PasswordHash = "AQAAAAEAACcQAAAAELIGzYdxN6Giv7M0KV3bOlPZanDbaOH+u71HsLZp1d8sNf0lOhnP+Kb4zRt7C1h7Dw==",
            PhoneNumberConfirmed = false,
            SecurityStamp = "AQAAAAEAACcQAAAAELIGzYdxN6Giv7M0KV3bOlPZanDbaOH+u71HsLZp1d8sNf0lOhnP+Kb4zRt7C1h7Dw==",
            TwoFactorEnabled = false,
            UserName = "f.sirhan@eskadenia.com",
            Status = EStatus.Active,
            StatusDate = seedDateOffset,
            Type = EUsersTypes.Admin,
            lastPasswordUpdate=seedDate,
            RequestStatusDate = seedDateOffset,
        }
     );

            builder.Entity<CustomerGroup>().HasSeedData(
              new CustomerGroup { Id = 2, FireBaseTopic = "GuestUsers", IsActive = true, IsDeleted = false, SiteId = null, CreatedOn = seedDateOffset, LatestUpdatedOn = seedDateOffset, Email = "f.sirhan@eskadenia.com",MaxSessionsNumber =4,PasswordExpiryInDays=30 },
                 new CustomerGroup
                 {
                     Id = 1,
                   
                     FireBaseTopic = "NoTopic",
                     IsActive = true,
                     IsDeleted = false,
                     SiteId = null,
                     CreatedOn = seedDateOffset,
                     LatestUpdatedOn = seedDateOffset,
                     Email = "f.sirhan@eskadenia.com",
                     Users = new List<CustomerGroupUser>(),
                     MaxSessionsNumber=4,
                     PasswordExpiryInDays=30,
                 }
                       
                 );

            builder.Entity<CustomerGroupDetails>().HasSeedData(
              new CustomerGroupDetails { Id = 2, Name = "Guests", Description = "Group for Guests",CustomerGroupId=2 },
                 new CustomerGroupDetails
                 {
                     Id = 1,
                     Name = "Default Group",
                     Description = "Default group for all users",
                     CustomerGroupId=1
                    
                 }

                 );
            builder.Entity<CustomerGroupUser>().HasSeedData(
              new CustomerGroupUser { CustomerGroupId = 1, UserId = 1, CreationDate = seedDateOffset }
                );

            builder.Entity<SiteSettings>().HasSeedData(
             new SiteSettings(SMSProviders.SMSGlobal.ToString()) { Module = "SendSMS", IsVisibleInCommonSettingPage = false, Value = "{\"ApiKey\":\"8c0e4c3e21a6607ed328ab2f1a253017\",\"ApiSecret\":\"92575d6ce94643eb6356a2bdb17b4699\",\"method\":\"POST\",\"host\":\"api.smsglobal.com\",\"port\":\"443\",\"version\":\"v2\",\"path\":\"sms\",\"smsid\":\"\",\"origin\":\"Test\"}", SiteId = 1L }
             );

            builder.Entity<SiteSettings>().HasSeedData(
           new SiteSettings(SMSProviders.SMSMPP.ToString()) { Module = "SendSMS", IsVisibleInCommonSettingPage = false, Value = "{\"Url\":\"http://82.212.81.40:8080/websmpp/websms?\",\"UserName\":\"AghatiSw\",\"Password\":\"Agh1234\",\"Source_Number\":\"Adv Aghati\",\"Type\":\"\"}", SiteId = 1L }
           );
       //     builder.Entity<SiteSettings>().HasSeedData(
       //new SiteSettings("SecuirtyFiltration") { Module = "SecuirtyFiltration", IsVisibleInCommonSettingPage = true, Value = "True", SiteId = 1L });
       //     /*(long)ERoleTypes.Auther*/
       //     //           builder.Entity<Role>().HasSeedData(
       //     //                     new Role { Id = 8, ConcurrencyStamp = "4776a1b2-dbe4-4056-82ec-8bed211d1454", Name = ERoleTypes.Auther.ToString(), NormalizedName = ERoleTypes.Auther.ToString().ToUpper(), Type = ERoleTypes.Auther }
       //     //);

            builder.Entity<SiteSettings>().HasSeedData(
           new SiteSettings(SMSProviders.SMSUniFonic.ToString()) { Module = "SendSMS", IsVisibleInCommonSettingPage = false, Value = "{\"ApiURL\":\"http://api.unifonic.com/wrapper/sendSMS.php\",\"sender\":\"Der3\",\"appsid\":\"FY8dEdKmf0RJ2JyH1wKY4hBPMc6UJW\",\"encoding\":\"UTF8\"}", SiteId = 1L }

       );
            builder.Entity<SiteSettings>().HasSeedData(
        new SiteSettings("DublicateEmail") { Module = "CanDublicate", IsVisibleInCommonSettingPage = false, Value = "1", SiteId = 1L } );

            builder.Entity<SiteSettings>().HasSeedData(
     new SiteSettings("DublicatePhoneNumber") { Module = "CanDublicate", IsVisibleInCommonSettingPage = false, Value = "1", SiteId = 1L });


            //builder.Entity<SiteSettings>().HasSeedData(
            // new SiteSettings("RoundDigit") { Module = "RoundDigit", IsVisibleInCommonSettingPage = false, Value = "2", SiteId = 2L });
            //builder.Entity<Media>().HasSeedData(
            //new Media { Id = 1, FileSize = 10, MediaType = 0, ContentType = "0", FileName = "testimage", PublicUrl = "test.jpg", Size = "10", Type = 0, Hight = "10", Width = "10", CreatedById = 1L, SiteId = 1, CreationDate = seedDateOffset }
            //);

            builder.Entity<SiteSettings>().HasSeedData(
                                    new SiteSettings("AndroidNotificationChannel") { Module = "PushNotification", IsVisibleInCommonSettingPage = false, Value = "DefaultChannel", SiteId = 1L });

            //builder.Entity<SiteSettings>().HasSeedData( 
            //    new SiteSettings("HyperPayTransactionUrl") { Module = "HyperPayTransactionUrl", IsVisibleInCommonSettingPage = false, Value = "https://eu-test.oppwa.com/v1/query/{id}?", SiteId = 2L });

            //builder.Entity<SiteSettings>().HasSeedData(
            //    new SiteSettings("EnviromentHyperPay") { Module = "EnviromentHyperPay", IsVisibleInCommonSettingPage = false, Value = "PRD-", SiteId = 2L });

            builder.Entity<SiteSettings>().HasSeedData(
    new SiteSettings("AcceptFriendRequestTemplateId") { Module = "FriendShipSettings", IsVisibleInCommonSettingPage = false, Value = "AcceptFriendRequestTemplate", SiteId = 1L });

            builder.Entity<SiteSettings>().HasSeedData(
  new SiteSettings("GroupTypeId") { Module = "UserAchievementBackgroundService", IsVisibleInCommonSettingPage = false, Value = "2", SiteId = 1L });



        }
    }
}


