using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.Interfaces
{
    public interface ICustomerGroup
    {

        Task<SmartTableResult<CustomerGroupVM>> List(SmartTableParam param);
        Task<List<CustomerGroupVM>> Get(long? SiteId = null);
        Task<List<UserGroupContactInfoVM>> GetUserGroupContactInfo(long SiteId);
        Task<bool> JoinGroup(long GroupId);
        Task<bool> LeaveGroup(long GroupId);
        Task<bool> AddBulkUserToGroup(AddUserToGroupDto model);
        Task<List<CustomerGroupVM>> GetCustomerGroupByType(long groupTypeId);
        Task<List<CustomerGroupVM>> GetGroupByTypeAndUser(long? groupTypeId, string groupName);
        Task<PaginationResult<UserGroupVm>> GetUsersByGroupIdWithFilters(long groupId, SmartTableParam model);
        Task<PaginationResult<CustomerGroupForm>> GetUnAssignedGroups(SmartTableParam model);
        Task<long> Post(CustomerGroupForm model);
        Task<long> Put([FromBody] CustomerGroupForm model);
    }
}
