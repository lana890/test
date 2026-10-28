using EskaCMS.Components.Models;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Components.Services.Interfaces
{
    public interface IPredefinedTemplatesServices
    {
        Task<PaginationResult<PredefinedTemplatesViewModel>> GetAllSettings(PredefinedTemplatesSearchTableParam<SearchPredefinedTemplatesViewModel> model);
        Task<PredefinedTemplatesViewModel> GetSettingById(int id);
        Task<bool> CreateSetting(PredefinedTemplatesViewModel setting);
        Task UpdateSetting(long id, PredefinedTemplatesViewModel setting);
        Task DeleteSetting(int id);
    }
}
