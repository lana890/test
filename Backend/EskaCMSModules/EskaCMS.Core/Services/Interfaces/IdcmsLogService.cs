using EskaCMS.Core.Areas.Core.ViewModels.LogError;
using EskaCMS.Infrastructure.Pagination;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Services
{
    public interface IdcmsLogService
    {
        Task AddInfo(long? UserId, string Module, string Category, long EventId, string EventName, string Scope, long? ParentId, string Message, string AdditionalInfo,long? SiteId);
        Task AddError( string Module, string exception, string Message, string AdditionalInfo,long? SiteId);
        Task AddError(long? UserId, string Module, string Scope, string exception, string Message, string AdditionalInfo,long? SiteId);
        Task AddInfo(long? UserId, string Module, string Message, string AdditionalInfo,long? SiteId);
        Task AddInfo(string Module, string Message, string AdditionalInfo, long? SiteId);
        Task AddWarning(long? UserId, string Module, string Message, string AdditionalInfo, long? SiteId);
        Task<PaginationResult<ResponseLogErrorVM>> GetErrorsLog(RequestErrorLogVM request,long SiteId);
    }
}
