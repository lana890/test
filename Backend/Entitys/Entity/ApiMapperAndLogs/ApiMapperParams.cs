using EskaCMS.Entities.Entity.ApiMapperAndLogs;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Core
{
    public class ApiMapperParams : EntityBase
    {
        public ApiMapper ApiMapper { get; set; }
        public long ApiMapperId { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string ParamName { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public int ParamOrder { get; set; }
        public ApiMapperParams Parent { get; set; } 
        public long? ParentId { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long CreatedById { get; set; }
        public ParameterDirection Direction { get; set; }
        public ParamsTypes ParamType { get; set; }
        public SourceType SourceType { get; set; }
        public string ParamValue { get; set; }
        public string DefaultValue { get; set; }
        public bool AllowNull { get; set; }
        public bool IsDeleted { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }

		public ICollection<APIMapperParamChild> APIMapperParamChild { get; set; }

		public string Alias { get; set; }
        // The link to a parent API output lives in APIMapperParamChild, keyed by (ApiMapperId, ParamName).
        // The old APIParamChildId back-pointer was redundant with that key and has been removed.
    }
}
