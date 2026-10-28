using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.ADIntegration.ViewModels
{
    public class ActiveDirectoryUser
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
        public string manager { get; set; }
        public SecurityIdentifier sID { get; set; }
        public static ActiveDirectoryUser CastToAdUser(UserPrincipal user, string Manager)
        {
            return new ActiveDirectoryUser
            {
                accountExpirationDate = user.AccountExpirationDate,
                accountLockoutTime = user.AccountLockoutTime,
                badLogonCount = user.BadLogonCount,
                description = user.Description,
                displayName = user.DisplayName,
                distinguishedName = user.DistinguishedName,
                emailAddress = user.EmailAddress,
                employeeId = user.EmployeeId,
                enabled = user.Enabled,
                givenName = user.GivenName,
                guid = user.Guid,
                homeDirectory = user.HomeDirectory,
                homeDrive = user.HomeDrive,
                lastBadPasswordAttempt = user.LastBadPasswordAttempt,
                lastLogon = user.LastLogon,
                lastPasswordSet = user.LastPasswordSet,
                middleName = user.MiddleName,
                name = user.Name,
                passwordNeverExpires = user.PasswordNeverExpires,
                passwordNotRequired = user.PasswordNotRequired,
                samAccountName = user.SamAccountName,
                scriptPath = user.ScriptPath,
                sID = user.Sid,
                surname = user.Surname,
                userCannotChangePassword = user.UserCannotChangePassword,
                userPrincipalName = user.UserPrincipalName,
                voiceTelephoneNumber = user.VoiceTelephoneNumber,
                manager = Manager
            };
        }

        public string GetDomainPrefix() => distinguishedName
            .Split(',')
            .FirstOrDefault(x => x.ToLower().Contains("dc"))
            .Split('=')
            .LastOrDefault()
            .ToUpper();
    }
}
