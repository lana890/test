

using EskaCMS.Core.Extensions;
using EskaCMS.DynamicForms.Services.IServices;
using EskaCMS.DynamicForms.ViewModels;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.DynamicForms.Services
{
    public class SubmissionServices : ISubmissionServices
    {
        private readonly IRepository<Submission> submissionRepository;
        private readonly IWorkContext workContext;
        private readonly IRepository<SubmissionInfo> submissionInfoRepository;

        public SubmissionServices(IRepository<Submission> submissionRepository, IWorkContext workContext, IRepository<SubmissionInfo> submissionInfoRepository)
        {
            this.submissionRepository = submissionRepository;
            this.workContext = workContext;
            this.submissionInfoRepository = submissionInfoRepository;
        }

        public async Task Create(List<DynamicFormsWebVM> data, Submission submission)
        {
            try
            {

                submissionRepository.Add(submission);

                await submissionRepository.SaveChangesAsync();

                var SubmissionInfo = data.ConvertAll(c => new SubmissionInfo
                {
                    FormFieldId = c.FormFieldId,
                    InputType = c.InputType,
                    Value = c.Value,
                    SubCategoryId = string.IsNullOrEmpty(c.SubCategoryId) || c.SubCategoryId == "0" ? null : Convert.ToInt64(c.SubCategoryId),
                    SubmissionId = submission.Id
                });
                submissionInfoRepository.AddRange(SubmissionInfo);

                await submissionInfoRepository.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<GetSubmissionVM> GetSubmission(RequestSubmission model)
        {
            try
            {
                var siteId = workContext.GetCurrentSiteId();
                var Result = new GetSubmissionVM();
                var Query = await submissionInfoRepository
                                           .Query()
                                           .Where(x => x.Submission.SiteId == siteId)
                                           .Include(x => x.Submission)
                                           .Include("Submission.CreatedBy")
                                           .Include("Submission.Page")
                                           .Include("Submission.FormItem")
                                           .Include("Submission.FormItem.FormItemDetails")
                                           .AsNoTracking()
                                           .FilterSubmission(model)
                                           .Where(x => x.Submission.FormItemId == model.FormItemId)
                                           .OrderByDescending(x => x.Id).ToListAsync();

                if (model.Conditions != null && model.Conditions.Count() > 0)
                {
                    Query = Query.Where(x => model.Conditions.Any(m => m.FormFieldId == x.FormFieldId)).ToList();

                    foreach (var condition in model.Conditions)
                    {
                        if (condition.ComparisonOperators.HasValue && !string.IsNullOrEmpty(condition.SearchValue))
                        {
                            switch (condition.ComparisonOperators.Value)
                            {
                                case ComparisonOperators.Equal:
                                    Query = Query.Where(n =>
                                                      n.Submission.Result == condition.SearchValue || n.Value == condition.SearchValue).ToList();

                                    break;

                                case ComparisonOperators.NotEqual:
                                    Query = Query.Where(n =>
                                                      n.Submission.Result != condition.SearchValue || n.Value != condition.SearchValue).ToList();
                                    break;

                                case ComparisonOperators.GreaterThan:
                                    Query = Query
                                            .Where(x => (IsInteger(x.Submission.Result) && Convert.ToInt32(x.Submission.Result) > Convert.ToInt32(condition.SearchValue))
                                                        || IsInteger(x.Value) && Convert.ToInt32(x.Value) > Convert.ToInt32(condition.SearchValue))
                                            .ToList();
                                    break;

                                case ComparisonOperators.LessThan:
                                    Query = Query.Where(x => (IsInteger(x.Submission.Result) && Convert.ToInt32(x.Submission.Result) < Convert.ToInt32(condition.SearchValue))
                                                            || IsInteger(x.Value) && Convert.ToInt32(x.Value) < Convert.ToInt32(condition.SearchValue)).ToList();
                                    break;

                                case ComparisonOperators.GreaterThanOrEqual:
                                    Query = Query.Where(x => (IsInteger(x.Submission.Result) && Convert.ToInt32(x.Submission.Result) >= Convert.ToInt32(condition.SearchValue))
                                                             || (IsInteger(x.Value) && Convert.ToInt32(x.Value) >= Convert.ToInt32(condition.SearchValue))).ToList();

                                    break;

                                case ComparisonOperators.LessThanOrEqual:
                                    Query = Query.Where(x => (IsInteger(x.Submission.Result) && Convert.ToInt32(x.Submission.Result) <= Convert.ToInt32(condition.SearchValue))
                                                             || (IsInteger(x.Value) && Convert.ToInt32(x.Value) <= Convert.ToInt32(condition.SearchValue)))

                                                .ToList();

                                    break;

                            }


                        }
                    }
                }

                var SubmissionIds = Query.Select(x => x.SubmissionId).ToList();

                if (SubmissionIds == null && SubmissionIds.Count() >= 0)
                {
                    return new GetSubmissionVM();

                }

                var SubmittedForms = await submissionRepository.Query()
                                           .Where(x => SubmissionIds.Contains(x.Id))
                                           .Include(x => x.SubmissionInfo)
                                           .Include("SubmissionInfo.FormField")
                                           .Include("SubmissionInfo.FormField.FormFieldDetails")
                                           .Include("SubmissionInfo.FormField.FormFieldDetails.Category")
                                           .Include("SubmissionInfo.FormField.FormFieldDetails.Category.CategoryDetails")
                                           .Include("SubmissionInfo.SubCategory")
                                           .Include("SubmissionInfo.SubCategory.CategoryDetails")
                                           .Include(x => x.CreatedBy)
                                           .Include(x => x.Page)
                                           .Include(x => x.FormItem)
                                           .ThenInclude(x => x.FormItemDetails)
                                           .Include(x => x.FormItem)
                                           .ThenInclude(x => x.FormFields)
                                           .AsNoTracking()

                                           .OrderByDescending(x => x.Id)
                                           .Where(x => model.SelectedUser == null || !model.SelectedUser.Any() || model.SelectedUser.Any(o => o == x.CreatedById))
                                           .Where(x => x.FormItemId == model.FormItemId)
                                           .Select(x => new SubmissionVM
                                           {
                                               CreationDate = x.CreationDate,
                                               FormItemId = x.FormItemId,
                                               FormItemName = x.FormItem.FormItemDetails.Select(x => x.Name).FirstOrDefault(),
                                               PageId = x.PageId,
                                               PageName = x.Page.Name,
                                               FullName = x.CreatedBy.FullName,
                                               Result = x.Result,
                                               Type = x.Type,
                                               NumberOfFields = x.FormItem.FormFields.Count(),
                                               NumberOfFieldsAnswered = x.SubmissionInfo.Where(x => !string.IsNullOrEmpty(x.Value)).Count(),
                                               SubmissionInfo = x.SubmissionInfo.Select(x => new SubmissionInfoVM
                                               {
                                                   FormFieldId = x.FormFieldId,
                                                   InputType = x.InputType,
                                                   Value = x.Value,
                                                   FormFieldName = x.FormField.FormFieldDetails.SelectMany(x => x.Category.CategoryDetails).Select(x => x.Name).FirstOrDefault(),
                                                   OneOfOptionsType = x.FormField.OneOfOptionsType
                                               }).ToList()
                                           }).ToListAsync();

                Result.FormItemName = SubmittedForms.Select(x => x.FormItemName).FirstOrDefault();
                Result.NumberOfResponses = SubmittedForms.Count();
                Result.SubmissionInfo = SubmittedForms.Paginate(model.PageSize, model.PageNumber);

                return Result;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<SubmissionStatisticsViewModel>> GetSubmissionStatistics(long formItemId)
        {
            try
            {
                var siteId = workContext.GetCurrentSiteId();
                var query = await submissionInfoRepository
                    .Query()
                    .Where(x => x.Submission.SiteId == siteId)
                    .Include(x => x.Submission)
                    .Include(x => x.Submission.CreatedBy)
                    .Include(x => x.Submission.Page)
                    .Include(x => x.Submission.FormItem)
                    .Include(x => x.Submission.FormItem.FormItemDetails)
                    .Include(x => x.FormField)
                        .ThenInclude(x => x.FormFieldDetails)
                        .ThenInclude(x => x.Category)
                        .ThenInclude(x => x.CategoryDetails)
                    .AsNoTracking()
                    .Where(x => x.Submission.FormItemId == formItemId)
                    .Where(x => x.InputType == EDynamicCriteriaInputTypes.Option)
                    .GroupBy(x => x.FormFieldId)
                    .Select(group => new SubmissionStatisticsViewModel
                    {
                        FieldName = group.SelectMany(x => x.FormField.FormFieldDetails)
                                         .SelectMany(x => x.Category.CategoryDetails)
                                         .Select(x => x.Name)
                                         .FirstOrDefault(),

                        Answers = group.Where(x => !string.IsNullOrEmpty(x.Value) &&
                                                   x.InputType == EDynamicCriteriaInputTypes.Option)
                                       .GroupBy(x => x.Value.ToLower())
                                       .Select(answerGroup => new AnswerStatisticsViewModel
                                       {
                                           Answer = answerGroup.Key,
                                           Count = answerGroup.Count()
                                       })
                                       .ToList()
                    })
                    .ToListAsync();
                return query;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task ValidateSurveyDuplication(long userId, GeneralEnums.FormItemType type, long? pageId)
        {
            try
            {
                var surveyExists = await submissionRepository.Query().CountAsync(x =>
                                      (!pageId.HasValue || x.PageId == pageId) && type == x.Type && userId == x.CreatedById) > 0;

                if (surveyExists)
                {
                    throw new Exception("A survey for this user has already been submitted");
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private bool IsInteger(string value)
        {
            return int.TryParse(value, out _);
        }
    }
}
