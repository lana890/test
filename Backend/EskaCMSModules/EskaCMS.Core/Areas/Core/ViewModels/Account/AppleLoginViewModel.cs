using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.ViewModels.Account
{
    public class AppleLoginViewModel
    {
        public string AccessToken { get; set; }
        [Required]
        public long CustomerGroupId { get; set; }

        [Required]
        public long RoleId { get; set; }

    }

    public class AppleSettingsViewModel
    {

        public string PrivateKey { get; set; }
        public string KeyId { get; set; }
        public string ClientId { get; set; }
        public string BundleId { get; set; }
        public string TeamId { get; set; }
        public string AppleUrl { get; set; }


    }
}
