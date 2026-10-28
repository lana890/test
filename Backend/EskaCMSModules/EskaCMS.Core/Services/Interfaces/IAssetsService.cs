
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Pagination;
using System.Collections.Generic;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Core.Models.TokenVM;

namespace EskaCMS.Core.Services
{
    public interface IAssetsService
    {
        Task<PaginationResult<GetAssetsVM>> GetAssets(GetAssertVM model);
        Task<bool> ChangeAssetsStatus(long AssetId, EStatus NewStatus);
        Task<bool> UpsertAssets(UpsertAssetVM Asset);
        Task<GetAssetViewModel> GetAssetsById(long id);
    }
}
