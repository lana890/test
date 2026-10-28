using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.DataAccess.Data;


namespace EskaCMS.Core.Extensions
{
    public class CMSRoleStore : RoleStore<Role, EskaDCMSDbContext, long, UserRole, IdentityRoleClaim<long>>
    {
        public CMSRoleStore(EskaDCMSDbContext context) : base(context)
        {
        }
    }
    //public class CMSRoleStore : RoleStore<Role, EskaCMSDbContext, long, UserRole, IdentityRoleClaim<long>>
    //{
    //    public CMSRoleStore(EskaCMSDbContext context) : base(context)
    //    {
    //    }
    //}
    //public class CMSMySqlRoleStore : RoleStore<Role, EskaCMSMySqlDbContext, long, UserRole, IdentityRoleClaim<long>>
    //{
    //    public CMSMySqlRoleStore(EskaCMSMySqlDbContext context) : base(context)
    //    {
    //    }
    //}
}
