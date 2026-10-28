using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Survey
{
    public class Questionnaire : EntityBase
    {

        [StringLength(150)]
        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }
        public QuestionnaireType Type { get; set; }
        public long SiteId { get; set; }

        [StringLength(150)]
        [Required(ErrorMessage = "The {0} field is required.")]
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public List<Section> Sections { get; set; }
        public List<FilledQuestionnaire> FilledQuestionnaires { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }

    }
}
