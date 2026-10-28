using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EskaCMS.Data.Model.Models
{
    public partial class EskaCMSDbContext : DbContext
    {
        public EskaCMSDbContext()
        {
        }

        public EskaCMSDbContext(DbContextOptions<EskaCMSDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<DcmsAliases> DcmsAliases { get; set; }
        public virtual DbSet<DcmsCategories> DcmsCategories { get; set; }
        public virtual DbSet<DcmsComponents> DcmsComponents { get; set; }
        public virtual DbSet<DcmsDomainValues> DcmsDomainValues { get; set; }
        public virtual DbSet<DcmsDomains> DcmsDomains { get; set; }
        public virtual DbSet<DcmsForms> DcmsForms { get; set; }
        public virtual DbSet<DcmsLayoutComponent> DcmsLayoutComponent { get; set; }
        public virtual DbSet<DcmsLayouts> DcmsLayouts { get; set; }
        public virtual DbSet<DcmsMedia> DcmsMedia { get; set; }
        public virtual DbSet<DcmsMediaFolders> DcmsMediaFolders { get; set; }
        public virtual DbSet<DcmsMediaVersions> DcmsMediaVersions { get; set; }
        public virtual DbSet<DcmsMenus> DcmsMenus { get; set; }
        public virtual DbSet<DcmsMenusSettings> DcmsMenusSettings { get; set; }
        public virtual DbSet<DcmsPages> DcmsPages { get; set; }
        public virtual DbSet<DcmsPagesComponents> DcmsPagesComponents { get; set; }
        public virtual DbSet<DcmsPagesSettings> DcmsPagesSettings { get; set; }
        public virtual DbSet<DcmsPagesTypes> DcmsPagesTypes { get; set; }
        public virtual DbSet<DcmsPostTypes> DcmsPostTypes { get; set; }
        public virtual DbSet<DcmsPosts> DcmsPosts { get; set; }
        public virtual DbSet<DcmsPostsTags> DcmsPostsTags { get; set; }
        public virtual DbSet<DcmsSiteFields> DcmsSiteFields { get; set; }
        public virtual DbSet<DcmsSites> DcmsSites { get; set; }
        public virtual DbSet<DcmsTags> DcmsTags { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
//            if (!optionsBuilder.IsConfigured)
//            {
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
//                optionsBuilder.UseMySql(Configuration.GetConnectionString("EskaCMSDatabase"), ServerVersion.AutoDetect(Configuration.GetConnectionString("EskaCMSDatabase")));
//            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.4-servicing-10062");

            modelBuilder.Entity<DcmsAliases>(entity =>
            {
                entity.ToTable("dcms_aliases", "cms_dynamic");

                entity.HasIndex(e => e.SiteId)
                    .HasName("FK_SITE_ALIASES_idx");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.AliasUrl)
                    .HasColumnName("ALIAS_URL")
                    .HasMaxLength(350)
                    .IsUnicode(false);

                entity.Property(e => e.CompanyId)
                    .HasColumnName("COMPANY_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.RedirectUrl)
                    .HasColumnName("REDIRECT_URL")
                    .HasMaxLength(350)
                    .IsUnicode(false);

                entity.Property(e => e.SiteId)
                    .HasColumnName("SITE_ID")
                    .HasColumnType("int(11)");

                entity.HasOne(d => d.Site)
                    .WithMany(p => p.DcmsAliases)
                    .HasForeignKey(d => d.SiteId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SITE_ALIASES");
            });

            modelBuilder.Entity<DcmsCategories>(entity =>
            {
                entity.ToTable("dcms_categories", "cms_dynamic");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.Name2)
                    .HasColumnName("NAME_2")
                    .HasMaxLength(500)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<DcmsComponents>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.TokenId, e.Name, e.Title });

                entity.ToTable("dcms_components", "cms_dynamic");

                entity.HasIndex(e => e.Category)
                    .HasName("FK_CATEGORY_ID_COMPONENT_idx");

                entity.HasIndex(e => e.SiteId)
                    .HasName("FK_SITE_COMPONENT_idx");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.TokenId)
                    .HasColumnName("TOKEN_ID")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.Title)
                    .HasColumnName("TITLE")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.Category)
                    .HasColumnName("CATEGORY")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CompnayId)
                    .HasColumnName("COMPNAY_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.DataKey)
                    .HasColumnName("DATA_KEY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.Name2)
                    .HasColumnName("NAME_2")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.SiteId)
                    .HasColumnName("SITE_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Status)
                    .HasColumnName("STATUS")
                    .HasColumnType("int(1)");

                entity.Property(e => e.StatusDate).HasColumnName("STATUS_DATE");

                entity.Property(e => e.Type)
                    .HasColumnName("TYPE")
                    .HasColumnType("int(2)");

                entity.HasOne(d => d.CategoryNavigation)
                    .WithMany(p => p.DcmsComponents)
                    .HasForeignKey(d => d.Category)
                    .HasConstraintName("FK_CATEGORY_ID_COMPONENT");

                entity.HasOne(d => d.Site)
                    .WithMany(p => p.DcmsComponents)
                    .HasForeignKey(d => d.SiteId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SITE_COMPONENT");
            });

            modelBuilder.Entity<DcmsDomainValues>(entity =>
            {
                entity.ToTable("dcms_domain_values", "cms_dynamic");

                entity.HasIndex(e => e.DomainId)
                    .HasName("FK_DOM_VAL_ID_idx");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.DomainId)
                    .HasColumnName("DOMAIN_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.Name2)
                    .HasColumnName("NAME_2")
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.Value)
                    .IsRequired()
                    .HasColumnName("VALUE")
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasDefaultValueSql("0");

                entity.HasOne(d => d.Domain)
                    .WithMany(p => p.DcmsDomainValues)
                    .HasForeignKey(d => d.DomainId)
                    .HasConstraintName("FK_DOM_VAL_ID");
            });

            modelBuilder.Entity<DcmsDomains>(entity =>
            {
                entity.ToTable("dcms_domains", "cms_dynamic");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.Name2)
                    .HasColumnName("NAME_2")
                    .HasMaxLength(500)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<DcmsForms>(entity =>
            {
                entity.ToTable("dcms_forms", "cms_dynamic");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CompanyId)
                    .HasColumnName("COMPANY_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.Settings)
                    .HasColumnName("SETTINGS")
                    .HasColumnType("longtext");

                entity.Property(e => e.SiteId)
                    .HasColumnName("SITE_ID")
                    .HasColumnType("int(11)");
            });

            modelBuilder.Entity<DcmsLayoutComponent>(entity =>
            {
                entity.ToTable("dcms_layout_component", "cms_dynamic");

                entity.HasIndex(e => e.ComponentId)
                    .HasName("FK_COMPONENT_ID_COMP_idx");

                entity.HasIndex(e => e.LayoutId)
                    .HasName("FK_LAYOUT_ID_COMP_idx");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ComponentId)
                    .HasColumnName("COMPONENT_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ComponentSettings)
                    .IsRequired()
                    .HasColumnName("COMPONENT_SETTINGS")
                    .HasColumnType("longtext");

                entity.Property(e => e.ComponentToken)
                    .HasColumnName("COMPONENT_TOKEN")
                    .HasColumnType("longtext");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.LayoutId)
                    .HasColumnName("LAYOUT_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.SettingsVersion)
                    .HasColumnName("SETTINGS_VERSION")
                    .HasColumnType("int(2)");

                entity.HasOne(d => d.Layout)
                    .WithMany(p => p.DcmsLayoutComponent)
                    .HasForeignKey(d => d.LayoutId)
                    .HasConstraintName("FK_LAYOUT_ID_COMP");
            });

            modelBuilder.Entity<DcmsLayouts>(entity =>
            {
                entity.ToTable("dcms_layouts", "cms_dynamic");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.LayoutSettings)
                    .IsRequired()
                    .HasColumnName("LAYOUT_SETTINGS")
                    .HasColumnType("longtext");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.SettignsVersion)
                    .HasColumnName("SETTIGNS_VERSION")
                    .HasColumnType("int(2)");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnName("TITLE")
                    .HasMaxLength(150)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<DcmsMedia>(entity =>
            {
                entity.ToTable("dcms_media", "cms_dynamic");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ContentType)
                    .IsRequired()
                    .HasColumnName("CONTENT_TYPE")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasColumnName("FILE_NAME")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.FloderId)
                    .HasColumnName("FLODER_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Hight)
                    .HasColumnName("HIGHT")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.PublicUrl)
                    .HasColumnName("PUBLIC_URL")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.Size)
                    .HasColumnName("SIZE")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Type)
                    .HasColumnName("TYPE")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Width)
                    .HasColumnName("WIDTH")
                    .HasColumnType("int(11)");

                entity.HasOne(d => d.IdNavigation)
                    .WithOne(p => p.DcmsMedia)
                    .HasForeignKey<DcmsMedia>(d => d.Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FOLDER_ID");
            });

            modelBuilder.Entity<DcmsMediaFolders>(entity =>
            {
                entity.ToTable("dcms_media_folders", "cms_dynamic");

                entity.HasIndex(e => e.ParentId)
                    .HasName("FK_SELF_idx");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.ParentId)
                    .HasColumnName("PARENT_ID")
                    .HasColumnType("int(11)");

                entity.HasOne(d => d.Parent)
                    .WithMany(p => p.InverseParent)
                    .HasForeignKey(d => d.ParentId)
                    .HasConstraintName("FK_SELF_ID");
            });

            modelBuilder.Entity<DcmsMediaVersions>(entity =>
            {
                entity.ToTable("dcms_media_versions", "cms_dynamic");

                entity.HasIndex(e => e.MediaId)
                    .HasName("FK_MEDIA_VERSION_ID_idx");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CrearionDate).HasColumnName("CREARION_DATE");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.FileExtention)
                    .HasColumnName("FILE_EXTENTION")
                    .HasMaxLength(45)
                    .IsUnicode(false);

                entity.Property(e => e.Hight)
                    .HasColumnName("HIGHT")
                    .HasColumnType("int(11)");

                entity.Property(e => e.MediaId)
                    .HasColumnName("MEDIA_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.Size)
                    .HasColumnName("SIZE")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Width)
                    .HasColumnName("WIDTH")
                    .HasColumnType("int(11)");

                entity.HasOne(d => d.Media)
                    .WithMany(p => p.DcmsMediaVersions)
                    .HasForeignKey(d => d.MediaId)
                    .HasConstraintName("FK_MEDIA_VERSION_ID");
            });

            modelBuilder.Entity<DcmsMenus>(entity =>
            {
                entity.ToTable("dcms_menus", "cms_dynamic");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CompnayId)
                    .HasColumnName("COMPNAY_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(45)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(350)
                    .IsUnicode(false);

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.SiteId)
                    .HasColumnName("SITE_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Type)
                    .HasColumnName("TYPE")
                    .HasColumnType("int(1)");
            });

            modelBuilder.Entity<DcmsMenusSettings>(entity =>
            {
                entity.ToTable("dcms_menus_settings", "cms_dynamic");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.MenuId)
                    .HasColumnName("MENU_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.MenuSettings)
                    .HasColumnName("MENU_SETTINGS")
                    .HasColumnType("longtext");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<DcmsPages>(entity =>
            {
                entity.ToTable("dcms_pages", "cms_dynamic");

                entity.HasIndex(e => e.LayoutId)
                    .HasName("FK_LAYOUT_ID_idx");

                entity.HasIndex(e => e.PageCategoryId)
                    .HasName("FK_CATEGORY_PAGE_idx");

                entity.HasIndex(e => e.PageTypeId)
                    .HasName("FK_PAGE_TYPE_PAGE_ID_idx");

                entity.HasIndex(e => e.SiteId)
                    .HasName("FK_SITE_ID_PAGE_idx");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CompanyId)
                    .HasColumnName("COMPANY_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.IsHidden)
                    .HasColumnName("IS_HIDDEN")
                    .HasColumnType("int(1)");

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasColumnName("KEY")
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.LayoutId)
                    .HasColumnName("LAYOUT_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.NavigationTitle)
                    .IsRequired()
                    .HasColumnName("NAVIGATION_TITLE")
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.PageCategoryId)
                    .HasColumnName("PAGE_CATEGORY_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.PageOder)
                    .HasColumnName("PAGE_ODER")
                    .HasColumnType("int(3)");

                entity.Property(e => e.PageSettings)
                    .HasColumnName("PAGE_SETTINGS")
                    .HasColumnType("longtext");

                entity.Property(e => e.PageTypeId)
                    .HasColumnName("PAGE_TYPE_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.PageVersion)
                    .HasColumnName("PAGE_VERSION")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ParentId)
                    .HasColumnName("PARENT_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Published).HasColumnName("PUBLISHED");

                entity.Property(e => e.Route)
                    .HasColumnName("ROUTE")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.SiteId)
                    .HasColumnName("SITE_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Status)
                    .HasColumnName("STATUS")
                    .HasColumnType("int(3)");

                entity.Property(e => e.StatusDate).HasColumnName("STATUS_DATE");

                entity.HasOne(d => d.Layout)
                    .WithMany(p => p.DcmsPages)
                    .HasForeignKey(d => d.LayoutId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LAYOUT_ID_PAGE");

                entity.HasOne(d => d.PageCategory)
                    .WithMany(p => p.DcmsPages)
                    .HasForeignKey(d => d.PageCategoryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CATEGORY_PAGE");

                entity.HasOne(d => d.PageType)
                    .WithMany(p => p.DcmsPages)
                    .HasForeignKey(d => d.PageTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PAGE_TYPE_PAGE");

                entity.HasOne(d => d.Site)
                    .WithMany(p => p.DcmsPages)
                    .HasForeignKey(d => d.SiteId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SITE_ID_OAGES");
            });

            modelBuilder.Entity<DcmsPagesComponents>(entity =>
            {
                entity.ToTable("dcms_pages_components", "cms_dynamic");

                entity.HasIndex(e => e.ComponentId)
                    .HasName("FK_COMPNENT_ID_idx");

                entity.HasIndex(e => e.PageId)
                    .HasName("FK_PAGE_ID_idx");

                entity.HasIndex(e => new { e.ComponentId, e.PageId, e.SettingVersion })
                    .HasName("FK_UNQIUE_ID")
                    .IsUnique();

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ComponentId)
                    .HasColumnName("COMPONENT_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.PageId)
                    .HasColumnName("PAGE_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.SettingVersion)
                    .HasColumnName("SETTING_VERSION")
                    .HasColumnType("int(2)");

                entity.Property(e => e.Settings)
                    .IsRequired()
                    .HasColumnName("SETTINGS")
                    .HasColumnType("longtext");

                entity.Property(e => e.TokenKey)
                    .HasColumnName("TOKEN_KEY")
                    .HasMaxLength(350)
                    .IsUnicode(false);

                entity.HasOne(d => d.Page)
                    .WithMany(p => p.DcmsPagesComponents)
                    .HasForeignKey(d => d.PageId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PAGE_ID");
            });

            modelBuilder.Entity<DcmsPagesSettings>(entity =>
            {
                entity.ToTable("dcms_pages_settings", "cms_dynamic");

                entity.HasIndex(e => e.PageId)
                    .HasName("FK_PAGE_SETTINGS_ID_idx");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.MetaDescription)
                    .HasColumnName("META_DESCRIPTION")
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.Property(e => e.MetaKeywords)
                    .HasColumnName("META_KEYWORDS")
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.PageId)
                    .HasColumnName("PAGE_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.RedirectType)
                    .HasColumnName("REDIRECT_TYPE")
                    .HasColumnType("int(2)");

                entity.Property(e => e.Slug)
                    .HasColumnName("SLUG")
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.HasOne(d => d.Page)
                    .WithMany(p => p.DcmsPagesSettings)
                    .HasForeignKey(d => d.PageId)
                    .HasConstraintName("FK_PAGE_SETTINGS_ID");
            });

            modelBuilder.Entity<DcmsPagesTypes>(entity =>
            {
                entity.ToTable("dcms_pages_types", "cms_dynamic");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Body)
                    .IsRequired()
                    .HasColumnName("BODY")
                    .HasColumnType("longtext");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<DcmsPostTypes>(entity =>
            {
                entity.ToTable("dcms_post_types", "cms_dynamic");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Body)
                    .HasColumnName("BODY")
                    .HasColumnType("longtext");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<DcmsPosts>(entity =>
            {
                entity.ToTable("dcms_posts", "cms_dynamic");

                entity.HasIndex(e => e.CategoryId)
                    .HasName("FK_POST_CATEGORY_ID_idx");

                entity.HasIndex(e => e.PageId)
                    .HasName("FK_PAGE_POST_ID_idx");

                entity.HasIndex(e => e.PostTypeId)
                    .HasName("FK_POSTTYPE_POSTS_ID_idx");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CategoryId)
                    .HasColumnName("CATEGORY_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IsPublished)
                    .IsRequired()
                    .HasColumnName("IS_PUBLISHED")
                    .HasMaxLength(45)
                    .IsUnicode(false);

                entity.Property(e => e.MetaDescription)
                    .HasColumnName("META_DESCRIPTION")
                    .HasMaxLength(300)
                    .IsUnicode(false);

                entity.Property(e => e.MetaKeywords)
                    .HasColumnName("META_KEYWORDS")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.PageId)
                    .HasColumnName("PAGE_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.PostTypeId)
                    .HasColumnName("POST_TYPE_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.PublishDate).HasColumnName("PUBLISH_DATE");

                entity.Property(e => e.RedirectType)
                    .HasColumnName("REDIRECT_TYPE")
                    .HasColumnType("int(11)");

                entity.Property(e => e.RedirectUrl)
                    .HasColumnName("REDIRECT_URL")
                    .HasMaxLength(300)
                    .IsUnicode(false);

                entity.Property(e => e.Route)
                    .HasColumnName("ROUTE")
                    .HasMaxLength(300)
                    .IsUnicode(false);

                entity.Property(e => e.Slug)
                    .IsRequired()
                    .HasColumnName("SLUG")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnName("TITLE")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.DcmsPosts)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_POST_CATEGORY_ID");

                entity.HasOne(d => d.Page)
                    .WithMany(p => p.DcmsPosts)
                    .HasForeignKey(d => d.PageId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PAGE_POST_ID");

                entity.HasOne(d => d.PostType)
                    .WithMany(p => p.DcmsPosts)
                    .HasForeignKey(d => d.PostTypeId)
                    .HasConstraintName("FK_POSTTYPE_POSTS_ID");
            });

            modelBuilder.Entity<DcmsPostsTags>(entity =>
            {
                entity.ToTable("dcms_posts_tags", "cms_dynamic");

                entity.HasIndex(e => e.PostId)
                    .HasName("FK_TAGS_POSTS_idx");

                entity.HasIndex(e => e.TagId)
                    .HasName("FK_POSTS_TAGS_idx");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.PostId)
                    .HasColumnName("POST_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.TagId)
                    .HasColumnName("TAG_ID")
                    .HasColumnType("int(11)");

                entity.HasOne(d => d.Post)
                    .WithMany(p => p.DcmsPostsTags)
                    .HasForeignKey(d => d.PostId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TAGS_POSTS");

                entity.HasOne(d => d.Tag)
                    .WithMany(p => p.DcmsPostsTags)
                    .HasForeignKey(d => d.TagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_POSTS_TAGS");
            });

            modelBuilder.Entity<DcmsSiteFields>(entity =>
            {
                entity.ToTable("dcms_site_fields", "cms_dynamic");

                entity.HasIndex(e => e.SiteId)
                    .HasName("FK_SITE_FIELDS_ID_idx");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(45)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.SiteId)
                    .HasColumnName("SITE_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.SortOrder)
                    .HasColumnName("SORT_ORDER")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Value)
                    .HasColumnName("VALUE")
                    .HasColumnType("longtext");

                entity.HasOne(d => d.Site)
                    .WithMany(p => p.DcmsSiteFields)
                    .HasForeignKey(d => d.SiteId)
                    .HasConstraintName("FK_SITE_FIELDS_ID");
            });

            modelBuilder.Entity<DcmsSites>(entity =>
            {
                entity.ToTable("dcms_sites", "cms_dynamic");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ContentLastModified).HasColumnName("CONTENT_LAST_MODIFIED");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.Culture)
                    .HasColumnName("CULTURE")
                    .HasMaxLength(45)
                    .IsUnicode(false);

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.Property(e => e.HosteNames)
                    .HasColumnName("HOSTE_NAMES")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.IsDefualt)
                    .HasColumnName("IS_DEFUALT")
                    .HasColumnType("int(1)");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.SiteTypeId)
                    .HasColumnName("SITE_TYPE_ID")
                    .HasColumnType("int(11)");
            });

            modelBuilder.Entity<DcmsTags>(entity =>
            {
                entity.ToTable("dcms_tags", "cms_dynamic");

                entity.HasIndex(e => e.PageId)
                    .HasName("FK_PAGE_TAGS_ID_idx");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.CreationDate).HasColumnName("CREATION_DATE");

                entity.Property(e => e.ModificationDate).HasColumnName("MODIFICATION_DATE");

                entity.Property(e => e.ModifiedBy)
                    .HasColumnName("MODIFIED_BY")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.PageId)
                    .HasColumnName("PAGE_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Slug)
                    .HasColumnName("SLUG")
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Title)
                    .HasColumnName("TITLE")
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.HasOne(d => d.Page)
                    .WithMany(p => p.DcmsTags)
                    .HasForeignKey(d => d.PageId)
                    .HasConstraintName("FK_PAGE_TAGS_ID");
            });
        }
    }
}
