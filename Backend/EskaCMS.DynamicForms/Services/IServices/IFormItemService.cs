using EskaCMS.DynamicForms.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.DynamicForms.Services.IServices
{
    public interface IFormItemService
    {
        Task<List<FormItemVM>> GetAllFormItems(long SiteId);
        Task<List<FormItemForWebVM>> GetAllFormItemsForWeb(long SiteId, long cultureId, RequestFormItemWebViewModel model);
        Task<FormItemVM> GetFormItemsById(long FormItemId);
        Task<FormItemVM> UpsertFormItems(UpsertFormItemVM Model, long UserId, long SiteId);
        Task<bool> UpdateFormItemsStatus(long FormItemId, EStatus NewStatus, long UserId);
        Task<bool> UpdateFormItemsAdditionalInfo(long FormItemId, string additionalInfo);
        Task<List<FormItemWithClassificationsVM>> GetAllFormItemsWithClassification(long siteId, long cultureId);
        Task AssignFormItemPermissions(RequestFormItemRoleViewModel model);
        Task<FormItemAssignedRolesVM> AssignedFormItemRoles(long id);
    }
}
