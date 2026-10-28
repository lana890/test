using System.Linq;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Module.ActivityLog.Models;

namespace EskaCommerce.Module.ActivityLog.Data
{
    public interface IActivityTypeRepository : IRepository<EskaCMS.Entities.Entity.ActivityLog.Activity>
    {
        IQueryable<MostViewEntityDto> List();
    }
}
