using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Forms.Model.FormsViewModel;

namespace EskaCMS.Forms.Services.FormsService.Interfaces
{
    public interface IFormService
    {
       // Task SaveForm(long PageId, string ComponentSettingsId, string Culture, string FormData, IFormFileCollection files);
        GridOutputVM<FormDataViewModel> GetSubmittedForms(long siteId, SearchTableParam<FormDataViewModel> param);
        Task<bool> NewFormSave(long PageId, string ComponentSettingsId, string Culture, string FormData, IFormFileCollection filesm, string HostName,bool?NotFromPricingQuotation=true);
        Task<bool> UpsertFormBuilderSettings(List<FormBuilderVm>  formBuilder);
        Task<List<FormResponseVm>> GetAllForms(long PageId);
        Task<List<GetAllKeysVM>>  GetAllKeys(long PageId);
        Task<FormEditVM> GetFormEdit(long Id);
    }
}
