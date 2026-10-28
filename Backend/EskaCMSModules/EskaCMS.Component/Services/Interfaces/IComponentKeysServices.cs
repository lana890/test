using EskaCMS.Core.ViewModels.ComponentKeys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services.Interfaces
{
    public interface IComponentKeysServices
    {

        Task<bool> UpsertComponentKeys(List<UpsertComponentKeysVM> Model);

        Task<List<ComponentKeysVM>> GetComponentKeys(bool? status);
        Task<ComponentKeysVM> GetComponentKeysById(long id);
        Task<bool> DeleteComponentKey(long id);
        Task<bool> ChangeStatus(long PageId);
        Task<bool> ChangeStatusByPageTemplateId(long PageTemplateId);

    }
}
