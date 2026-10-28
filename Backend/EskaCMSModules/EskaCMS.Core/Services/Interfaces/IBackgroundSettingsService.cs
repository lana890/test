using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public interface IBackgroundSettingsService
    {
        Task<string> Create(BackgroundServiceSettingsCreate request);
        Task<string> Update(BackgroundServiceSettingsUpdate request);
        Task<SmartGridOutputVM<BackgroundServiceSettingsList>> GetList(ListRequestVM<BackgroundServiceSettingsListRequest> param);
        Task<List<BackgroundServiceSettingsList>> GetAll();
        Task<BackgroundServiceSettingsResponse> Get(string id);
    }
}