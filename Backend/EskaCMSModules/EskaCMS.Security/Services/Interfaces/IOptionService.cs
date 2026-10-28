
using EskaCMS.Security.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface IOptionService
    {
        Task<List<OptionLevelViewModel>> GetOptionsLevels(long CategoryTypeId, long? SiteId, List<long> UserOptionsIds = null);
        Task<bool> CreateAsync(AddOptionViewModel Option);
        Task<bool> EditOption(AddOptionViewModel Option);
        Task<List<Option>> GetBySiteAsync(long? SiteId);
        Task<bool> DeleteAsync(long OptionId);
    }
}
