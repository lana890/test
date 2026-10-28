using EskaCMS.Infrastructure.Models;
using System;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Survey
{
    public class FilledAnswer : EntityBase
    {
        public long FilledQuestionnaireId { get; set; }
        public long QuestionId { get; set; }
        public long? AnswerId { get; set; }
        public string AnswerValue { get; set; }
        [StringLength(150)]
        [Required(ErrorMessage = "The {0} field is required.")]
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }

        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public virtual FilledQuestionnaire FilledQuestionnaire { get; set; }
        public virtual Question Question { get; set; }
        public virtual Answer Answer { get; set; }

        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
    }
}
