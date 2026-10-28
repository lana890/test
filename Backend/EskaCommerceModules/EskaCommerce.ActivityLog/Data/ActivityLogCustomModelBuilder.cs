using Microsoft.EntityFrameworkCore;
using EskaCommerce.Module.ActivityLog.Models;
using EskaCMS.Infrastructure.Data;

using EskaCMS.Entities.Entity.ActivityLog;
namespace EskaCommerce.Module.ActivityLog.Data
{
    public class ActivityLogCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Activity>().HasIndex(x => x.ActivityTypeId);

            //modelBuilder.Entity<ActivityType>().HasSeedData(new ActivityType(1) { Name = "EntityView" });
        }
    }
}

