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
    public interface ITemplateAssetsServices
    {
        Task<bool> UpsertTemplateAssets(TemplateAssetsVM assetTemplate);

        Task<bool> ChangeStatus(long Id, EStatus status);
        Task<List<TemplateAssetsViewModel>> GetTemplatesAssets();
    }
}
