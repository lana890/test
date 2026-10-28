using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.DynamicForms
{
    public class SubmissionInfo:EntityBase
    {
        public long FormFieldId { get; set; }
        public FormField FormField { get; set; }

        public string Value { get; set; }

        public EDynamicCriteriaInputTypes InputType { get; set; }

        public long? SubCategoryId { get; set; }
        public Category SubCategory { get; set; }

        public long SubmissionId { get; set; }
        public Submission Submission { get; set; }


    }
}
