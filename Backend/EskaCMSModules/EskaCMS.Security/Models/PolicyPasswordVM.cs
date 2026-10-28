using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Models
{
    public class PolicyPasswordVM
    {
        public long? Id { get; set; }

        public long? SiteId { get; set; }

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
        public EStatus Status { get; set; }


        public DateTimeOffset StatusDate { get; set; }


        [StringLength(450)]
        public string RegularExpression { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        
        [Required(ErrorMessage = "The {0} field is required.")]
        public int NoRepeatedPassword { get; set; } // Number Of Repeated Passwords

        public bool UseAlreadyUsedPassword { get; set; }

        public bool UseUsernameSamePWD { get; set; }
    }
}
