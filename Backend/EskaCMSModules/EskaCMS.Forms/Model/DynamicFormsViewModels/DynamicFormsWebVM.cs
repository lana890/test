using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.DynamicForms.ViewModels
{
    public class DynamicFormsWebVM
    {
        public string FormFieldName { get; set; } 
        public long FormFieldId { get; set; }

        public EDynamicCriteriaInputTypes InputType { get; set; }


        public string Value { get; set; } // for InputType currency || text || number

        public string SubCategoryId { get; set; } // for InputType option


        public long CurrencyRatesId { get; set; } // for InputType currency
    }

    public class DynamicFormsSettingsVM
    {
        public bool? allowDuplicateSurveyPerUser { get; set; }

    }
    public class DynamicFormsFormDataForWebVM
    {
        public List<DynamicFormsWebVM> FormData { get; set; }

    }
}
