using EskaCMS.Entities.Entity.ApiMapperAndLogs;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Policy;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Core
{
    public class ApiMapper : EntityBase
    {

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(1000)]
        public string URL { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]

        public ApihttpMethods HttpMethod { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public ApiTypes Type { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public EStatus Status { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long CreatedById { get; set; }

        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public ApiAuthorizationTypes  AuthorizationType { get; set; }

        public string WebMethodName { get; set; }
        public virtual Site Site { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public long? SiteId { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public string MediaType { get; set; }

        public string ContractName { get; set; }
        public string ContentType { get; set; }
        public string Accept { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
        public ApiReponseType ApiReponseType { get; set; }
        public ICollection<ApiMapperParams> ApiMapperParams { get; set; }

		public ICollection<APIMapperParamChild> APIMapperParamChild { get; set; }


		public List<ApiMapperAuthorization> ApiMapperAuthorizations { get; set; }
        public long? ParentId { get; set; }
        public virtual ApiMapper Parent { get; set; }
        public bool EnableLog { get; set; }

        public List<ApiMapperTransactionLog>? apiMapperTransactionLogs { get; set; }
        // Inverse of the removed Parent navigation. Left mapped it would make EF Core create a shadow
        // self FK on Core_ApiMapper (a column that does not exist). The parent/child relation now lives
        // in APIMapperParamChild.
        //public ICollection<ApiMapper> Children { get; set; }

    }
}
