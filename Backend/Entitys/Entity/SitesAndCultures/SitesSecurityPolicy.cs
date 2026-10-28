using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Core
{
    public class SitesSecurityPolicy : EntityBase
    {

        [Required(ErrorMessage = "The {0} field is required.")]
        public long SiteId { get; set; }
        public virtual Site Site { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public int MinDigits { get; set; }


        [Required(ErrorMessage = "The {0} field is required.")]
        public int MinLength { get; set; }


        [Required(ErrorMessage = "The {0} field is required.")]
        public int MinLowerCaseLetters { get; set; }


        [Required(ErrorMessage = "The {0} field is required.")]
        public int MinSpecialCharacters { get; set; }


        [Required(ErrorMessage = "The {0} field is required.")]
        public int MinUpperCaseLetters { get; set; }



        [Required(ErrorMessage = "The {0} field is required.")]
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }


        public DateTimeOffset StatusDate { get; set; }


        [StringLength(450)]
        public string RegularExpression { get; set; }


        public virtual User CreatedBy { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public virtual User ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public long? ModifiedById { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public int NoRepeatedPassword { get; set; } // Number Of Repeated Passwords

        public bool UseAlreadyUsedPassword { get; set; }

        public bool UseUsernameSamePWD { get; set; }

    }
}
