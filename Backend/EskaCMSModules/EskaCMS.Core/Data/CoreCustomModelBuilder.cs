using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Entities.Entity.ApiMapperAndLogs;

namespace EskaCMS.Core.Data
{
    public class CoreCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Page>().ToTable("Pages_Page");
            modelBuilder.Entity<Page>().HasKey(p => new { p.Id });
            modelBuilder.Entity<Page>().HasIndex(p => new { p.CultureId, p.SiteId, p.SlugInfoId }).IsUnique();//.HasName("LocalizaionIndex");
            modelBuilder.Entity<Page>().HasIndex(p => p.CultureId);

            // modelBuilder.Entity<Page>().Property(s => s.Slug).HasMaxLength(1000);


            modelBuilder.Entity<PageVersions>().ToTable("Pages_PageVersions");
            modelBuilder.Entity<PagesRoles>().ToTable("Pages_PagesRoles");
            modelBuilder.Entity<PagesRolesVersions>().ToTable("Pages_PagesRolesVersions");
            modelBuilder.Entity<PageTemplates>().ToTable("Pages_PageTemplates");
            modelBuilder.Entity<TemplateImportedFiles>().ToTable("Pages_TemplateImportedFiles");
            modelBuilder.Entity<TemplateVersions>().ToTable("Pages_TemplateVersions");

            modelBuilder.Entity<Page>()
                .HasMany(e => e.PageVersions)
                .WithOne(e => e.Page)
                .HasForeignKey(e => e.PageId);
            //.OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<Page>()
                .HasOne(e => e.CurrentVersion)
                .WithOne()
                .HasForeignKey<Page>(e => e.CurrentVersionId);
            //.OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PageTemplates>()
                .HasMany(e => e.PageTemplateVersions)
                .WithOne(e => e.Template)
                .HasForeignKey(e => e.TemplateId);
            //.OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PageTemplates>()
                .HasOne(e => e.CurrentVersion)
                .WithOne()
                .HasForeignKey<PageTemplates>(e => e.CurrentVersionId);

            modelBuilder.Entity<Page>()
               .HasOne(e => e.ParentLoginPage)
               .WithMany(e => e.AuthorizedPages)
               .HasForeignKey(e => e.ParentLoginPageId);

            modelBuilder.Entity<Page>().HasIndex(x => x.ParentLoginPageId).IsUnique(false);

            modelBuilder.Entity<PagePreviewCodes>().HasIndex(x => x.Code).IsUnique(true);

            modelBuilder.Entity<SiteSettings>().HasKey(ba => new { ba.Id, ba.SiteId });
            modelBuilder.Entity<SiteSettings>().ToTable("Core_AppSetting");
            modelBuilder.Entity<ApiMapper>().ToTable("Core_ApiMapper");
            modelBuilder.Entity<ApiMapperParams>().ToTable("Core_ApiMapperParams");
            modelBuilder.Entity<APIMapperParamChild>().ToTable("Core_ApiMapperParamsChild");

            modelBuilder.Entity<UsersSites>().ToTable("Core_UsersSites");
            modelBuilder.Entity<EmailTemplates>().ToTable("Core_EmailTemplates");
            modelBuilder.Entity<EmailTemplates>().HasKey(ba => new { ba.Id, ba.SiteId });
            modelBuilder.Entity<Media>().ToTable("Core_Media");
            modelBuilder.Entity<Media>().HasKey(ba => new { ba.Id });

            modelBuilder.Entity<Media>().HasIndex(ba => new { ba.Filename, ba.SiteId ,ba.FolderId,ba.Status }).IsUnique();


            //modelBuilder.Entity<MediaDetails>().ToTable("Core_MediaDetails");
            //modelBuilder.Entity<MediaDetails>().HasKey(ba => new { ba.Id });
            //modelBuilder.Entity<MediaDetails>().HasOne<SitesCultures>(ba => ba.Culture);
            //modelBuilder.Entity<MediaDetails>()
            //        .HasIndex(ba => new { ba.CultureId, ba.MediaId }).IsUnique();




            modelBuilder.Entity<SitesHostNames>().ToTable("Core_SitesHostNames");
            modelBuilder.Entity<SitesHostNames>().HasIndex(h => new { h.HostName, h.Prefix }).IsUnique();




            modelBuilder.Entity<MediaFolders>().ToTable("Core_MediaFolders");
            modelBuilder.Entity<MediaFolders>().HasKey(ba => new { ba.Id });

            modelBuilder.Entity<MediaFoldersDetails>().HasIndex(ba => new {  ba.CultureId, ba.FolderId }).IsUnique();
            //modelBuilder.Entity<MediaFoldersDetails>().HasOne<Media>(x => x.ThumbnailMedia).WithOne(x =>x.FolderDetails).HasForeignKey<MediaFoldersDetails>(x => x.ThumbnailId).IsRequired(false);
           
            modelBuilder.Entity<MediaFolders>().HasMany(a => a.Media).WithOne(x => x.Folder).HasForeignKey(x => x.FolderId);

            modelBuilder.Entity<CurrentLoggedInUsers>(b =>
            {
                b.ToTable("Core_CurrentLoggedInUsers");
                b.HasIndex(x => new { x.UserId, x.SiteId })
                    .HasDatabaseName("IX_Core_CurrentLoggedInUsers_UserId_SiteId");
                b.HasIndex(x => new { x.ClientIP, x.UserId })
                    .HasDatabaseName("IX_Core_CurrentLoggedInUsers_ClientIP_UserId");
                b.HasIndex(x => new { x.TypeLogin, x.UserId, x.ApiToken, x.KillSession })
                    .HasDatabaseName("IX_Core_CurrentLoggedInUsers_ActiveSession");
            });

            modelBuilder.Entity<Site>()
                .HasOne(x => x.Thumbnail)
                 .WithOne(e => e.Site)
                 .HasForeignKey<Site>(p => p.ThumbnailId);


            modelBuilder.Entity<Category>().ToTable("Core_Category");
            modelBuilder.Entity<Category>().HasKey(p => new { p.Id });
            modelBuilder.Entity<CategoryDetails>().HasIndex(p => new { p.CultureId,  p.SlugInfoId }).IsUnique();//.HasName("LocalizaionIndex");
            modelBuilder.Entity<CategoryDetails>().HasIndex(p => p.CultureId);
            modelBuilder.Entity<Option>()
              .HasOne(s => s.Category)
              .WithMany(g => g.Options)
              .HasForeignKey(s => s.CategoryId)
              .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
              .ToTable("Core_User");
            modelBuilder.Entity<User>()
              .Property(P => P.PhoneNumber).HasMaxLength(15);

            //modelBuilder.Entity<SitesSecurityPolicy>()
            //    .ToTable("Core_SitesSecurityPolicy");

            modelBuilder.Entity<Role>()
                .ToTable("Core_Role");

            modelBuilder.Entity<IdentityUserClaim<long>>(b =>
            {
                b.HasKey(uc => uc.Id);
                b.ToTable("Core_UserClaim");
            });

            modelBuilder.Entity<IdentityRoleClaim<long>>(b =>
            {
                b.HasKey(rc => rc.Id);
                b.ToTable("Core_RoleClaim");
            });


            modelBuilder.Entity<UserRole>(b =>
            {
                b.HasKey(ur => new { ur.UserId, ur.RoleId });
                b.HasOne(ur => ur.Role).WithMany(x => x.Users).HasForeignKey(r => r.RoleId);
                b.HasOne(ur => ur.User).WithMany(x => x.UserRoles).HasForeignKey(u => u.UserId);
                b.ToTable("Core_UserRole");
            }); 
            
            modelBuilder.Entity<BackgroundServiceSettings>(b =>
            {
                b.HasKey(ur => new { ur.SiteId, ur.Id });
                b.HasIndex(ur => ur.Id).HasDatabaseName("IX_Core_BackgroundServiceSettings_Id");
                b.ToTable("Core_BackgroundServiceSettings");
            });

            modelBuilder.Entity<UsersSites>(b =>
            {
                b.HasIndex(ur => new { ur.UserId, ur.SiteId, ur.IsLockout })
                    .HasDatabaseName("IX_Core_UsersSites_UserId_SiteId_IsLockout");
            });

            modelBuilder.Entity<IdentityUserLogin<long>>(b =>
            {
                b.ToTable("Core_UserLogin");
            });

            modelBuilder.Entity<IdentityUserToken<long>>(b =>
            {
             
                b.ToTable("Core_UserToken");
            });
           
                //modelBuilder.Entity<UserOTP>().HasIndex(p => new { p.SiteId, p.Value, p.Username, p.Purpose,p.ExpiryDate}).IsUnique();
             
         
          
            modelBuilder.Entity<Entity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.EntityId);
            });

            modelBuilder.Entity<CustomerGroupUser>(b =>
            {
                b.HasKey(ur => new { ur.UserId, ur.CustomerGroupId });
                b.HasOne(ur => ur.User).WithMany(r => r.CustomerGroups).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(ur => ur.CustomerGroup).WithMany(u => u.Users).HasForeignKey(u => u.CustomerGroupId).OnDelete(DeleteBehavior.Cascade);
                b.ToTable("Core_CustomerGroupUser");
            });

            modelBuilder.Entity<SlugInfo>().ToTable("Core_SlugInfo");
            modelBuilder.Entity<SlugInfo>().HasKey(p => new { p.Id });
            modelBuilder.Entity<SlugInfo>().HasIndex(p => new { p.Slug, p.CultureId, p.SiteId }).IsUnique();
            CoreSeedData.SeedData(modelBuilder);
            //-------------------** Testing Wise **----------------------------


            //-------------------** End Of Testing Wise **----------------------------

            //modelBuilder.Entity<UsersSites>().HasKey(sc => new { sc.SiteId, sc.UserId });
            //modelBuilder.Entity<UsersSites>()
            //    .HasOne<User>(sc => sc.User)
            //    .WithMany(s => s.UserSites)
            //    .HasForeignKey(sc => sc.UserId);
            //modelBuilder.Entity<UsersSites>()
            //    .HasOne<Site>(sc => sc.Site)
            //    .WithMany(s => s.UserSite)
            //    .HasForeignKey(sc => sc.SiteId);
            //     modelBuilder.Entity<SiteSettings>()
            //.Property(b => b.Id).HasMaxLength(250);

            //     modelBuilder.Entity<SiteSettings>()
            //    .HasKey(o => new { o.Id, o.SiteId });

            // }
        }

    }
}
