
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.MediaEntity;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.DynamicForms
{
    public partial class FormItem : EntityBase
    {
        public FormItem()
        {
            Classifications = new List<Classifications>();
            FormFieldGroups = new List<FormFieldGroup>();
            FormFields = new List<FormField>();
            FormItemDetails = new List<FormItemDetails>();
        }

        // public int Id { get; set; }
        public string Icon { get; set; }
        public string AdditionalInfo { get; set; }

        public long? ThumbnailId { get; set; }
        public Media Thumbnail { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }
        public IList<Classifications> Classifications { get; set; }

        public IList<FormItemDetails> FormItemDetails { get; set; }
        public IList<FormFieldGroup> FormFieldGroups { get; set; }

        public IList<FormField> FormFields { get; set; }

        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }


        public FormItemType Type { get; set; }

        public long? ModifiedById { get; set; }
        public virtual User ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }


        [Required(ErrorMessage = "The {0} field is required.")]
        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public ICollection<FormItemRole> FormItemRoles { get; set; }
        public ICollection<Submission>? Submissions { get; set; }

    }
}
