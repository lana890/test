using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;
using Newtonsoft.Json;

namespace EskaDCMSActiveDirectory.Models
{
    public class LoginViewModel
    {
        public LoginViewModel(string Username,string Password)
        {
            this.Username = Username;       
            this.Password = "Eska@"+Password;       
        }

        public string Username { get; set; }

        public string Password { get; set; }

        public IList<long?> AuthorizedGroups { get; set; } = new List<long?>();


        public int LoginType { get; set; } = 0;


        public bool FromGoogleOrFacebookOrApple { get; set; } = false;
        public bool FromActiveDirectory { get; set; } = true;

        public string SerializeObject()

        {
           return JsonConvert.SerializeObject(this);
        }

    }

    public class CoreLoginViewModel
    {
        public CoreLoginViewModel(string Username)
        {
            this.Username = Username;
            this.BranchId = 1;
            this.CompanyId = 1;
            this.IncludeRefreshToken = true;
        }

        public string Username { get; set; }


        public int CompanyId { get; set; }
        public int BranchId { get; set; }
        public bool IncludeRefreshToken { get; set; }

        public string SerializeObject()

        {
            return JsonConvert.SerializeObject(this);
        }

    }

    public class UserRoleVM
    {
        public long RoleId { get; set; }
        public string RoleName { get; set; }
    }
    public class TokenResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class UserOptionsVM
    {
        public string PropertyName { get; set; }
        public string Value { get; set; }
        public int ControlType { get; set; }
        public bool IsEditable { get; set; }
        public bool IsVisible { get; set; }
        public long Id { get; set; }
    }
    public class CoreLoginResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string SessionId { get; set; }
    }

    public class LoginResponse
    {
        public TokenResponse Token { get; set; }
        public string Culture { get; set; }
        public IList<UserOptionsVM> UserAdditionData { get; set; }
        public string Email { get; set; }
        public string ImageUrl { get; set; }
        public IList<UserRoleVM> Role { get; set; }
    }
    public class loginRequestViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RefreshCoreTokenVM
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string SessionId { get; set; }

    }
}
