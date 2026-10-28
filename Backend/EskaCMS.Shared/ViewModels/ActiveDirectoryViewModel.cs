using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.ADIntegration.ViewModels
{
    public class ActiveDirectoryViewModel
    {
        public DateTime? accountExpirationDate { get; set; }
        public DateTime? accountLockoutTime { get; set; }
        public int badLogonCount { get; set; }
        public string description { get; set; }
        public string displayName { get; set; }
        public string distinguishedName { get; set; }
        public string domain { get; set; }
        public string emailAddress { get; set; }
        public string employeeId { get; set; }
        public bool? enabled { get; set; }
        public string givenName { get; set; }
        public Guid? guid { get; set; }
        public string homeDirectory { get; set; }
        public string homeDrive { get; set; }
        public DateTime? lastBadPasswordAttempt { get; set; }
        public DateTime? lastLogon { get; set; }
        public DateTime? lastPasswordSet { get; set; }
        public string middleName { get; set; }
        public string name { get; set; }
        public bool passwordNeverExpires { get; set; }
        public bool passwordNotRequired { get; set; }
        public string samAccountName { get; set; }
        public string scriptPath { get; set; }
        public string surname { get; set; }
        public bool userCannotChangePassword { get; set; }
        public string userPrincipalName { get; set; }
        public string voiceTelephoneNumber { get; set; }
    }
}
