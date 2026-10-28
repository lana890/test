using EskaCMS.EskaCoreReports.ViewModels;
using EskaCMS.Infrastructure.Repositories;
using EskaCMS.Infrastructure.Web.SmartTable;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.EskaCoreReports.ViewModels.EskaCoreReportVM;

namespace EskaCMS.EskaCoreReports.Repositories
{
    public interface IReportRepository
    {

        #region reports
        Task<ReportVM> GetReportById(long id);
        Task<PagenationReportMenuListVM> GetReportsByMenuId(ReportsByMenuIdSearchVM? MenuId);

        Task<ReportsBreadCrumbVM> GetReportBreadCrumb(string Slug);
        Task<int> GetReportsCount(long siteId);
        Task<bool> Create(EskaCoreReportVM model);

        Task<bool> ChangeReportStatus(long Id, EStatus Status);

        Task<bool> Update(EskaCoreReportVM model);
        Task<SmartTableResult<ReportDetailsVM>> GetReports(SmartTableParam param, long SiteId);
        Task<List<ReportHistoryVM>> GetReportHistory(long id);
        Task<SmartTableResult<ReportDetailsVM>> GetReportsByAuthors(SmartTableParam Param, long SiteId , long UserId);

        #endregion
    }
}
