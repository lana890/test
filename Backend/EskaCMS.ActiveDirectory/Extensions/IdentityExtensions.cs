using EskaDCMSActiveDirectory.Models;
using EskaDCMSActiveDirectory.Services;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;

namespace EskaDCMSActiveDirectory.Extensions
{
    public static class IdentityExtensions
    {
        public static IQueryable<UserPrincipal> FilterUsers(this IQueryable<UserPrincipal> principals) =>
            principals.Where(x => x.Guid.HasValue);

        public static IQueryable<ActiveDirectoryUser> SelectAdUsers(this IQueryable<UserPrincipal> principals) =>
            principals.Select(x => ActiveDirectoryUser.CastToAdUser(x,null));
    }
}

