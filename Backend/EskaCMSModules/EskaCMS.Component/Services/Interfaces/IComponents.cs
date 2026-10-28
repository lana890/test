
using EskaCMS.Components.Models;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Components;
using EskaCMS.Core.Models;
using System.Collections.Generic;

using System.Threading.Tasks;


namespace EskaCMS.Components.Services.Interfaces
{
    public interface IComponent
    {

        Task<IList<ComponentsCategoriesViewModel> >GetAllComponents(long siteId);

        Task<bool> SaveComponent(ComponentViewModel comp);

        Task<ComponentViewModel> GetComponentByID(long ID);
        Task<EskaCMS.Entities.Entity.Components.Component> DeleteComponent(long UserId, int ID);
      //  Task<bool> SaveComponentCategory(long siteId,string name, long iD, long UserId);
      //  Task<Category> DeleteComponentCategory(int userId, int id);
        Task<bool> SaveComponentSettings(string settings, int id);
        Task<EskaCMS.Entities.Entity.Components.Component> ShareComponentAsync(ComponentViewModel cat);
    }
}
