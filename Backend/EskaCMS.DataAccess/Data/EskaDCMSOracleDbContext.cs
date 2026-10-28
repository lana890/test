using EskaCMS.Entities.Entity.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;

namespace EskaCMS.DataAccess.Data
{
    public class EskaDCMSOracleDbContext : EskaDCMSDbContext
    {
        private static readonly ValueConverter<bool, short> OracleBoolConverter =
            new ValueConverter<bool, short>(
                v => v ? (short)1 : (short)0,
                v => v == 1);

        private static readonly ValueConverter<bool?, short?> OracleNullableBoolConverter =
            new ValueConverter<bool?, short?>(
                v => v.HasValue ? (short?)(v.Value ? (short)1 : (short)0) : null,
                v => v.HasValue ? v.Value == 1 : (bool?)null);

        public EskaDCMSOracleDbContext(DbContextOptions<EskaDCMSOracleDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureOracleIdentityColumns(modelBuilder);

            ApplyOracleBooleanConventions(modelBuilder);
            ApplyOracleBooleanManualFix(modelBuilder);

            ApplyOracleStringConventions(modelBuilder);

            RemoveOracleIdentityDuplicateIndexes(modelBuilder);
            RemoveDuplicateOracleIndexes(modelBuilder);
            ApplyOracleUppercaseNaming(modelBuilder);
            EnsureUniqueOracleIndexNames(modelBuilder);
        }

        private static void ApplyOracleBooleanConventions(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var entity = modelBuilder.Entity(entityType.ClrType);

                var boolProps = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(bool))
                    .ToList();

                foreach (var prop in boolProps)
                {
                    entity.Property<bool>(prop.Name)
                        .HasConversion(OracleBoolConverter)
                        .HasPrecision(1);
                }

                var nullableBoolProps = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(bool?))
                    .ToList();

                foreach (var prop in nullableBoolProps)
                {
                    entity.Property<bool?>(prop.Name)
                        .HasConversion(OracleNullableBoolConverter)
                        .HasPrecision(1);
                }
            }
        }

        private static void ApplyOracleBooleanManualFix(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Site>(entity =>
            {
                entity.Property(e => e.IsADAuthRequired)
                    .HasConversion(OracleBoolConverter)
                    .HasPrecision(1);

                entity.Property(e => e.EnableTwoFactorAuthentication)
                    .HasConversion(OracleBoolConverter)
                    .HasPrecision(1);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.IsADAuthRequired)
                    .HasConversion(OracleBoolConverter)
                    .HasPrecision(1);
            });

            modelBuilder.Entity<UsersSites>(entity =>
            {
                entity.Property(e => e.IsDefault)
                    .HasConversion(OracleBoolConverter)
                    .HasPrecision(1);
            });

            modelBuilder.Entity<SiteSettings>(entity =>
            {
                entity.Property(e => e.IsVisibleInCommonSettingPage)
                    .HasConversion(OracleBoolConverter)
                    .HasPrecision(1);
            });

            modelBuilder.Entity<SitesHostNames>(entity =>
            {
                entity.Property(e => e.IsActive)
                    .HasConversion(OracleBoolConverter)
                    .HasPrecision(1);
            });
        }

        private static void ApplyOracleStringConventions(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var entity = modelBuilder.Entity(entityType.ClrType);

                var stringProps = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(string))
                    .ToList();

                foreach (var prop in stringProps)
                {
                    var propertyName = prop.Name;

                    var currentColumnType = prop.GetColumnType();
                    if (!string.IsNullOrWhiteSpace(currentColumnType))
                    {
                        continue;
                    }

                    var currentMaxLength = prop.GetMaxLength();
                    if (currentMaxLength.HasValue)
                    {
                        continue;
                    }

                    var isPrimaryKey = entityType.FindPrimaryKey()?
                        .Properties.Any(p => p.Name == propertyName) ?? false;

                    var isForeignKey = entityType.GetForeignKeys()
                        .Any(fk => fk.Properties.Any(p => p.Name == propertyName));

                    var hasIndex = entityType.GetIndexes()
                        .Any(i => i.Properties.Any(p => p.Name == propertyName));

                    if (isPrimaryKey || isForeignKey || hasIndex)
                    {
                        entity.Property(propertyName)
                            .HasMaxLength(450);

                        continue;
                    }

                    var boundedLength = GetOracleBoundedStringLength(propertyName);

                    if (boundedLength.HasValue)
                    {
                        entity.Property(propertyName)
                            .HasMaxLength(boundedLength.Value);

                        continue;
                    }

                    // Important:
                    // Do not use NCLOB globally.
                    // Use NVARCHAR2 with length to avoid ORA-00932.
                    entity.Property(propertyName)
                        .HasMaxLength(2000);
                }
            }
        }

        private static void ConfigureOracleIdentityColumns(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(x => x.UserName).HasMaxLength(256);
                entity.Property(x => x.NormalizedUserName).HasMaxLength(256);
                entity.Property(x => x.Email).HasMaxLength(256);
                entity.Property(x => x.NormalizedEmail).HasMaxLength(256);
                entity.Property(x => x.PasswordHash).HasMaxLength(512);
                entity.Property(x => x.SecurityStamp).HasMaxLength(256);
                entity.Property(x => x.ConcurrencyStamp).HasMaxLength(256);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(256);
                entity.Property(x => x.NormalizedName).HasMaxLength(256);
                entity.Property(x => x.ConcurrencyStamp).HasMaxLength(256);
            });

            modelBuilder.Entity<CurrentLoggedInUsers>(entity =>
            {
                entity.Property(x => x.ApiToken).HasMaxLength(2000);
                entity.Property(x => x.RefreshTokenHash).HasMaxLength(450);
                entity.Property(x => x.ClientIP).HasMaxLength(128);
            });

            modelBuilder.Entity<UserOTP>(entity =>
            {
                entity.Property(x => x.Username).HasMaxLength(256);
                entity.Property(x => x.ProviderValue).HasMaxLength(450);
                entity.Property(x => x.Purpose).HasMaxLength(450);
                entity.Property(x => x.Value).HasMaxLength(450);
            });

            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<long>>(entity =>
            {
                entity.Property(x => x.ClaimType).HasMaxLength(450);
                entity.Property(x => x.ClaimValue).HasMaxLength(2000);
            });

            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<long>>(entity =>
            {
                entity.Property(x => x.ClaimType).HasMaxLength(450);
                entity.Property(x => x.ClaimValue).HasMaxLength(2000);
            });

            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<long>>(entity =>
            {
                entity.Property(x => x.LoginProvider).HasMaxLength(256);
                entity.Property(x => x.Name).HasMaxLength(256);
                entity.Property(x => x.Value).HasMaxLength(2000);
            });

            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<long>>(entity =>
            {
                entity.Property(x => x.LoginProvider).HasMaxLength(256);
                entity.Property(x => x.ProviderKey).HasMaxLength(256);
                entity.Property(x => x.ProviderDisplayName).HasMaxLength(256);
            });
        }

        private static void RemoveOracleIdentityDuplicateIndexes(ModelBuilder modelBuilder)
        {
            var userEntity = modelBuilder.Model.FindEntityType(typeof(User));

            if (userEntity == null)
            {
                return;
            }

            var redundantIndexes = userEntity.GetIndexes()
                .Where(index =>
                    index.GetDatabaseName() == "NormalizedEmailidx" ||
                    index.GetDatabaseName() == "NormalizedUserNameidx")
                .ToList();

            foreach (var redundantIndex in redundantIndexes)
            {
                userEntity.RemoveIndex(redundantIndex);
            }
        }

        private static void RemoveDuplicateOracleIndexes(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var duplicateIndexes = entityType.GetIndexes()
                    .GroupBy(GetIndexSignature)
                    .SelectMany(g => g.Skip(1))
                    .ToList();

                foreach (var duplicateIndex in duplicateIndexes)
                {
                    entityType.RemoveIndex(duplicateIndex);
                }
            }
        }

        private static string GetIndexSignature(IReadOnlyIndex index)
        {
            var columns = string.Join("|", index.Properties.Select(p => p.Name));
            return columns;
        }

        private static int? GetOracleBoundedStringLength(string propertyName)
        {
            if (propertyName.EndsWith("Hash", StringComparison.OrdinalIgnoreCase))
            {
                return 512;
            }

            if (propertyName.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("ClaimValue", StringComparison.OrdinalIgnoreCase))
            {
                return 2000;
            }

            if (propertyName.Contains("IPAddress", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("IPAdress", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("IPAddres", StringComparison.OrdinalIgnoreCase))
            {
                return 128;
            }

            if (propertyName.EndsWith("IP", StringComparison.OrdinalIgnoreCase) ||
                propertyName.EndsWith("Ip", StringComparison.OrdinalIgnoreCase))
            {
                return 128;
            }

            if (propertyName.EndsWith("Url", StringComparison.OrdinalIgnoreCase) ||
                propertyName.EndsWith("URL", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("Url", StringComparison.OrdinalIgnoreCase))
            {
                return 2000;
            }

            if (propertyName.EndsWith("Slug", StringComparison.OrdinalIgnoreCase) ||
                propertyName.EndsWith("Route", StringComparison.OrdinalIgnoreCase))
            {
                return 1000;
            }

            if (propertyName.Contains("Description", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("ShortDescription", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("Value", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("Css", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("Html", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("Json", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("Body", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("Content", StringComparison.OrdinalIgnoreCase))
            {
                return 2000;
            }

            if (propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
                propertyName.EndsWith("Code", StringComparison.OrdinalIgnoreCase) ||
                propertyName.EndsWith("Type", StringComparison.OrdinalIgnoreCase) ||
                propertyName.EndsWith("Name", StringComparison.OrdinalIgnoreCase) ||
                propertyName.EndsWith("Key", StringComparison.OrdinalIgnoreCase) ||
                propertyName.EndsWith("Email", StringComparison.OrdinalIgnoreCase) ||
                propertyName.EndsWith("UserName", StringComparison.OrdinalIgnoreCase) ||
                propertyName.EndsWith("Stamp", StringComparison.OrdinalIgnoreCase) ||
                propertyName.EndsWith("Provider", StringComparison.OrdinalIgnoreCase))
            {
                return 450;
            }

            return null;
        }

        private static void ApplyOracleUppercaseNaming(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var entity = modelBuilder.Entity(entityType.ClrType);
                var tableName = entityType.GetTableName();

                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    entityType.SetTableName(tableName.ToUpperInvariant());
                }

                foreach (var property in entityType.GetProperties())
                {
                    if (!string.IsNullOrWhiteSpace(property.Name))
                    {
                        entity.Property(property.Name)
                            .HasColumnName(property.Name.ToUpperInvariant());
                    }
                }

                var primaryKey = entityType.FindPrimaryKey();

                if (primaryKey != null && !string.IsNullOrWhiteSpace(primaryKey.GetName()))
                {
                    primaryKey.SetName(primaryKey.GetName()!.ToUpperInvariant());
                }

                foreach (var key in entityType.GetKeys())
                {
                    if (!string.IsNullOrWhiteSpace(key.GetName()))
                    {
                        key.SetName(key.GetName()!.ToUpperInvariant());
                    }
                }

                foreach (var foreignKey in entityType.GetForeignKeys())
                {
                    if (!string.IsNullOrWhiteSpace(foreignKey.GetConstraintName()))
                    {
                        foreignKey.SetConstraintName(foreignKey.GetConstraintName()!.ToUpperInvariant());
                    }
                }

                foreach (var index in entityType.GetIndexes())
                {
                    var databaseName = index.GetDatabaseName();

                    if (!string.IsNullOrWhiteSpace(databaseName))
                    {
                        index.SetDatabaseName(databaseName.ToUpperInvariant());
                    }
                }
            }
        }

        private static void EnsureUniqueOracleIndexNames(ModelBuilder modelBuilder)
        {
            var duplicateIndexGroups = modelBuilder.Model.GetEntityTypes()
                .SelectMany(entityType => entityType.GetIndexes())
                .Where(index => !string.IsNullOrWhiteSpace(index.GetDatabaseName()))
                .GroupBy(index => index.GetDatabaseName()!, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1);

            foreach (var group in duplicateIndexGroups)
            {
                foreach (var index in group)
                {
                    var tableName = index.DeclaringEntityType.GetTableName();

                    if (string.IsNullOrWhiteSpace(tableName))
                    {
                        continue;
                    }

                    index.SetDatabaseName($"{tableName}_{group.Key}".ToUpperInvariant());
                }
            }
        }
    }
}