using System.Collections;
using System.Reflection;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Notifications;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Infrastructure;
using EskaCommerce.Infrastructure.Models;
using UserClaim = Microsoft.AspNetCore.Identity.IdentityUserClaim<long>;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EskaCMS.OTP.Data
{
    public class OtpDbContext : DbContext
    {
        static OtpDbContext()
        {
            // Same switch EskaDCMSMyPostgresDbContext sets.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        public OtpDbContext(DbContextOptions<OtpDbContext> options) : base(options)
        {
        }

        // Tables used by the OTP flow.
        private static readonly Type[] MappedEntityTypes =
        {
            typeof(UserOTP),
            typeof(User),
            typeof(UsersSites),
            typeof(SiteSettings),
            typeof(EmailsSettings),
            typeof(EmailTemplates),
            typeof(DCMSLog),
            typeof(ApiLog),
            typeof(CypherParam),
            typeof(CurrentLoggedInUsers),
            typeof(BackgroundServiceSettings),
            typeof(NotificationScheme),
            typeof(NotificationDetail),
            typeof(UserNotification),
            typeof(Culture),
            // Tables used by Account/login and Guest/CreateUser.
            typeof(Role),
            typeof(UserRole),
            typeof(CustomerGroup),
            typeof(CustomerGroupUser),
            typeof(UserOptions),
            typeof(Site),
            typeof(SitesHostNames),
            typeof(UserLoginTransactions),
            typeof(UserClaim),
        };

        private static readonly HashSet<(Type, string)> KeptNavigations = new()
        {
            (typeof(UserOTP), nameof(EskaCMS.Entities.Entity.Core.UserOTP.User)),
            (typeof(User), nameof(User.UserSites)),
            (typeof(User), nameof(User.UserOTP)),
            (typeof(User), nameof(User.UserRoles)),
            (typeof(User), nameof(User.CustomerGroups)),
            (typeof(User), nameof(User.UserOptions)),
            (typeof(UsersSites), nameof(UsersSites.User)),
            (typeof(UserRole), nameof(UserRole.User)),
            (typeof(UserRole), nameof(UserRole.Role)),
            (typeof(CustomerGroupUser), nameof(CustomerGroupUser.User)),
            (typeof(CustomerGroupUser), nameof(CustomerGroupUser.CustomerGroup)),
            (typeof(UserOptions), nameof(UserOptions.User)),
        };

        private static readonly Dictionary<Type, string> TableNameOverrides = new()
        {
            [typeof(SiteSettings)] = "Core_AppSetting",
            [typeof(UsersSites)] = "Core_UsersSites",
            [typeof(EmailTemplates)] = "Core_EmailTemplates",
            [typeof(User)] = "Core_User",
            [typeof(CurrentLoggedInUsers)] = "Core_CurrentLoggedInUsers",
            [typeof(BackgroundServiceSettings)] = "Core_BackgroundServiceSettings",
            [typeof(CypherParam)] = "Security_CypherParam",
            [typeof(UserNotification)] = "Notifications_UserNotification",
            [typeof(Culture)] = "Culture",
            [typeof(UserClaim)] = "Core_UserClaim",
        };

        public DbSet<UserOTP> UserOTP { get; set; }
        public DbSet<User> Users { get; set; }

        private static readonly ValueConverter<bool, short> SmallIntBoolConverter =
            new ValueConverter<bool, short>(
                v => v ? (short)1 : (short)0,
                v => v == 1);

        private static readonly ValueConverter<DateTimeOffset, DateTime> SqlServerDateTimeOffsetConverter =
            new ValueConverter<DateTimeOffset, DateTime>(
                v => v.UtcDateTime,
                v => new DateTimeOffset(v, TimeSpan.Zero));

        // Same as EskaDCMSDbContext: validate data annotations before saving.
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ValidateEntities();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ValidateEntities();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void ValidateEntities()
        {
            var modifiedEntries = ChangeTracker.Entries()
                .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified);

            foreach (var entity in modifiedEntries)
            {
                if (entity.Entity is ValidatableObject validatableObject)
                {
                    var validationResults = validatableObject.Validate();
                    if (validationResults.Any())
                    {
                        throw new ValidationException(entity.Entity.GetType(), validationResults);
                    }
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. Keep every other entity class of EskaCMS.Entities out of this model.
            var mapped = new HashSet<Type>(MappedEntityTypes);
            foreach (var type in typeof(User).Assembly.GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && !mapped.Contains(type)
                    && type.Namespace != null && type.Namespace.StartsWith("EskaCMS.Entities.Entity"))
                {
                    modelBuilder.Ignore(type);
                }
            }

            // 2. Register the OTP tables with only the navigations OTP needs.
            foreach (var type in MappedEntityTypes)
            {
                var entity = modelBuilder.Entity(type);

                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (IsNavigationCandidate(property.PropertyType) && !KeptNavigations.Contains((type, property.Name)))
                    {
                        entity.Ignore(property.Name);
                    }
                }

                entity.ToTable(TableNameOverrides.TryGetValue(type, out var tableName) ? tableName : ConventionTableName(type));
            }

            // Keys / tokens configured by CoreCustomModelBuilder and ASP.NET Identity in the main system.
            modelBuilder.Entity<SiteSettings>().HasKey(x => new { x.Id, x.SiteId });
            modelBuilder.Entity<EmailTemplates>().HasKey(x => new { x.Id, x.SiteId });
            modelBuilder.Entity<BackgroundServiceSettings>().HasKey(x => new { x.SiteId, x.Id });
            modelBuilder.Entity<User>().HasKey(x => x.Id);
            modelBuilder.Entity<User>().Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

            modelBuilder.Entity<User>().HasMany(x => x.UserSites).WithOne(x => x.User).HasForeignKey(x => x.UserId);
            modelBuilder.Entity<User>().HasMany(x => x.UserOTP).WithOne(x => x.User).HasForeignKey(x => x.UserId);
            modelBuilder.Entity<User>().HasMany(x => x.UserOptions).WithOne(x => x.User).HasForeignKey(x => x.UserId);

            modelBuilder.Entity<Role>().HasKey(x => x.Id);
            modelBuilder.Entity<Role>().Property(x => x.ConcurrencyStamp).IsConcurrencyToken();
            modelBuilder.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });
            modelBuilder.Entity<User>().HasMany(x => x.UserRoles).WithOne(x => x.User).HasForeignKey(x => x.UserId);
            modelBuilder.Entity<UserRole>().HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);

            modelBuilder.Entity<CustomerGroupUser>().HasKey(x => new { x.UserId, x.CustomerGroupId });
            modelBuilder.Entity<User>().HasMany(x => x.CustomerGroups).WithOne(x => x.User).HasForeignKey(x => x.UserId);
            modelBuilder.Entity<CustomerGroupUser>().HasOne(x => x.CustomerGroup).WithMany().HasForeignKey(x => x.CustomerGroupId);

            modelBuilder.Entity<UserClaim>().HasKey(x => x.Id);

            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            ApplyProviderChanges(modelBuilder);
        }

        // EskaDCMSDbContext.RegisterConvention
        private static string ConventionTableName(Type type)
        {
            var nameParts = type.Namespace.Split('.');
            return nameParts.Length >= 4
                ? string.Concat(nameParts[3], "_", type.Name)
                : string.Concat(nameParts[1], "_", type.Name);
        }

        // A property EF would treat as a navigation: a class (other than string / byte[]) or a collection of classes.
        private static bool IsNavigationCandidate(Type type)
        {
            if (type == typeof(string) || type == typeof(byte[]) || type.IsValueType)
                return false;

            if (typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
            {
                var element = type.GetGenericArguments()[0];
                return element != typeof(string) && !element.IsValueType;
            }

            return type.IsClass || type.IsInterface;
        }

        // Runtime type mappings identical to EskaDCMSDbContext + its provider-specific subclasses.
        private void ApplyProviderChanges(ModelBuilder modelBuilder)
        {
            var providerName = Database.ProviderName ?? string.Empty;

            if (providerName == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                ApplySqlServerConfiguration(modelBuilder);
            }
            else if (providerName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                ApplyPostgresConfiguration(modelBuilder);
            }
            else if (providerName.Contains("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                ApplyOracleConfiguration(modelBuilder);
            }
            // MySQL/Pomelo: default mappings, same as the main system.
        }

        private static void ApplySqlServerConfiguration(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var entity = modelBuilder.Entity(entityType.Name);

                foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(bool)).ToList())
                {
                    entity.Property(property.Name)
                        .HasConversion(SmallIntBoolConverter)
                        .HasColumnType("smallint");
                }

                foreach (var property in entityType.GetProperties()
                             .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?))
                             .ToList())
                {
                    entity.Property(property.Name)
                        .HasConversion(SqlServerDateTimeOffsetConverter)
                        .HasColumnType("datetime2");
                }
            }
        }

        private static void ApplyPostgresConfiguration(ModelBuilder modelBuilder)
        {
            var utcConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
                v => v.ToUniversalTime(),
                v => v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));

                foreach (var property in properties)
                {
                    if (entityType.FindProperty(property.Name) == null)
                        continue;

                    modelBuilder.Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion(utcConverter)
                        .HasColumnType("timestamp with time zone");
                }
            }
        }

        private static void ApplyOracleConfiguration(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var entity = modelBuilder.Entity(entityType.ClrType);

                foreach (var prop in entityType.GetProperties()
                             .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?))
                             .ToList())
                {
                    entity.Property(prop.Name).HasColumnType("TIMESTAMP(7) WITH TIME ZONE");
                }

                foreach (var prop in entityType.GetProperties().Where(p => p.ClrType == typeof(bool)).ToList())
                {
                    entity.Property<bool>(prop.Name)
                        .HasConversion(SmallIntBoolConverter)
                        .HasPrecision(1);
                }

                // EskaDCMSOracleDbContext.ApplyOracleUppercaseNaming
                var tableName = entityType.GetTableName();
                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    entityType.SetTableName(tableName.ToUpperInvariant());
                }

                foreach (var property in entityType.GetProperties())
                {
                    entity.Property(property.Name).HasColumnName(property.Name.ToUpperInvariant());
                }
            }
        }
    }
}
