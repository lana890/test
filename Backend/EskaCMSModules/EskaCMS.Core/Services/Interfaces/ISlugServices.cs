using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Core.Services.Interface
{
    public interface ISlugServices
    {
        Task<long?> AddIfNotExsist(SlugManagementVm slugManagementVm);
        Task<bool> DeleteSlugInfo(long id);
        Task<string> GetSlugById(long? id);
        Task<long?> Upsert(SlugManagementVm slugManagementVm);
        Task<long> UpsertAndGetId(SlugManagementVm slugManagementVm);
        Task<SlugInfo> GetSlugDataBySlug(string Slug);
    }
}
