using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

using EskaCMS.Entities.Entity.Core;
namespace EskaCMS.Core.Services
{
    public class ApiLogService : IApiLogService
    {
        public readonly IRepository<ApiLog> _logRepository;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public ApiLogService(IRepository<ApiLog> logRepository , IServiceScopeFactory serviceScopeFactory)
        {
            _logRepository = logRepository;
            _serviceScopeFactory = serviceScopeFactory;

        }
        public async Task<bool> CreateLog(ApiLogVM log)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var _logRepo = scope.ServiceProvider.GetRequiredService<IRepository<ApiLog>>();

                    ApiLog coreLog = new ApiLog
                    {
                        ApiUrl = log.ApiUrl,
                        Date = DateTimeOffset.Now,
                        Request = log.Request,
                        FullUrl = log.FullUrl,
                        HttpMethod = log.HttpMethod,
                        Response = log.Response,
                        SiteId = log.SiteId,
                        StatusCode = log.StatusCode,
                        UserId = log.UserId
                    };
                    _logRepo.Add(coreLog);
                    await _logRepo.SaveChangesAsync();

                    return true;
                }
            }
            catch (Exception e)
            {
                return false;
            }
          
        }
    }
}
