using EskaCMS.Entities.Entity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Models
{
    public class UserOTPVM
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public List<UserOTPDetailsVM> lstOtps { get; set; }

    }
    public class UserOTPDetailsVM
    {
        public string Value { get; set; }
        public E2FAProviders ProviderType { get; set; }
        public string ProviderValue { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string Purpose { get; set; }
        public TimeSpan Validity { get; set; }
        public DateTimeOffset ExpiryDate { get; set; }
    }
   
   
}
