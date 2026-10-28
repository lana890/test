using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Manage;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Pages.Models;
using EskaCMS.Security.Models;
using EskaCMS.Sites.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface IUserWorkingHoursService
    {
        Task<bool> IsInWorkingHours(long UserId, long SiteId );


        Task<bool> upsert(UpsertWorkingHoursVm workingHours);
        
        Task<bool> Remove(long Id);

        Task<WorkingHoursVm> GetById(long Id);
        Task<PaginationResult<WorkingHoursVm>> GetAllWorkingHourse(PaginationFiltersVM Model);
    }
}
