using AppleAuth.Constants;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AppleAuth.Cryptography
{
    /// <summary>
    /// Class that manages generation of JWT tokens.
    /// </summary>
    public class TokenGenerator
    {
        private static readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly IRepository<DCMSLog> repository;

        /// <summary>
        /// Generates the signed JSON Web Token
        /// </summary>
        /// <param name="privateKey"></param>
        /// <param name="teamId"></param>
        /// <param name="clientId"></param>
        /// <param name="keyId"></param>
        /// <param name="expiration"></param>
        /// <returns></returns>
        public string GenerateAppleClientSecret(IRepository<DCMSLog> repository,string privateKey, string teamId, string clientId, string keyId, int expiration = 5)
        {
            try
            {

            var key = GetFormattedPrivateKey(privateKey);

                repository.Add(new DCMSLog
                {

                    AdditionInfo = JsonConvert.SerializeObject(key),
                    Channel = EskaCMS.Entities.Enum.GeneralEnums.EPlatformSource.Admin,
                    SiteId = 3,
                    UserId = 2,
                    Date = DateTimeOffset.Now,
                    LogType = EskaCMS.Entities.Enum.GeneralEnums.LogTypes.Information,



                });
                 repository.SaveChanges();
                var ecDsaCng = ECDsa.Create();


                repository.Add(new DCMSLog
                {

                    AdditionInfo = JsonConvert.SerializeObject(ecDsaCng),
                    Channel = EskaCMS.Entities.Enum.GeneralEnums.EPlatformSource.Admin,
                    SiteId = 3,
                    UserId = 2,
                    Date = DateTimeOffset.Now,
                    LogType = EskaCMS.Entities.Enum.GeneralEnums.LogTypes.Information,



                });
                repository.SaveChanges();
                repository.Add(new DCMSLog
                {

                    AdditionInfo = JsonConvert.SerializeObject("Before ImportPkcs8PrivateKey"),
                    Channel = EskaCMS.Entities.Enum.GeneralEnums.EPlatformSource.Admin,
                    SiteId = 3,
                    UserId = 2,
                    Date = DateTimeOffset.Now,
                    LogType = EskaCMS.Entities.Enum.GeneralEnums.LogTypes.Information,



                });
                repository.SaveChanges();
                ecDsaCng.ImportPkcs8PrivateKey(Convert.FromBase64String(key), out var _);


                repository.Add(new DCMSLog
                {

                    AdditionInfo = JsonConvert.SerializeObject("After ImportPkcs8PrivateKey"),
                    Channel = EskaCMS.Entities.Enum.GeneralEnums.EPlatformSource.Admin,
                    SiteId = 3,
                    UserId = 2,
                    Date = DateTimeOffset.Now,
                    LogType = EskaCMS.Entities.Enum.GeneralEnums.LogTypes.Information,



                });
                repository.SaveChanges();
                var signingCredentials = new SigningCredentials(
              new ECDsaSecurityKey(ecDsaCng), SecurityAlgorithms.EcdsaSha256);


                repository.Add(new DCMSLog
                {

                    AdditionInfo = JsonConvert.SerializeObject("After signingCredentials"),
                    Channel = EskaCMS.Entities.Enum.GeneralEnums.EPlatformSource.Admin,
                    SiteId = 3,
                    UserId = 2,
                    Date = DateTimeOffset.Now,
                    LogType = EskaCMS.Entities.Enum.GeneralEnums.LogTypes.Information,



                });
                repository.SaveChanges();
                var now = DateTime.UtcNow;

            var expiresInMinutes = expiration > 5 ? now.AddMinutes(expiration) : now.AddMinutes(5);

            var claims = new List<Claim>
            {
                new Claim(ClaimConstants.Issuer, teamId),
                new Claim(ClaimConstants.IssuedAt, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64),
                new Claim(ClaimConstants.Expiration, EpochTime.GetIntDate(expiresInMinutes).ToString(), ClaimValueTypes.Integer64),
                new Claim(ClaimConstants.Audience, "https://appleid.apple.com"),
                new Claim(ClaimConstants.Sub, clientId)
            };

            var token = new JwtSecurityToken(
                issuer: teamId,
                claims: claims,
                expires: expiresInMinutes,
                signingCredentials: signingCredentials);
             
            token.Header.Add(ClaimConstants.KeyID, keyId);

            return _tokenHandler.WriteToken(token);


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// Verify if the token that Apple has sent is valid and genuine.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="clientId"></param>
        public static void VerifyAppleIDToken(string token, string clientId)
        {
            try
            {

            var deserializedToken = _tokenHandler.ReadJwtToken(token);
            var claims = deserializedToken.Claims;

            SecurityKey publicKey;

            var expClaim = claims.FirstOrDefault(x => x.Type == ClaimConstants.Expiration).Value;
            var expirationTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).DateTime;

            if (expirationTime < DateTime.UtcNow)
            {
                throw new SecurityTokenExpiredException("Expired token");
            }

            var applePublicKeys = _httpClient.GetAsync("https://appleid.apple.com/auth/keys");
            var keyset = new JsonWebKeySet(applePublicKeys.Result.Content.ReadAsStringAsync().Result);

            publicKey = keyset.Keys.FirstOrDefault(x => x.Kid == deserializedToken.Header.Kid);

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = "https://appleid.apple.com",
                IssuerSigningKey = publicKey,
                ValidAudience = clientId
            };

            _tokenHandler.ValidateToken(token, validationParameters, out var _);

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// Removes empty lines from string; Also removes lines like the following:"-----BEGIN PRIVATE KEY-----"
        /// </summary>
        public string GetFormattedPrivateKey(string keyString)
        {
            StringBuilder cleanedKey = new StringBuilder();
            var keyLines = keyString.Split(GlobalConstants.NewLineSeparators, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < keyLines.Length; i++)
            {
                if (!keyLines[i].Contains("PRIVATE"))
                {
                    cleanedKey.Append(keyLines[i]);
                }
            }

            return cleanedKey.ToString();
        }
    }
}
