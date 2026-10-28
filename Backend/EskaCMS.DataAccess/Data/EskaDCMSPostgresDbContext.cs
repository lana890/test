using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.DataAccess.Data
{
    public class EskaDCMSMyPostgresDbContext : EskaDCMSDbContext
    {
         static EskaDCMSMyPostgresDbContext()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }
        public EskaDCMSMyPostgresDbContext(DbContextOptions<EskaDCMSMyPostgresDbContext> options) : base(options) {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {        // DateTime =timestamp without time zone 
                     var dateTimeProps = entityType.GetProperties().Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));
                foreach (var prop in dateTimeProps) 
                { 
                    modelBuilder.Entity(entityType.ClrType).Property(prop.Name).HasColumnType("timestamp without time zone");
                }
                // DateTimeOffset=timestamp with time zone
                var dtoProps = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?));
                foreach (var prop in dtoProps) 
                {
                    modelBuilder.Entity(entityType.Name)
                       .Property(prop.Name)
                       .HasColumnType("timestamp with time zone");
                }

           
                var decimalProperties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?));
                foreach (var property in decimalProperties)
                {
                    modelBuilder
                        .Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion<double>();
                }
            }
        }

     
    }
}
