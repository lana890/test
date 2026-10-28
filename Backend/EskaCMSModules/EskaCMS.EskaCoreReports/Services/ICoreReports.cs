
using EskaCMS.EskaCoreReports.ViewModels;
using EskaCMS.Infrastructure.Web.SmartTable;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.EskaCoreReports.Services
{
    public interface ICoreReports
    {
        Task<bool> UpsertReport(EskaCoreReportVM coreReportVM);

        Task<ReportsBreadCrumbVM> GetReportBreadCrumb(string Slug);
        Task<bool> ChangeReportStatus(long Id,EStatus status );
        Task<SmartTableResult<ReportDetailsVM>> GetReports(SmartTableParam param, long SiteId);
        Task<int> GetReportsCount(long SiteId);
        Task<PagenationReportMenuListVM> GetReportsByMenuId(ReportsByMenuIdSearchVM Model);
        Task<ReportVM> GetReportById(long id);
        Task<List<ReportHistoryVM>> GetReportHistory(long id);
        Task<SmartTableResult<ReportDetailsVM>> GetReportsByAuthors(SmartTableParam Param, long SiteId , long UserId);
    }
}
