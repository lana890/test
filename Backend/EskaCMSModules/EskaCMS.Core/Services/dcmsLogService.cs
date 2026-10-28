using EskaCMS.Core.Areas.Core.ViewModels.LogError;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using EskaCMS.Entities.Entity.Core;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;

namespace EskaCMS.Core.Services
{
    public class dcmsLogService : IdcmsLogService
    {
        private readonly IRepository<DCMSLog> _dcmsLogRepository;
        private readonly ILogger _logger;
        private readonly IWorkContext _workContext;
        public dcmsLogService(IRepository<DCMSLog> dcmsLogRepository, ILoggerFactory loggerFactory, IWorkContext workContext)
        {
            _dcmsLogRepository = dcmsLogRepository;
            _logger = loggerFactory.CreateLogger<dcmsLogService>();
            _workContext = workContext;
        }

        public async Task AddInfo(long? UserId, string Module, string Category, long EventId,string EventName,string Scope,long? ParentId,string Message,string AdditionalInfo,long? SiteId )
        {
            try
            {

                var Currentplattform = _workContext.GetCurrentPlatform();
                DCMSLog objlog = new DCMSLog();

                objlog.AdditionInfo = AdditionalInfo;
                objlog.Category = Category;
                objlog.EventId = EventId;
                objlog.UserId = UserId;
                objlog.EventName = EventName;
                objlog.LogType = LogTypes.Information;
                objlog.Message = Message;
                objlog.Module = Module;
                objlog.Scope = Scope;
                objlog.ParentId = ParentId;
                objlog.SiteId = SiteId;
                objlog.Channel = Currentplattform;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add Log/{GetType().Name}: {exc.Message}");
              
               
            }
        }
        public async Task AddError( string Module, string exception,string Message,string AdditionalInfo,long? SiteId)
        {
            try
            {
                var Currentplattform = _workContext.GetCurrentPlatform();
                DCMSLog objlog = new DCMSLog();
                objlog.Channel = Currentplattform;
                objlog.AdditionInfo = AdditionalInfo;
                objlog.LogType= LogTypes.Error;
                objlog.SiteId = SiteId;
                objlog.Message = Message;
                objlog.Exception = exception;
                objlog.Module = Module;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add error Log/{GetType().Name}: {exc.Message}");
               
            }
        }
        public async Task AddError(long? UserId, string Module,string Scope, string exception, string Message, string AdditionalInfo,long? SiteId)
        {
            try
            {
                var Currentplattform = _workContext.GetCurrentPlatform();
                DCMSLog objlog = new DCMSLog();
                objlog.Channel = Currentplattform;
                objlog.AdditionInfo = AdditionalInfo;
                objlog.LogType = LogTypes.Error;
                objlog.Scope = Scope;
                objlog.UserId = UserId;
                objlog.Message = Message;
                objlog.Exception = exception;
                objlog.Module = Module;
                objlog.SiteId = SiteId;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add error Log/{GetType().Name}: {exc.Message}");

            }
        }
        public async Task AddInfo(long? UserId, string Module,  string Message, string AdditionalInfo, long? SiteId)
        {
            try
            {
                var Currentplattform = _workContext.GetCurrentPlatform();
                DCMSLog objlog = new DCMSLog();
                objlog.LogType = LogTypes.Information;
                objlog.AdditionInfo = AdditionalInfo;
                objlog.UserId = UserId;
                objlog.Message = Message;
                objlog.Module = Module;
                objlog.SiteId = SiteId  ;
                objlog.Channel = Currentplattform;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add error Log/{GetType().Name}: {exc.Message}");

            }
        }

        public async Task AddInfo( string Module, string Message, string AdditionalInfo, long? SiteId)
        {
            try
            {
                var Currentplattform = _workContext.GetCurrentPlatform();
                DCMSLog objlog = new DCMSLog();
                objlog.LogType = LogTypes.Information;
                objlog.AdditionInfo = AdditionalInfo;
                objlog.Channel = Currentplattform;
                objlog.Message = Message;
                objlog.Module = Module;
                objlog.SiteId = SiteId;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add error Log/{GetType().Name}: {exc.Message}");

            }
        }

        public async Task AddWarning(long? UserId, string Module, string Message, string AdditionalInfo, long? SiteId)
        {
            try
            {
                DCMSLog objlog = new DCMSLog();
                objlog.LogType = LogTypes.Warning;
                objlog.AdditionInfo = AdditionalInfo;
                objlog.UserId = UserId;
                objlog.Message = Message;
                objlog.Module = Module;
                objlog.SiteId = SiteId;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add error Log/{GetType().Name}: {exc.Message}");

            }
        }

        public async Task<PaginationResult<ResponseLogErrorVM>> GetErrorsLog(RequestErrorLogVM request,long SiteId)
        {
            try
            {
                var query = _dcmsLogRepository.Query().Include(x=>x.User).Where(x =>
                                                       (x.SiteId == null || x.SiteId == SiteId) &&
                                                       (string.IsNullOrEmpty(request.SearchText) ||
                                                        x.Module.Contains(request.SearchText) ||
                                                        x.Scope.Contains(request.SearchText) ||
                                                        x.Exception.Contains(request.SearchText) ||
                                                        x.AdditionInfo.Contains(request.SearchText) ||
                                                        x.Message.Contains(request.SearchText)||
                                                        (x.UserId!=null && x.User.UserName.Contains(request.SearchText))
                                                               ));

                if (request.FromDate.HasValue)
                {

                    query = query.Where(x => x.Date >= request.FromDate.Value);


                }
                if (request.ToDate.HasValue)
                {

                    query = query.Where(x => x.Date <= request.ToDate.Value);
                }

                var Result = await query.Select(x => new ResponseLogErrorVM
                {
                    Id=x.Id,
                    Scope = x.Scope,
                    Date = x.Date,
                    AdditionalInfo = x.AdditionInfo,
                    exception = x.Exception,
                    Message = x.Message,
                    Module = x.Module,
                    UserId = x.UserId,
                    Channel=x.Channel,
                    UserName = x.User == null ? "" : x.User.UserName
                }).OrderByDescending(x => x.Id).PaginateAsync(request.PageSize, request.PageNumber);


                return Result;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

      
    }
}
