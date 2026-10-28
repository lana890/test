using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity.Core;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using EskaCommerce.Infrastructure.Models;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Catalog;
using Microsoft.EntityFrameworkCore.Migrations;
using EskaCMS.Entities.Entity.Forms;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Reflection.Emit;
using Microsoft.Extensions.Options;
using EskaCMS.Infrastructure.Enums;

namespace EskaCMS.DataAccess.Data
{
    public class EskaDCMSDbContext : IdentityDbContext<User, Role, long, IdentityUserClaim<long>, UserRole, IdentityUserLogin<long>, IdentityRoleClaim<long>, IdentityUserToken<long>>
    {
        public EskaDCMSDbContext(DbContextOptions options) : base(options)
        {
        }

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<IdentityPasskeyData>();

            GlobalConfiguration.CurrentDatabaseProvider = ResolveProvider();

            List<Type> typeToRegisters = new List<Type>();
            foreach (var module in GlobalConfiguration.Modules)
            {
                typeToRegisters.AddRange(module.Assembly.DefinedTypes.Select(t => t.AsType()));
            }

            RegisterEntities(modelBuilder, typeToRegisters);
            RegisterConvention(modelBuilder);
            base.OnModelCreating(modelBuilder);
            RegisterCustomMappings(modelBuilder, typeToRegisters);
            ApplyProviderChanges(modelBuilder);
        }

        private EProvider? ResolveProvider()
        {
            if (string.IsNullOrWhiteSpace(Database.ProviderName))
                return null;

            if (Database.ProviderName.Contains("Oracle", StringComparison.OrdinalIgnoreCase))
                return EProvider.Oracle;

            if (Database.ProviderName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
                return EProvider.PostgreSQL;

            if (Database.ProviderName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
                return EProvider.SqlServer;

            if (Database.ProviderName.Contains("MySql", StringComparison.OrdinalIgnoreCase))
                return EProvider.MySql;

            return null;
        }

        public void ApplyProviderChanges(ModelBuilder modelBuilder)
        {
            // Apply decimal precision to all providers
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18,2)");
            }

            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    var properties = entityType.ClrType.GetProperties()
                        .Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));

                    foreach (var property in properties)
                    {
                        modelBuilder
                            .Entity(entityType.Name)
                            .Property(property.Name)
                            .HasConversion(new DateTimeOffsetToBinaryConverter());
                    }

                    var decimalProperties = entityType.ClrType.GetProperties()
                        .Where(p => p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?));

                    foreach (var property in decimalProperties)
                    {
                        modelBuilder
                            .Entity(entityType.Name)
                            .Property(property.Name)
                            .HasConversion<double>();
                    }
                }
            }
            else if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL" || Database.IsNpgsql())
            {
                ApplyPostgresConfiguration(modelBuilder);
            }
            else if (Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                ApplySqlServerConfiguration(modelBuilder);
            }
            else if (!string.IsNullOrWhiteSpace(Database.ProviderName) &&
                     Database.ProviderName.Contains("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                ApplyOracleConfiguration(modelBuilder);
            }
            // MySQL/Pomelo works correctly with default EF Core type mappings.
            // No special overrides needed for datetime(6) or decimal(18,2) —
            // Pomelo handles these automatically via MySqlServerVersion(8.0.39).
        }

        // This database stores every SQL Server boolean as smallint (0/1), so bool
        // properties need a value converter to read/write those columns. Without it EF
        // maps bool to the native 'bit' type and throws "Unable to cast Int16 to Boolean".
        private static readonly ValueConverter<bool, short> SqlServerBoolConverter =
            new ValueConverter<bool, short>(
                v => v ? (short)1 : (short)0,
                v => v == 1);

        // SQL Server cannot map a DateTimeOffset property to a datetime2 column on its own.
        // Store the UTC instant as datetime2 via this converter (offset is normalized to UTC).
        private static readonly ValueConverter<DateTimeOffset, DateTime> SqlServerDateTimeOffsetConverter =
            new ValueConverter<DateTimeOffset, DateTime>(
                v => v.UtcDateTime,                        // DateTimeOffset -> DateTime (UTC)
                v => new DateTimeOffset(v, TimeSpan.Zero)); // DateTime -> DateTimeOffset (UTC)

        // Runtime SQL Server type mappings. These MUST stay in sync with the conversions in
        // EskaDCMSSqlServerDbContext (used at design time) so that what migrations write to
        // the database is what the running app reads back. Only bool and DateTimeOffset need
        // a converter; every other CLR type maps to a native, matching SQL Server column.
        private void ApplySqlServerConfiguration(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var entity = modelBuilder.Entity(entityType.Name);

                // bool -> smallint (matches the existing smallint columns in the database)
                foreach (var property in entityType.GetProperties()
                             .Where(p => p.ClrType == typeof(bool))
                             .ToList())
                {
                    entity.Property(property.Name)
                        .HasConversion(SqlServerBoolConverter)
                        .HasColumnType("smallint");
                }

                // DateTimeOffset -> datetime2 (requires the converter above)
                foreach (var property in entityType.GetProperties()
                             .Where(p => p.ClrType == typeof(DateTimeOffset) ||
                                         p.ClrType == typeof(DateTimeOffset?))
                             .ToList())
                {
                    entity.Property(property.Name)
                        .HasConversion(SqlServerDateTimeOffsetConverter)
                        .HasColumnType("datetime2");
                }
            }
        }

        private void ApplyPostgresConfiguration(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var dateTimeProperties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTime) ||
                                p.PropertyType == typeof(DateTime?));

                foreach (var property in dateTimeProperties)
                {
                    modelBuilder.Entity(entityType.Name)
                        .Property(property.Name)
                        .HasColumnType("timestamp with time zone");
                }
            }

            var utcConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
                v => v.ToUniversalTime(),
                v => v
            );

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTimeOffset) ||
                                p.PropertyType == typeof(DateTimeOffset?));

                foreach (var property in properties)
                {
                    modelBuilder.Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion(utcConverter);
                }
            }
        }

        private void ApplyOracleConfiguration(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var entity = modelBuilder.Entity(entityType.ClrType);

                var dateTimeProps = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));

                foreach (var prop in dateTimeProps)
                {
                    entity.Property(prop.Name).HasColumnType("TIMESTAMP(7)");
                }

                var dtoProps = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?));

                foreach (var prop in dtoProps)
                {
                    entity.Property(prop.Name).HasColumnType("TIMESTAMP(7) WITH TIME ZONE");
                }

                var decimalProps = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

                foreach (var prop in decimalProps)
                {
                    entity.Property(prop.Name).HasColumnType("NUMBER(18,2)");
                }
            }
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

        private static void RegisterConvention(ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                if (entity.ClrType.Namespace != null)
                {
                    var tableName = string.Empty;
                    var nameParts = entity.ClrType.Namespace.Split('.');

                    tableName = nameParts.Count() >= 4
                        ? string.Concat(nameParts[3], "_", entity.ClrType.Name)
                        : string.Concat(nameParts[1], "_", entity.ClrType.Name);

                    modelBuilder.Entity(entity.Name).ToTable(tableName);
                }
            }

            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        private static void RegisterEntities(ModelBuilder modelBuilder, IEnumerable<Type> typeToRegisters)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.GetForeignKeys()
                    .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade)
                    .ToList()
                    .ForEach(fk => fk.DeleteBehavior = DeleteBehavior.Restrict);
            }

            var entityTypes = typeToRegisters.Where(x =>
                (x.GetTypeInfo().IsSubclassOf(typeof(EntityBase)) ||
                 x.GetTypeInfo().IsSubclassOf(typeof(EntityBaseWithCustomId)))
                && !x.GetTypeInfo().IsAbstract);

            foreach (var type in entityTypes)
            {
                modelBuilder.Entity(type);
            }
        }

        private static void RegisterCustomMappings(ModelBuilder modelBuilder, IEnumerable<Type> typeToRegisters)
        {
            var customModelBuilderTypes = typeToRegisters.Where(x => typeof(ICustomModelBuilder).IsAssignableFrom(x));
            foreach (var builderType in customModelBuilderTypes)
            {
                if (builderType != null && builderType != typeof(ICustomModelBuilder))
                {
                    var builder = (ICustomModelBuilder)Activator.CreateInstance(builderType);
                    builder.Build(modelBuilder);
                }
            }
        }
    }
}