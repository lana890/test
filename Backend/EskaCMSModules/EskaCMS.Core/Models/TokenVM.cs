using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Models
{
   public class TokenVM
    {
        public class TokenLoginModel
        {
            [Required(ErrorMessage = "The {0} field is required.")]
            public string Username { get; set; }

            [Required(ErrorMessage = "The {0} field is required.")]
            public string Password { get; set; }
        }

        public class RefreshTokenModel
        {
            [Required(ErrorMessage = "The {0} field is required.")]
            public string Token { get; set; }

            [Required(ErrorMessage = "The {0} field is required.")]
            public string RefreshToken { get; set; }
        }

        public class TokenResponse
        {
            public string Token { get; set; }
            public string RefreshToken { get; set; }
            public DateTime ExpiryDate { get; set; }
        }
    }
}
