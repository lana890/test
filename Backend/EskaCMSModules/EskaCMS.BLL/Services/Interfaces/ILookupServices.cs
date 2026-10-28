using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.General.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.General;

namespace EskaCMS.General.Services.Interfaces
{
    public interface ILookupServices
    {

        Task<List<LookupVM>> GetLookup();
        Task<List<LookupValuesVM>> GetLookupValues(long lookupId, long? ParentId);
        Task<LookupVM> GetLookupById(long lookupId);
        Task<LookupValuesWithLookupId> GetLookupValuesbylookupId(long lookupId,long? ParentId=null);
        Task<LookupValuesWithLookupId> GetLookupValues();
        Task<LookupValuesWithLookupId> GetLookupValueList(long LookupId, LookupSearchVM lookupSearchVM);
        Task<long> AddNewLookups(LookupModel lookupModel, long SiteId);
        Task<bool> EditLookups(LookupModel lookupModel, long Id);
        Task<bool> DeleteLookup(long Id);
        Task<long> AddLookupValues(LookupValueVM lookupValuesVM);
        Task<bool> ChangeLookupValuesStatus(long Id, Entities.Enum.GeneralEnums.EStatus eStatus);
        Task<bool> EditLookupValues(LookupValuesWithoutOrder lookupValuesVM, long Id);
        Task<List<LookupValuesVM>> GetLookupValuesByRoles(List<long> RoleIds);
        Task<bool> UpsertLookupValuesRoles(LookupValuesRolesVM lookupRolesVM);
        Task<LookupValuesVM> getLookupValuesbyLookupIdAndvalueId(long lookupId, long ValueId);

        Task<List<LookupModel>> GetLookupsList(LookupSearchVM searchParamsVM, long SiteId);
        Task<bool> ChangeStatus(long LookupId, Entities.Enum.GeneralEnums.EStatus eStatus);
        Task<LookupValuesVM> GetAllLookupValues(long LookupId);
        Task<List<LookupValues>> ChangeLookupOrder(LookupValuesWithOrder Model);
        Task<LookupValuesDetails> GetAllLookupValuesByLookupId(long LookupId);
        Task<LookupValuesWithRole> GetRoleForLookupValues(long LookupvalueId);
        Task<LookupValuesWithLookupId> GetLookupByLookupIdAndName(long lookupId, string name);

        Task<LookupValuesDetails> GetAllLookupValuesByCode(string code);
    }
}
