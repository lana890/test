using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Survey
{
    public class Answer : EntityBase
    {
        public string Value { get; set; }
        public bool IsCorrect { get; set; }
        public long QuestionId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(150)]
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public Question Question { get; set; }
        public List<FilledAnswer> FilledAnswers { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
    }
}
