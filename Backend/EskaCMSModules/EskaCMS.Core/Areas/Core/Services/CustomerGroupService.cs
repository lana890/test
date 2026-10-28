using EskaCMS.Core.Areas.Core.Interfaces;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Areas.Core.ViewModels.GroupTypeModels.Response;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using EskaCMS.Core.Services.Interfaces;

namespace EskaCMS.Core.Areas.Core.Services
{
    public class CustomerGroupService : ICustomerGroup
    {


        private readonly IRepository<CustomerGroup> _CustomerGroupRepository;
        private readonly IAlertServices alertServices;
        private readonly IRepository<User> userRepository;
        private readonly IRepository<CustomerGroupDetails> customergroupDetailsRepository;
        private readonly IWorkContext _workContext;
        private readonly IRepositoryWithCustomId<CustomerGroupUser> _customerGroupUserRepository;
        public CustomerGroupService(IRepository<CustomerGroup> CustomerGroupRepository, IAlertServices alertServices, IRepository<User> userRepository, IRepository<CustomerGroupDetails> customergroupDetailsRepository, IRepositoryWithCustomId<CustomerGroupUser> customerGroupUserRepository, IWorkContext workContext)
        {
            _CustomerGroupRepository = CustomerGroupRepository;
            this.alertServices = alertServices;
            this.userRepository = userRepository;
            this.customergroupDetailsRepository = customergroupDetailsRepository;
            _customerGroupUserRepository = customerGroupUserRepository;
            _workContext = workContext;
        }


        public async Task<SmartTableResult<CustomerGroupVM>> List(SmartTableParam param)
        {
            try
            {
                var cultureId = _workContext.GetCurrentCultureId();

                var query = _CustomerGroupRepository.Query()
                    .Include(x => x.customerGroupDetails)
                    .ThenInclude(c => c.Culture)
                    .Include(x => x.Thumbnail)
                    .Where(x => !x.IsDeleted);

                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;

                    if (search.Name != null)
                    {
                        string name = search.Name;
                        query = query.Where(x => x.customerGroupDetails.Any(p => p.Name.Contains(name)));
                    }

                    if (search.GroupTypeId != null)
                    {
                        long GroupTypeId = search.GroupTypeId;
                        query = query.Where(x => x.GroupTypeId == GroupTypeId);
                    }

                    if (search.CreatedOn != null)
                    {
                        if (search.CreatedOn.before != null)
                        {
                            DateTimeOffset before = search.CreatedOn.before;
                            query = query.Where(x => x.CreatedOn <= before);
                        }

                        if (search.CreatedOn.after != null)
                        {
                            DateTimeOffset after = search.CreatedOn.after;
                            query = query.Where(x => x.CreatedOn >= after);
                        }
                    }
                }

                var customerGroups = query.ToSmartTableResult(
                    param,
                    x => new CustomerGroupVM
                    {
                        Id = x.Id,
                        Name = x.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                        Description = x.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                        customerGroupDetails = x.customerGroupDetails.Select(x => new GetCustomerGroupDetailsForm
                        {
                            Description = x.Description,
                            CultureId = x.CultureId.Value,
                            Name = x.Name,
                            CultureCode = x.Culture.CultureId


                        }).ToList(),
                        IsActive = x.IsActive,
                        CreatedOn = x.CreatedOn,
                        Email = x.Email,
                        MaxSessions = x.MaxSessionsNumber,
                        PasswordExpiryInDays = x.PasswordExpiryInDays,
                        EnableTwoFactorAuthentication = x.EnableTwoFactorAuthentication,
                        ThumbnailId = x.ThumbnailId,
                        ImageUrl = x.ThumbnailId.HasValue ? x.Thumbnail.PublicUrl : null,
                        GroupType = x.GroupTypeId.HasValue ? new GroupTypeGridVM { Id = x.GroupTypeId.Value, Name = x.GroupType.Name, Description = x.GroupType.Description, Status = x.GroupType.Status } : null,
                        Topic = x.FireBaseTopic,
                        AdditionalInfo = x.AdditionalInfo
                    });

                return customerGroups;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<long> Post(CustomerGroupForm model)
        {
            try
            {
                long SiteId = await _workContext.GetCurrentSiteIdAsync();

                var customerGroup = new CustomerGroup
                {
                    IsActive = model.IsActive,
                    FireBaseTopic = model.FireBaseTopic,
                    SiteId = SiteId,
                    Email = model.Email,
                    MaxSessionsNumber = model.MaxSessions,
                    PasswordExpiryInDays = model.PasswordExpiryInDays,
                    EnableTwoFactorAuthentication = model.EnableTwoFactorAuthentication.HasValue ? model.EnableTwoFactorAuthentication.Value : false,
                    GroupTypeId = model.GroupTypeId == 0 ? null : model.GroupTypeId,
                    ThumbnailId = model.ThumbnailId,


                };

                _CustomerGroupRepository.Add(customerGroup);
                await _CustomerGroupRepository.SaveChangesAsync();

                var CustomerGroupDetails = model.customerGroupDetails.ConvertAll(c => new CustomerGroupDetails
                {

                    CultureId = c.CultureId,
                    Description = c.Description,
                    Name = c.Name,
                    CustomerGroupId = customerGroup.Id


                }).Where(x => !string.IsNullOrEmpty(x.Name)).ToList();
                customergroupDetailsRepository.AddRange(CustomerGroupDetails);
                await customergroupDetailsRepository.SaveChangesAsync();
                return customerGroup.Id;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }




        }


        public async Task<long> Put([FromBody] CustomerGroupForm model)
        {
            try
            {


                var customerGroup = await _CustomerGroupRepository.Query().FirstOrDefaultAsync(x => x.Id == model.Id);
                customerGroup.IsActive = model.IsActive;
                customerGroup.LatestUpdatedOn = DateTimeOffset.Now;
                customerGroup.FireBaseTopic = model.FireBaseTopic;
                customerGroup.Email = model.Email;
                customerGroup.MaxSessionsNumber = model.MaxSessions;
                customerGroup.EnableTwoFactorAuthentication = model.EnableTwoFactorAuthentication.HasValue ? model.EnableTwoFactorAuthentication.Value : false;
                customerGroup.PasswordExpiryInDays = model.PasswordExpiryInDays;
                customerGroup.GroupTypeId = model.GroupTypeId == 0 ? null : model.GroupTypeId;
                customerGroup.ThumbnailId = model.ThumbnailId;

                await _CustomerGroupRepository.SaveChangesAsync();

                await UpdateUserTwoFactorAuthentication(customerGroup.Id, customerGroup.EnableTwoFactorAuthentication);
                await UpdateCustomerGroupDetails(model.customerGroupDetails, model.Id.Value);

                return customerGroup.Id;

            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }
        public async Task<PaginationResult<UserGroupVm>> GetUsersByGroupIdWithFilters(long groupId, SmartTableParam model)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var query = _customerGroupUserRepository.Query()
                                                     .Where(x => x.CustomerGroupId == groupId && (x.CustomerGroup.SiteId == siteId))
                                                     .Include(x => x.User)
                                                     .AsNoTracking();


            if (model.Search.PredicateObject != null)
            {
                dynamic search = model.Search.PredicateObject;



                if (search.UserName != null)
                {
                    string UserName = search.UserName;
                    query = query.Where(x => x.User.NormalizedUserName.Contains(UserName.ToUpper()));
                }

                if (search.Email != null)
                {
                    string Email = search.Email;
                    query = query.Where(x => x.User.NormalizedEmail.Contains(Email.ToUpper()));
                }

                if (search.FullName != null)
                {
                    string FullName = search.FullName;
                    query = query.Where(x => x.User.FullName.ToUpper().Contains(FullName.ToUpper()));
                }

            }



            var result = await query.Select(x => new UserGroupVm
            {
                Email = x.User.Email,
                GroupId = x.CustomerGroupId,
                EncryptedUserId = EncryptDecryptHelper.Encrypt(x.UserId.ToString()),
                Image = x.User.Image,
                UserId = x.UserId,
                FullName = x.User.FullName,
                UserName = x.User.UserName,
            }).PaginateAsync(model.Pagination.Number, model.Pagination.Start);

            return result;
        }


        public async Task<PaginationResult<CustomerGroupForm>> GetUnAssignedGroups(SmartTableParam model)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();
            var query = _CustomerGroupRepository.Query().Include(x => x.customerGroupDetails)
                                                     .ThenInclude(x => x.Culture)
                                                     .Where(x => !x.Users.Any(x => x.UserId == userId) && x.SiteId == siteId && !x.IsDeleted)
                                                     .AsNoTracking();
            var cultureId = _workContext.GetCurrentCultureId();


            if (model.Search.PredicateObject != null)
            {
                dynamic search = model.Search.PredicateObject;



                if (search.Name != null)
                {
                    string Name = search.Name;
                    query = query.Where(x => x.customerGroupDetails.Any(i => i.Name.ToUpper().Contains(Name.ToUpper())));
                }

                if (search.Description != null)
                {
                    string Description = search.Description;
                    query = query.Where(x => x.customerGroupDetails.Any(i => i.Description.ToUpper().Contains(Description.ToUpper())));
                }

                if (search.GroupTypeId != null)
                {
                    long GroupTypeId = search.GroupTypeId;
                    query = query.Where(x => x.GroupTypeId == GroupTypeId);
                }

            }


            var customerGroups = await query.ToListAsync();

            var result = await customerGroups.Select(x => new CustomerGroupForm
            {
                Id = x.Id,
                Email = x.Email,
                GroupTypeId = x.GroupTypeId,
                Name = x.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                Description = x.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                MaxSessions = x.MaxSessionsNumber,
                customerGroupDetails = x.customerGroupDetails.Select(x => new CustomerGroupDetailsForm
                {
                    Description = x.Description,
                    CultureId = x.CultureId.Value,
                    Name = x.Name,
                    CultureCode = x.Culture.CultureId



                }).ToList(),
                PasswordExpiryInDays= x.PasswordExpiryInDays,
                EnableTwoFactorAuthentication = x.EnableTwoFactorAuthentication,
                FireBaseTopic = x.FireBaseTopic,
                ImageUrl = x.ThumbnailId.HasValue ? x.Thumbnail.PublicUrl : null,
                IsActive = x.IsActive,


            }).PaginateAsync(model.Pagination.Number, model.Pagination.Start);

            return result;
        }

        public async Task<List<CustomerGroupVM>> Get(long? SiteId = null)
        {
            try
            {
                var cultureId = _workContext.GetCurrentCultureId();

                var customerGroups = await _CustomerGroupRepository.Query().Include(x => x.customerGroupDetails).ThenInclude(x => x.Culture)
                    .Where(x => !x.IsDeleted && (SiteId == null || x.SiteId == SiteId || x.SiteId == null))
                    .Include(x => x.Thumbnail)
                 .Select(x => new CustomerGroupVM
                 {
                     Id = x.Id,
                     Name = x.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                     Description = x.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),

                     Topic = x.FireBaseTopic,
                     Email = x.Email,
                     IsActive = x.IsActive,
                     ThumbnailId = x.ThumbnailId,
                     ImageUrl = x.ThumbnailId.HasValue ? x.Thumbnail.PublicUrl : null,
                     MaxSessions = x.MaxSessionsNumber,
                     AdditionalInfo = x.AdditionalInfo,
                     CreatedOn = x.CreatedOn,
                     customerGroupDetails = x.customerGroupDetails.Select(x => new GetCustomerGroupDetailsForm
                     {
                         Description = x.Description,
                         CultureId = x.CultureId.Value,
                         Name = x.Name,
                         CultureCode = x.Culture.CultureId



                     }).ToList(),
                     PasswordExpiryInDays = x.PasswordExpiryInDays,
                     EnableTwoFactorAuthentication = x.EnableTwoFactorAuthentication,
                     GroupType = x.GroupTypeId.HasValue ? new GroupTypeGridVM { Id = x.GroupTypeId.Value, Name = x.GroupType.Name, Description = x.GroupType.Description, Status = x.GroupType.Status } : null


                 })
                 .ToListAsync();

                return customerGroups;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<UserGroupContactInfoVM>> GetUserGroupContactInfo(long SiteId)
        {
            try
            {
                var cultureId = _workContext.GetCurrentCultureId();

                var UserGroup = await _CustomerGroupRepository.Query().Include(x => x.customerGroupDetails).Where(x => x.SiteId == SiteId && x.Email != null)
                    .Select(x => new UserGroupContactInfoVM
                    {
                        Id = x.Email,

                        Value = x.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault()

                    }).ToListAsync();


                return UserGroup;


            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<bool> JoinGroup(long GroupId)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var userId = await _workContext.GetCurrentUserId();


                var group = _customerGroupUserRepository.Query()
                                                        .Where(x => x.UserId == userId && x.CustomerGroupId == GroupId)
                                                        .FirstOrDefault();



                if (group != null)
                    throw new Exception("User Already join in this group");


                var userGroup = new CustomerGroupUser
                {
                    UserId = userId,
                    CustomerGroupId = GroupId,
                    CreationDate=DateTimeOffset.Now,
                };

                _customerGroupUserRepository.Add(userGroup);
                _customerGroupUserRepository.SaveChanges();


                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<bool> LeaveGroup(long GroupId)
        {
            try
            {

                var siteId = _workContext.GetCurrentSiteId();
                var userId = await _workContext.GetCurrentUserId();


                var group = _customerGroupUserRepository.Query()
                                                        .Where(x => x.UserId == userId && x.CustomerGroupId == GroupId)
                                                        .FirstOrDefault();

                if (group == null)
                    throw new Exception("User didn't join this group");



                _customerGroupUserRepository.Remove(group);
                _customerGroupUserRepository.SaveChanges();


                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<bool> AddBulkUserToGroup(AddUserToGroupDto model)
        {
            try
            {

                var siteId = _workContext.GetCurrentSiteId();
                var User =await _workContext.GetCurrentUser();

                var customergroup = await  _CustomerGroupRepository.Query()
                                                              .Include(x=>x.customerGroupDetails)
                                                              .Where(x => x.Id == model.CustomerGroupId
                                                              ).FirstOrDefaultAsync();
                var usersAlreadyInGroup = _customerGroupUserRepository.Query()
                                                        .Where(x => x.CustomerGroup.SiteId == siteId && x.CustomerGroupId == model.CustomerGroupId)
                                                        .Select(x => x.UserId)
                                                        .ToList();

                var userIdsToBeAdded = model.UserIds.Where(x => usersAlreadyInGroup.Any() ? !usersAlreadyInGroup.Contains(x) : true)
                                                    .ToList();

                var newUsers = userIdsToBeAdded.Select(x => new CustomerGroupUser
                {
                    CustomerGroupId = model.CustomerGroupId,
                    UserId = x,
                    CreationDate = DateTimeOffset.Now,
                }).ToList();

                _customerGroupUserRepository.AddRange(newUsers);

                await _customerGroupUserRepository.SaveChangesAsync();
                try
                {
                    if (userIdsToBeAdded.Count() > 0)
                    {
                        var GroupName = customergroup.customerGroupDetails.Where(x=>!string.IsNullOrEmpty( x.Name)).Select(x => x.Name).FirstOrDefault();
                        await alertServices.SendRealTimeAlert(userIdsToBeAdded.ToArray(), GroupName ?? "",model.CustomerGroupId, User.FullName,User.UserName,customergroup.GroupTypeId.HasValue? customergroup.GroupTypeId.Value:0);
                    }
                }
                catch (Exception ex)
                {

                }
                return true;



            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<List<CustomerGroupVM>> GetCustomerGroupByType(long groupTypeId)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var cultureId = _workContext.GetCurrentCultureId();

                var customerGroups = await _CustomerGroupRepository.Query().Include(x => x.customerGroupDetails)
                    .Where(x => !x.IsDeleted && x.GroupTypeId == groupTypeId && x.GroupType.SiteId == siteId)
                    .Include(x => x.Thumbnail)
                  .Select(x => new CustomerGroupVM
                  {
                      Id = x.Id,
                      Name = x.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                      Description = x.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                      Topic = x.FireBaseTopic,
                      Email = x.Email,
                      IsActive = x.IsActive,
                      ThumbnailId = x.ThumbnailId,
                      ImageUrl = x.ThumbnailId.HasValue ? x.Thumbnail.PublicUrl : null,
                      MaxSessions = x.MaxSessionsNumber,
                      PasswordExpiryInDays = x.PasswordExpiryInDays,
                      EnableTwoFactorAuthentication = x.EnableTwoFactorAuthentication,
                      GroupType = x.GroupTypeId.HasValue ? new GroupTypeGridVM { Id = x.GroupTypeId.Value, Name = x.GroupType.Name, Description = x.GroupType.Description, Status = x.GroupType.Status } : null,
                      AdditionalInfo = x.AdditionalInfo

                  })
                 .ToListAsync();

                return customerGroups;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<CustomerGroupVM>> GetGroupByTypeAndUser(long? groupTypeId, string groupName)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var userId = await _workContext.GetCurrentUserId();
                var cultureId = _workContext.GetCurrentCultureId();


                var groups = await _customerGroupUserRepository.Query()
                                                        .AsNoTracking()
                                                        .Include(x => x.CustomerGroup)
                                                        .Include("CustomerGroup.customerGroupDetails")
                                                        .Include("CustomerGroup.customerGroupDetails.Culture")
                                                        .Include("CustomerGroup.Thumbnail")
                                                        .Where(x => x.UserId == userId && x.CustomerGroup.SiteId == siteId && (groupTypeId.HasValue ? x.CustomerGroup.GroupTypeId == groupTypeId : true))
                                                        .Where(x => !string.IsNullOrEmpty(groupName) ? x.CustomerGroup.customerGroupDetails.Any(v => v.Name.ToLower().Contains(groupName.ToLower())) : true)
                                                        .Select(x => new CustomerGroupVM
                                                        {
                                                            Id = x.CustomerGroup.Id,
                                                            Name = x.CustomerGroup.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                                                            Description = x.CustomerGroup.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                                                            Topic = x.CustomerGroup.FireBaseTopic,
                                                            Email = x.CustomerGroup.Email,
                                                            IsActive = x.CustomerGroup.IsActive,
                                                            ThumbnailId = x.CustomerGroup.ThumbnailId,
                                                            AdditionalInfo = x.CustomerGroup.AdditionalInfo,
                                                            CreatedOn = x.CustomerGroup.CreatedOn,
                                                            customerGroupDetails = x.CustomerGroup.customerGroupDetails.Select(x => new GetCustomerGroupDetailsForm
                                                            {
                                                                Description = x.Description,
                                                                CultureId = x.CultureId.Value,
                                                                Name = x.Name,
                                                                CultureCode = x.Culture.CultureId


                                                            }).ToList(),
                                                            ImageUrl = x.CustomerGroup.ThumbnailId.HasValue ? x.CustomerGroup.Thumbnail.PublicUrl : null,
                                                            MaxSessions = x.CustomerGroup.MaxSessionsNumber,
                                                            PasswordExpiryInDays = x.CustomerGroup.PasswordExpiryInDays,
                                                            EnableTwoFactorAuthentication = x.CustomerGroup.EnableTwoFactorAuthentication,
                                                            GroupType = x.CustomerGroup.GroupTypeId.HasValue ? new GroupTypeGridVM { Id = x.CustomerGroup.GroupTypeId.Value, Name = x.CustomerGroup.GroupType.Name, Description = x.CustomerGroup.GroupType.Description, Status = x.CustomerGroup.GroupType.Status } : null

                                                        })
                                                        .OrderByDescending(x => x.Id)
                                                         .ToListAsync();


                return groups;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task UpdateCustomerGroupDetails(List<CustomerGroupDetailsForm> customerGroupDetails, long id)
        {
            try
            {
                var CustomerGroupDetails = await customergroupDetailsRepository.Query().Where(x => x.CustomerGroupId == id).ToListAsync();

                foreach (var CustomerGroupDetail in CustomerGroupDetails)
                {
                    foreach (var UpdatecustomerGroupDetails in customerGroupDetails)
                    {
                        if (CustomerGroupDetail.CultureId == UpdatecustomerGroupDetails.CultureId)
                        {
                            CustomerGroupDetail.Description = UpdatecustomerGroupDetails.Description;
                            CustomerGroupDetail.Name = UpdatecustomerGroupDetails.Name;
                        }
                    }
                }

                //for Insert If Not Exist
                var CultureIds = CustomerGroupDetails.Select(x => x.CultureId).ToList();
                var InsertQuery = customerGroupDetails.Where(x => !CultureIds.Any(u => u == x.CultureId)).Select(x => new CustomerGroupDetails
                {
                    Description = x.Description,
                    CultureId = x.CultureId,
                    CustomerGroupId = id,
                    Name = x.Name,

                }).ToList();
                if (InsertQuery != null && InsertQuery.Count() > 0)
                {

                    customergroupDetailsRepository.AddRange(InsertQuery);
                    await customergroupDetailsRepository.SaveChangesAsync();

                };

                //

                customergroupDetailsRepository.UpdateRange(CustomerGroupDetails);
                await customergroupDetailsRepository.SaveChangesAsync();

                return;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task UpdateUserTwoFactorAuthentication(long id, bool EnableTwoFactorAuthentication)
        {
            try
            {
                var Users = await userRepository.Query().Include(x => x.CustomerGroups).Where(x => x.CustomerGroups.Any(x => x.CustomerGroupId == id)).ToListAsync();
                foreach (var user in Users)
                {
                    user.TwoFactorEnabled = EnableTwoFactorAuthentication;
                }

                await userRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

    }
}
