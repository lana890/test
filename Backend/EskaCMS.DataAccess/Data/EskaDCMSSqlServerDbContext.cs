using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;

namespace EskaCMS.DataAccess.Data
{
    public class EskaDCMSSqlServerDbContext : EskaDCMSDbContext
    {
        public EskaDCMSSqlServerDbContext(
            DbContextOptions<EskaDCMSSqlServerDbContext> options
        ) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // GLOBAL CONVERTERS (FIXES ALL CAST ISSUES)
            // =====================================================

            // 🔹 smallint <-> bool (MySQL -> SQL Server fix)
            var smallIntBoolConverter = new ValueConverter<bool, short>(
                v => v ? (short)1 : (short)0,
                v => v == 1                     
            );

            // 🔹 datetime <-> DateTimeOffset
            var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, DateTime>(
                v => v.UtcDateTime,                       // DateTimeOffset -> DateTime
                v => new DateTimeOffset(v, TimeSpan.Zero) // DateTime -> DateTimeOffset
            );

            // =====================================================
            // APPLY CONFIGURATIONS GLOBALLY
            // =====================================================

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var entity = modelBuilder.Entity(entityType.ClrType);

                // -------------------------------------------------
                // bool -> smallint
                var boolProps = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(bool));

                foreach (var prop in boolProps)
                {
                    entity.Property(prop.Name)
                          .HasConversion(smallIntBoolConverter)
                          .HasColumnType("smallint");
                }

                // -------------------------------------------------
                // DateTime -> datetime2
                var dateTimeProps = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));

                foreach (var prop in dateTimeProps)
                {
                    entity.Property(prop.Name)
                          .HasColumnType("datetime2");
                }

                // -------------------------------------------------
                // DateTimeOffset -> datetime2 + converter
                var dtoProps = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?));

                foreach (var prop in dtoProps)
                {
                    entity.Property(prop.Name)
                          .HasConversion(dateTimeOffsetConverter)
                          .HasColumnType("datetime2");
                }

                // -------------------------------------------------
                // decimal -> decimal(18,2)
                var decimalProps = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

                foreach (var prop in decimalProps)
                {
                    entity.Property(prop.Name)
                          .HasColumnType("decimal(18,2)");
                }

                // -------------------------------------------------
                // string configuration
                var stringProps = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(string) && p.GetMaxLength() == null);

                foreach (var prop in stringProps)
                {
                    var hasIndex = entityType.GetIndexes()
                        .Any(i => i.Properties.Any(p => p.Name == prop.Name));

                    var isPrimaryKey = entityType.FindPrimaryKey()?
                        .Properties.Any(p => p.Name == prop.Name) ?? false;

                    if (isPrimaryKey || hasIndex)
                    {
                        entity.Property(prop.Name)
                              .HasMaxLength(450);
                    }
                    else
                    {
                        entity.Property(prop.Name)
                              .HasColumnType("nvarchar(max)");
                    }
                }
            }
        }
    }
}
