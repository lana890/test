using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.DynamicForms.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.DynamicForms.Services.IServices
{
    public interface IFormFieldService
    {
        Task<List<FormFieldVM>> GetFormFields(long FormItemId, long SiteId);

        Task<List<FormFieldVM>> GetNumaricalFormFields(long FormItemId, long SiteId);
        Task<FormFieldVM> GetFormFieldById(long Id);
        Task<bool> UpdateFormFieldStatus(long Id, EStatus NewStatus, long UserId);
        Task<List<FormFieldVM>> UpsertFormField(List<FormFieldVMs> Model, long UserId, long SiteId);
        Task<List<FormFieldGroup>> GetFormFieldGroups(long FormItemId);
        Task<List<FormFieldGroup>> UpsertGroup(List<FormFieldGroupVM> Model);
        Task<bool> DeleteGroup(long GroupId);
        Task<PaginationResult<FormFieldVM>> GetGeneralFormFields(SmartSearchTableParam<AllGeneralFormField> model);
    }
}
