using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Survey
{
    public class Question : EntityBase
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(1000)]
        public string QuestionText { get; set; }
        public EskaCMS.Entities.Enum.QuestionType Type { get; set; }
        public  EskaCMS.Entities.Enum.ChoiceType ChoiceType { get; set; }
        public EskaCMS.Entities.Enum.AnswerFieldType AnswerFieldType { get; set; }
        public EskaCMS.Entities.Enum.DateFormat DateFormat { get; set; }
        public long SectionId { get; set; }
        public int QuestionOrder { get; set; }
        public bool IsRequired { get; set; }
        public bool HasOtherAnswer { get; set; }

        [StringLength(150)]
        [Required(ErrorMessage = "The {0} field is required.")]
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public virtual Section Section { get; set; }
        public List<Answer> MultipleChoiceAnswers { get; set; }
        public List<FilledAnswer> FilledAnswers { get; set; }

        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
    }
}
