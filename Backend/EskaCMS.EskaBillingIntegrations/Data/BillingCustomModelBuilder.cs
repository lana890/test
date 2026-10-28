using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Entities;

using EskaCMS.Entities.Entity.Core;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Notifications.Data
{
    public class BillingCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {

            //modelBuilder.Entity<CustomerSims>().HasIndex(s => new { s.ICCID, s.MSISDN,s.IMSI });
            //modelBuilder.Entity<CustomerSims>().HasIndex(s => s.ICCID).IsUnique();
            //modelBuilder.Entity<CustomerSims>().HasIndex(s => s.MSISDN).IsUnique();
            //modelBuilder.Entity<CustomerSims>().HasIndex(s => s.IMSI).IsUnique();
            //modelBuilder.Entity<CustomerSims>().HasIndex(s => s.AccountId); 
            ////modelBuilder.Entity<CustomerSimsHistory>().HasIndex(s => s.CustomerSimsId).IsUnique();
            //modelBuilder.Entity<CustomerSimsHistory>().HasIndex(s => s.CustomerSimsId).IsUnique(false);


            //modelBuilder.Entity<BackgroundServiceSettings>().HasSeedData(

            // new BackgroundServiceSettings
            // {
            //     Id = "CustomerSimsBackgroundService",
            //     Name = "CustomerSimsBackgroundService",
            //     CreationDate = DateTimeOffset.Now,
            //     ModificationDate = DateTimeOffset.Now,
            //     CreatedById = 1,
            //     TimeSpan = TimeSpan.FromMinutes(60),
            //     IntervalType = IntervalType.Period,
            //     IsActive = true,
            //     PeriodType = BGPeriodType.ByTime,
            //     SiteId = 1
            // }

            // );


        }
    }
}

