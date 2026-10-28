using EskaCMS.Core.BusinessModels;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Entities.Entity.ApiMapperAndLogs;
namespace EskaCMS.Core.Services
{
    public interface IApiMapper
    {

        Task<object> TestInvokerAPI(ApiMapperViewModel ApiInfo, JObject obj, long userId,string token ="");
        Task<dynamic> Invoke(long ApiId, JObject obj,long userId, string token = null);
        Task<dynamic> StaticInvoke(StaticInvokeVM model);
        Task<List<ApiMapperViewModel>> GetApiMapperSiteId(long SiteId);
        Task<ApiMapperViewModel> GetApiMapperById(long APIId);
        Task<long> AddApiMapper(DatasourceVM datasourceVM, long siteId, long userID);
        Task<long> EditApiMapper(DatasourceVM datasourceVM, long siteId, long userID);
        Task<List<UserOptionsVM>> GetAuthenticationUserOptions(string propertyName, string refreshPropertyName);

        Task<dynamic> GetMapperParamsByMapperId(long mapperId, long siteId, long userID, ParameterDirection direction);
        Task<ApiMapperParams> AddApiMapperParams(ApiMapperParamsViewModel MapperParams, long userID);
        Task<ApiMapperParams> EditApiMapperParams(ApiMapperParamsViewModel MapperParams, long userID);
        Task<bool> DeletMapperParams(string paramName, long siteId, long userId);

		Task<bool> DeletMapperParamsChild(long id, long siteId, long userId);


		///Interface for luay API
		Task<ApiMapperDetailsViewModel> GetDatasourceById(long id);
        Task<dynamic> GetDatasourceByName(string name, JObject obj, int userId = 0, string token = "");
		Task<List<ApiMapperDetailsViewModel>> GetDataSourceListBySiteId(long id);
        Task<PaginationResult<GetApiMapperTransactionLogVM>> GetApiMapperTransactionHistory(SearchApiMapperTransactionHistory model);

        Task<APIMapperParamChild> LinkChildParamToParent(LinkChildParamToParentRequest req, long userID);
        Task<ResponsePathPreviewVM> PreviewResponsePath(ResponsePathPreviewRequest req, long userId, string authToken = "");
        bool DeleteDataSource(long Id);
        Task<List<inputValuesViewModel>> GetInputValues(long Id);

        Task<dynamic> HandleWebAPIRequests(long ApiMapperId, dynamic obj, long userId, string authToken = "", dynamic FormData = null);
    }
}
