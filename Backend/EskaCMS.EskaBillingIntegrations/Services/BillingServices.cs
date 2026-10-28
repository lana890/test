using AutoMapper;
using AutoMapper.QueryableExtensions;
using EskaCMS.Core.Entities;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.EskaBilling.Models;
using EskaCMS.EskaBilling.Models.response;
using EskaCMS.EskaBilling.Services.Interfaces;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Services
{
    public class BillingServices : IBillingServices
    {

        private readonly IWorkContext _workContext;
        private readonly IRepository<CustomerGroup> _customerGroup;
        private readonly IRepository<CustomerGroupDetails> customergroupDetailsRepository;
        private readonly IRepositoryWithCustomId<CustomerGroupUser> _customerGroupUser;

        public BillingServices(IWorkContext workContext ,
                            IRepository<CustomerGroup> customerGroup, IRepository<CustomerGroupDetails> customergroupDetailsRepository,

                            IRepositoryWithCustomId<CustomerGroupUser> customerGroupUser )
        {
            _workContext = workContext;
            _customerGroup = customerGroup;
            this.customergroupDetailsRepository = customergroupDetailsRepository;
            _customerGroupUser = customerGroupUser;
        }

        public async Task<bool> CheckUserGroup(List<BillingGroupVm> billingGroupVm)
        {
            var userId = await _workContext.GetCurrentUserId();
            var siteId =  _workContext.GetCurrentSiteId();
            var CultureId =  _workContext.GetCurrentCultureId();

            var allAdditionalInfo = billingGroupVm.Select(x=>x.AdditionalInfo.ToLower()).ToList(); 

            var alluserGroups = _customerGroup.Query()
                                              .Where(x => x.SiteId == siteId && allAdditionalInfo.Contains(x.AdditionalInfo.ToLower()) && !x.IsDeleted)
                                              .AsNoTracking()
                                              .Include(x => x.Users)
                                              .ToList();

            foreach (var group in billingGroupVm)
            {

                var lowerAdditionalInfo = group.AdditionalInfo.ToLower();

                var userGroup = alluserGroups.Where(x => x.AdditionalInfo.ToLower() == lowerAdditionalInfo).FirstOrDefault();

                if (userGroup == null)
                {
                    var customerGroupToBeAdded = new CustomerGroup
                    {
                        AdditionalInfo = group.AdditionalInfo,
                        FireBaseTopic = group.GroupName,
                        SiteId = siteId,
                        IsActive = true
                    };

                    _customerGroup.Add(customerGroupToBeAdded);
                    _customerGroup.SaveChanges();

                    await AddGroupDetails(group.GroupName, customerGroupToBeAdded.Id);
                    await AddUserToGroup(userId, customerGroupToBeAdded.Id);

                }
                else if (!userGroup.Users.Any(x => x.UserId == userId))
                {
                    await AddUserToGroup(userId, userGroup.Id);             
                }
            }


            return true;

        }

        private async Task AddGroupDetails(string groupName, long id)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var CustomerGroupDetails = new CustomerGroupDetails
                {

                    CultureId = CultureId,
                    Description = groupName,
                    Name = groupName,
                    CustomerGroupId = id


                };
                customergroupDetailsRepository.Add(CustomerGroupDetails);
                await customergroupDetailsRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> AddUserToGroup(long userId , long customerGroupId)
        {

            var addedUserToGroup = new CustomerGroupUser { CustomerGroupId = customerGroupId, UserId = userId };
            _customerGroupUser.Add(addedUserToGroup);
            _customerGroupUser.SaveChanges();

            return true;
        }




    }
}
