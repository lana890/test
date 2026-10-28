using EskaCMS.Core.BusinessModels;

using System;
using System.Collections;
using System.Collections.Generic;
using static EskaCMS.Core.Models.TokenVM;

namespace EskaCMS.Security.Models
{
    public class UsernameVM
    {
        public string Username { get; set; }
    }
    public class FingerVM
    {
        public long  UserId { get; set; }
        public string Signature { get; set; }
        public string Payload { get; set; }
    }
    public class FingerPublicVM
    {
        public string PublicKey { get; set; }
    }
    public class UserTokenResponse
    {
        public string EncryptedId { get; set; }
        public long Id { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string EncodedPhoneNumber { get; set; }
        public string EncodedEmail { get; set; }
        public string Image { get; set; }

        public string Username { get; set; }
        public DateTimeOffset? DateOfBirth { get; set; }
    }

    public class UserOption
    {
        public List<UserOptionsVM> AdditionalData { get; set; }
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
    public class UserAdditionData
    {
        public string PropertyName { get; set; }
        public string Value { get; set; }
    }
    public class loginRequestAdViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public class DCMSToken
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
    public class UserRoleVM
    {
        public long RoleId { get; set; }
        public string RoleName { get; set; }
    }
    public class UserAdditionDataVM
    {
        public string Value { get; set; }
        public string PropertyName { get; set; }
    }

}

//using EskaCMS.Core.BusinessModels;
//using EskaCMS.Entities.Entity;
//using EskaCMS.Infrastructure.Models;
//using System;
//using System.Collections;
//using System.Collections.Generic;

//namespace EskaCMS.Security.Models
//{
//    public class UserTokenResponse
//    {
//        public string FullName { get; set; }
//        public string PhoneNumber { get; set; }
//        public string Email { get; set; }
//        public string EncodedPhoneNumber { get; set; }
//        public string EncodedEmail { get; set; }
//        public string Image { get; set; }

//        public string Username { get; set; }
//        public DateTimeOffset? DateOfBirth { get; set; }
//    }
//    public class UserResponse
//    {
//        //public string fullName { get; set; }
//        //public string phoneNumber { get; set; }
//        //public string email { get; set; }
//        //public string image { get; set; }
//        //public long id { get; set; }
//        //public int MyProperty { get; set; }
//        public List<UserOptionsVM> AdditionalData { get; set; }
//    }

//    //public class UserOption
//    //{
//    //    public string PropertyName { get; set; }
//    //    public string Value { get; set; }
//    //}
//    public class LoginResponse
//    {

//        public RoleOptionsViewModel RoleOptions { get; set; }
//        // public long roleId { get; set; }
//        // public string role { get; set; }
//        public string culture { get; set; }
//        // public IList<string> roleOptions { get; set; }
//        public IList<string> defaultSiteCultures { get; set; }
//        public UsersSites defaultSiteInfo { get; set; }
//    }

//}