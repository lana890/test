using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using EskaCMS.EskaCoreReports.Repositories;
using EskaCMS.EskaCoreReports.ViewModels;
using EskaCMS.Infrastructure.Web.SmartTable;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.EskaCoreReports.Services
{
    public class CoreReports : ICoreReports
    {
        private readonly EskaDCMSDbContext _context;
        private readonly IReportRepository _reportRepository;
        private readonly IWorkContext _workContext;

        public CoreReports(
            EskaDCMSDbContext context,
            IReportRepository reportRepository,
            IWorkContext workContext
            )
        {
            _context = context;
            _reportRepository = reportRepository;
            _workContext = workContext;
        }


        public async Task<bool> UpsertReport(EskaCoreReportVM model)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {


                    if (model.Id.Value == 0)
                    {
                       await _reportRepository.Create(model);
                    }
                    else
                    {
                        await _reportRepository.Update(model);
                    }
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw ex;
                }
            });

        }
        public async Task<SmartTableResult<ReportDetailsVM>> GetReports(SmartTableParam param, long SiteId)
        {
            try
            {
                return await _reportRepository.GetReports(param, SiteId);

            }
            catch (Exception ex)
            {

                throw  ex;
            }
        }
        public async Task<ReportVM> GetReportById(long Id)
        {
            try
            {
                var Result=   await _reportRepository.GetReportById(Id);

                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<ReportsBreadCrumbVM> GetReportBreadCrumb(string Slug)
        {
            try
            {
                return  await _reportRepository.GetReportBreadCrumb(Slug);

            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public async Task<PagenationReportMenuListVM> GetReportsByMenuId(ReportsByMenuIdSearchVM Model)
        {
            try
            {

            return await _reportRepository.GetReportsByMenuId(Model);

            }
            catch (Exception EX)
            {

                throw;
            }
        }

        public async Task<int> GetReportsCount(long SiteId)
        {
            try
            {

            return await _reportRepository.GetReportsCount(SiteId);

            }
            catch (Exception ex)
            {

                throw;
            }
        }


        public async Task<bool> ChangeReportStatus(long Id,EStatus status)
        {
            try
            {
                await _reportRepository.ChangeReportStatus(Id,status);
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<ReportHistoryVM>> GetReportHistory(long id)
        {
            try
            {
                return await _reportRepository.GetReportHistory(id);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<SmartTableResult<ReportDetailsVM>> GetReportsByAuthors(SmartTableParam Param, long SiteId, long UserId)
        {
            try
            {
                return await _reportRepository.GetReportsByAuthors(Param, SiteId, UserId);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

    }
}
