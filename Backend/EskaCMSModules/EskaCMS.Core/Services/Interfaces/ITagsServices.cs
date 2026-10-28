using EskaCMS.Core.Areas.Core.ViewModels.Tags;
using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Services.Interfaces
{
    public interface ITagsServices
    {
        Task<List<GetTagsViewModel>> GetTagsByEntityType(SearchEntitesViewModel model);
        Task<List<GetAllTagsDto>> GetTags(SearchViewModel model);
        Task<bool> UpdateTag(TagsEntityViewModel model);
        Task<bool> CreateTag(TagsEntityViewModel model);
        Task<List<long>> FetchTagsByEntityAsync(List<long> EntityIds, EEntity entityType, long[]? TagsId = null);
        Task<Dictionary<long, GetTagsViewModel>> GetFirstTagByEntityIds(SearchEntitesViewModel model);
    }
}
