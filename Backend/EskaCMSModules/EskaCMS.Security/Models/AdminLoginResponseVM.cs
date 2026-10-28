using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Core.Models.TokenVM;

namespace EskaCMS.Security.Models
{
  public  class AdminLoginResponseVM
    {
        public AdminLoginResponseVM()
        {
            OptionsCodes = new List<string>();
        }
        public long SiteId { get; set; }
        public bool IsDefault { get; set; }
        public List<string> OptionsCodes { get; set; }

        public TokenResponse Token { get; set; }

    }
    public class DeleteAccountViewModel
    {
        public string password { get; set; }
    }
    public class  CrawlerViewModel
    {
        public string UserName { get; set; }
    }
}
