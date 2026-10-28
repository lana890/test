using EskaCMS.Forms.Model.FormsViewModel;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Forms.Services.FormsService.Interfaces
{
    public interface ISubmittedFormService
    {
        Task<PaginationResult<SubmittedFormDetailsVM>> GetAllSubmittedForm(SmartTableParam param);
        Task<SubmittedFormResponseVM> SubmitForm(SubmittedFormVM model);
        Task<SubmittedFormDetailsVM> GetSubmittedFormByCode(SubmittedFormByCodeVM model);
        Task< Dictionary<string,List< ExpandoObject>>> GetStatistics(StatisticsVM model);
        Task<PaginationResult<SubmittedFormDetailsVM>> AllSubmittedByStatusOrCode(SubmittedFormByCodeVM model);
    }
}
