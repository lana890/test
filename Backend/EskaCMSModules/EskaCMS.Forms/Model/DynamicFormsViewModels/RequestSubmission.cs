using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Infrastructure.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.DynamicForms.ViewModels
{
    public class RequestSubmission
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }


        public DateTimeOffset? FromDate { get; set; }

        public DateTimeOffset? ToDate { get; set; }

        public string FullName { get; set; }
        public long FormItemId { get; set; }

        public List<SubmittionConditionsVM> Conditions { get; set; }
        public List<long>? SelectedUser { get; set; }

    }

    public class SubmissionVM
    {
        public DateTimeOffset CreationDate { get; set; }
        public string FullName { get; set; }
        public long FormItemId { get; set; }
        public string FormItemName { get; set; }
        public FormItemType Type { get; set; }
        public long? PageId { get; set; }
        public string PageName { get; set; }
        public string Result { get; set; }
        public int NumberOfFields { get; set; }
        public int NumberOfFieldsAnswered { get; set; }
        public IList<SubmissionInfoVM> SubmissionInfo { get; set; }

    }
    public class SubmissionStatisticsViewModel
    {
        public string FieldName { get; set; }
        public List<AnswerStatisticsViewModel> Answers { get; set; }
    }

    public class AnswerStatisticsViewModel
    {
        public string Answer { get; set; }
        public int Count { get; set; }
    }

    public class GetSubmissionVM
    {
        public int NumberOfResponses { get; set; }
        public string FormItemName { get; set; }

        public PaginationResult<SubmissionVM> SubmissionInfo { get; set; }

    }

    public class ExportSubmissionVM
    {
        public string FullName { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string FormItemName { get; set; }
        public string Result { get; set; }
        public string NumberOfQuestions_NumberOfAnswers { get; set; }
        public string Question_Answer { get; set; }

    }
    public class SubmissionInfoVM
    {
        public long FormFieldId { get; set; }
        public string FormFieldName { get; set; }

        public string Value { get; set; }

        public EDynamicCriteriaInputTypes InputType { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EDynamicFormsOneOfOptions OneOfOptionsType { get; set; }

    }

    public class SubmittionConditionsVM
    {
        public long FormFieldId { get; set; }

        public ComparisonOperators? ComparisonOperators { get; set; }
        public string SearchValue { get; set; }


    }

    public class FilterFormItemViewModel
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }

        public string Name { get; set; }

        public FormItemType? Type { get; set; }
    }


    public class SubmittedFormItemViewModel
    {
        public long Id { get; set; }


        public string? ThumbnailUrl { get; set; }
        public long? ThumbnailId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public FormItemType Type { get; set; }
        public int NumberOfResponses { get; set; }
        public bool HasOneOfOption { get; set; }
    }
}
