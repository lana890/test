using EskaCMS.DynamicForms.ViewModels;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.DynamicForms.Services.IServices
{
    public interface ISubmissionServices
    {
        Task Create(List<DynamicFormsWebVM> data, Submission submission);
        Task<GetSubmissionVM> GetSubmission(RequestSubmission model);
        Task<List<SubmissionStatisticsViewModel>> GetSubmissionStatistics(long formItemId);
        Task ValidateSurveyDuplication(long userId, GeneralEnums.FormItemType type, long? pageId);
    }
}
