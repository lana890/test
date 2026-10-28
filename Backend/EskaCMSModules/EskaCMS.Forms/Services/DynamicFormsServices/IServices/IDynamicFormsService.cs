using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.DynamicForms.ViewModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Forms.Model.FormsViewModel;

namespace EskaCMS.DynamicForms.Services.IServices
{
    public interface IDynamicFormsService
    {
        Task<string> Submitted(string FormData, DynamicFormsSettingsVM model, List<DynamicFormsWebVM> Data, long FormItemId, long SiteId, long UserId, long PageId, string ComponentSettingsId, string Culture, IFormFileCollection Files);
        Task<Classifications> GetClassificationById(long ClassId);
        Task<List<Classifications>> GetClassifications(long FormItemId);
        Task<Classifications> UpsertClassification(ClassificationVM Model, long UserId, long SiteId);
        Task<Classifications> UpdateClassificationStatus(long ClassId, EStatus NewStatus, long UserId);
        Task<bool> UpdateCriteriaStatus(long CriteriaId, EStatus NewStatus, long UserId);

        Task<Criteria> GetClassificationOperation(long ClassificationId);

        Task<bool> UpsertOperationCriateria(CriteriaVM Model, long UserId, long SiteId);

        Task<ClassificationSearchVM> GetPaginatedClassifications(long FormItemId, SearchTableParam<string> Data);

        Task<FormItemVM> DuplicateFormItem(long FormItemId);
        Task<PaginationResult<Classifications>> GetGeneralClassifications(SmartSearchTableParam<AllGeneralFormField> model);
        Task<PaginationResult<Criteria>> GetGeneralCriteria(SmartSearchTableParam<AllGeneralFormField> model);
        Task<bool> UpsertGeneralCriteria(CriteriaVM Model);

        Task<string> Submitted(List<DynamicFormsWebVM> Data, long FormItemId, long SiteId, long UserId, string Culture, IFormFileCollection? Files);
        Task<PaginationResult<SubmittedFormItemViewModel>> GetAllFormItemForAdmin(FilterFormItemViewModel model);
    }
}
