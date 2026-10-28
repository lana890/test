using System.Linq;
using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity.ActivityLog;
using EskaCMS.Entities.Entity;
using EskaCMS.Module.Core.Data;
using EskaCommerce.Module.ActivityLog.Models;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.DataAccess.Data;

namespace EskaCommerce.Module.ActivityLog.Data
{
    public class ActivityRepository : Repository<Activity>, IActivityTypeRepository
    {
        private const int MostViewActivityTypeId = 1;

        public ActivityRepository(EskaDCMSDbContext context) : base(context)
        {
        }
        //public ActivityRepository(EskaCMSDbContext context) : base(context)
        //{
        //}

        public IQueryable<MostViewEntityDto> List()
        {
            return from a in DbSet
                join e in Context.Set<Entity>() on new { a.EntityId, a.EntityTypeId } equals new { e.EntityId, e.EntityTypeId }
                where a.ActivityTypeId == MostViewActivityTypeId
                group a by new {a.EntityId, a.EntityTypeId, e.Name, e.Slug}
                into g
                orderby g.Count() descending
                select new MostViewEntityDto
                {
                    EntityTypeId = g.Key.EntityTypeId,
                    EntityId = g.Key.EntityId,
                    Name = g.Key.Name,
                    Slug = g.Key.Slug,
                    ViewedCount = g.Count()
                };
        }
    }
}
