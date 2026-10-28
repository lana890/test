using EskaCMS.Infrastructure.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Survey
{
    public class FilledQuestionnaire : EntityBase
    {
        public long QuestionnaireId { get; set; }
        public long UserId { get; set; }
        public string IPAddres { get; set; }
        [StringLength(150)]
        [Required(ErrorMessage = "The {0} field is required.")]
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }

        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public Questionnaire Questionnaire { get; set; }
        public List<FilledAnswer> FilledAnswers { get; set; }

        public virtual User User { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
    }
}
