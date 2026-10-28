using EskaCMS.Sites.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Sites.Services.Interfaces
{
    public interface ISitePreferance
    {
        Task<bool> AddNewSitePreferences(CreateSitePreferanceViewModel SitePreferanceVM);
        Task<bool> UpdateSitePreferences(UpdateSitePreferanceVM SitePreferanceVM);
        Task<GetSitePreferanceVM> GetSitePreference();
        Task<bool> RemoveIfExist(long SiteId);
    }
}
