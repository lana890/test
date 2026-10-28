using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System;
using EskaDCMSActiveDirectory.Models;

namespace EskaDCMSActiveDirectory.Services
{
    public class ActiveDirectoryUser
    {
      
        public string description { get; set; }
        public string displayName { get; set; }
        public string emailAddress { get; set; }
   
        public string samAccountName { get; set; }
        public string DistinguishedName { get; set; }

        public string surname { get; set; }
        public string voiceTelephoneNumber { get; set; }
        public string manager { get; set; }
        public SecurityIdentifier sID { get; set; }
        public static ActiveDirectoryUser CastToAdUser(UserPrincipal user, string Manager)
        {
            return new ActiveDirectoryUser
            {
             
                description = user.Description,
                DistinguishedName = user.DistinguishedName,

                displayName = user.DisplayName,
                emailAddress = user.EmailAddress,
                samAccountName = user.SamAccountName,
                sID = user.Sid,
                surname = user.Surname,
                voiceTelephoneNumber = user.VoiceTelephoneNumber,
                manager = Manager
            };
        }

        //public string GetDomainPrefix() => distinguishedName
        //    .Split(',')
        //    .FirstOrDefault(x => x.ToLower().Contains("dc"))
        //    .Split('=')
        //    .LastOrDefault()
        //    .ToUpper();
    }

    public class HREmployee
    {
        public int? ID { get; set; }
        public int? MANAGER_ID { get; set; }
        public string NAME { get; set; }
        public string USERNAME { get; set; }

    }
    public class CoreResponse<T>
    {
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsError { get; set; }

    }

    public class DCMSResponse<T>
    {
        public T content { get; set; }
        public string code { get; set; }
        public string message { get; set; }

    }
    public class GetCurrentADUserVM
    {

    }
    public class LoginWithADVM 
    {
        public string Manager { get; set; }
        public string Username { get; set; }
        public CoreLoginResponse CoreToken { get; set; }
        public string FullName { get; set; }
        public LoginResponse LoginResponse { get; set; }
        public LoginWithADVM()
        {
                
        }
        public  LoginWithADVM(ActiveDirectoryUser CurrentUser, DCMSResponse<LoginResponse> LoginResponse)
        {
            this.FullName = CurrentUser.displayName;
            this.Manager = CurrentUser.manager;
            this.Username = CurrentUser.samAccountName;
            this.LoginResponse = LoginResponse.content;
        }

    }
}
