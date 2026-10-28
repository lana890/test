using EskaCMS.Entities.Entity.EcomLog;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.ShoppingCart.EcommerceLog.ModelView;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.ShoppingCart.EcommerceLog.Services.Interface
{
    public interface IEcommerceLogService
    {
        Task<EcommerceLogs> CreateLogAsync(EcommerceLogs log);
        Task<PaginationResult<EcommerceLogWithVersionsVM>> GetAllLogsAsync(SearchLogParam logParam);
        Task<EcommerceLogWithVersionsVM> GetLogWithAllVersionsAsync(long id);
        Task<EcommerceLogs> SetMainVersion(long logId, long versionId);
        Task<bool> ReSubmitLogAsync(long ecomLogId, RequestPayload? Request = null);
        Task UpdateLogStatusAsync(long logId, TransactionStatus status, Cart? cart, string response, int statusCode, RequestPayload? payload = null, bool? IsVersionExecuted = false);
        Task UpdateLogWithErrorAsync(long logId, Exception ex);
        //   Task UpdateLogCurrentVersionAsync(long logId, long versionId);
        //   Task<EcommerceLogsVersions> AddAsync(AddVersionRequest version);
        Task<EcommerceLogsVersions> GetById(long versionId);
        Task<EcommerceLogsVersions> AddInitialVersionAsync(AddVersionRequest version);
        Task<List<EcommerceLogsVersions>> GetLogsForRetryAsync(int maxRetry);
        Task<EcommerceLogs> CreateLogWithResponseAsync(RequestPayload request, string response, int httpStatusCode, long userId, long siteId,long? cartId);

    }

}
