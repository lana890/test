using EskaCMS.Events.Models;
using EskaCMS.Infrastructure.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Events.Repositories.Interfaces
{
    public interface ILocationsRepository : IBaseRepository<EventLocationVM,long>
    {
      Task<List<EventLocationVM>> GetLocationsBySiteId();
      
      Task<bool> ToggleStatus(long LocationId, EStatus LocationStatus);

        Task<List<EventLocationVM>> GetLocationsForAdmins();

    }
}
