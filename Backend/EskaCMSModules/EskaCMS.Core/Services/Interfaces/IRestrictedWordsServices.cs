using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Services.Interfaces
{
    public interface IRestrictedWordsServices
    {
        Task<long> UpsertRestrictedPasswords(RestrictedWordVM model);
        Task<bool> UpdateRestrictedWordStatus(long Id, EStatus newStatus);
        Task<bool> IsRestrictedWord(string content,long? SiteId=null);
        Task<PaginationResult<AllRestrictedWordVM>> AllRestrictedWords(SmartSearchTableParam<GetAllRestrictedWordFilter> model);


    }
}
