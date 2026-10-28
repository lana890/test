using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models
{

    public class GoogleTokenInfoViewModel
    {
        public string Issuer { get; set; }
        public string AuthorizedParty { get; set; }
        public string Audience { get; set; }
        public string Subject { get; set; }
        public string Email { get; set; }
        public bool EmailVerified { get; set; }
        public string AccessTokenHash { get; set; }
        public string Name { get; set; }
        public string PictureUrl { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string Locale { get; set; }
        public long IssuedAt { get; set; }
        public long Expiration { get; set; }
    }

    public class GoogleSettingsViewModel
    {

        public string AppId { get; set; }
        public string AppSecret { get; set; }


    }
}
