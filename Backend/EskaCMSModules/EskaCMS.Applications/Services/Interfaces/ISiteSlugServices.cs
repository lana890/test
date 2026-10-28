using EskaCMS.Sites.Models;
using EskaCMS.Sites.Services.Model;
using EskaCMS.UserDashboard.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Sites.Services.Interfaces
{
    public interface ISiteSlugServices
    {
        Task<SlugInfoSettings> GetSlugSettingsForWebSp(UserNavigationVm userNavigationVm);

        Task<bool> CanAccessSlug(long TargetId, SlugsTypes SlugType);

        Task<SlugInfoSettings> GetSettingsByPreviewCodeForWebSp(string? code);

    }
}
