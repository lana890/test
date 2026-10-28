using EskaCMS.Core.Areas.Core.Interfaces;
using EskaCMS.Core.Areas.Core.ViewModels.GroupTypeModels.Request;
using EskaCMS.Core.Areas.Core.ViewModels.GroupTypeModels.Response;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.Services
{
    public class GroupTypeService : IGroupTypeService
    {
        private readonly IRepository<CustomerGroupType> _CustomerGroupTypeRepository;
        private readonly IWorkContext _workContext;

        public GroupTypeService(IRepository<CustomerGroupType> CustomerGroupTypeRepository,
                                IWorkContext workContext)
        {
            _CustomerGroupTypeRepository = CustomerGroupTypeRepository;
            _workContext = workContext;
        }

        public async Task<bool> ValidateGroupType(CustomerGroupType model , long siteId)
        {

           
            if (_CustomerGroupTypeRepository.Query().Any(x=>x.SiteId == siteId && x.Id != model.Id && x.Name.ToLower() == model.Name.ToLower()))
            {
                throw new Exception("there is another group type with the same name");
            }

            return true;
        }

        public async Task<bool> Create(CreateVM model)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();

            var groupType = new CustomerGroupType
            {
                Name = model.Name,
                Description = model.Description,
                CreatedById = userId,
                CreationDate = DateTimeOffset.Now,
                SiteId = siteId,
                Status = EStatus.Published,
            };

            var validate = await ValidateGroupType(groupType,siteId);

            if (!validate)
            {
                return false;
            }

            _CustomerGroupTypeRepository.Add(groupType);
            _CustomerGroupTypeRepository.SaveChanges();

            return true;
        }

        public async Task<GroupTypeGridVM> Details(long groupTypeId)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();
            var groupType = _CustomerGroupTypeRepository.Query()
                                                        .Where(x => x.Id == groupTypeId && x.SiteId == siteId && x.Status !=EStatus.Deleted)
                                                        .FirstOrDefault();

            if (groupType == null)
            {
                throw new Exception("Customer group type not found");
            }


            var result = new GroupTypeGridVM { Id = groupType.Id, Name = groupType.Name, Description = groupType.Description };

            return result;
        }

        public async Task<bool> Update(UpdateVM model)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();
            var groupType = _CustomerGroupTypeRepository.Query()
                                                        .Where(x => x.Id == model.Id && x.SiteId == siteId)
                                                        .FirstOrDefault();

            if(groupType == null)
            {
                throw new Exception("Customer group type not found");
            }


            groupType.Name = model.Name;
            groupType.Description = model.Description;
            groupType.ModifiedById = userId;
            groupType.ModificationDate = DateTimeOffset.Now;
            _CustomerGroupTypeRepository.SaveChanges();

            return true;
        }

        public async Task<PaginationResult<GroupTypeGridVM>> GetAll(SmartTableParam param)
        {
           

            var siteId = _workContext.GetCurrentSiteId();

            var query = _CustomerGroupTypeRepository.Query()
                                                    .Where(x => x.SiteId == siteId && x.Status != EStatus.Deleted)
                                                    .AsNoTracking();




            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;



                if (search.From != null)
                {
                    DateTimeOffset From = search.From;
                    query = query.Where(x => x.CreationDate >= From);
                }


                if (search.To != null)
                {
                    DateTimeOffset To = search.To;
                    query = query.Where(x => x.CreationDate <= To);
                }

                if (search.Name != null)
                {
                    string Name = search.Name;
                    query = query.Where(x => x.Name.Contains(Name));
                }

                if (search.Status != null)
                {
                    EStatus Status = (EStatus) search.Status;
                    query = query.Where(x => x.Status == Status);
                }




            }

            if (!query.Any())
            {
                return new PaginationResult<GroupTypeGridVM>(new List<GroupTypeGridVM>(), 0, 0);
            }



            var result = await query.OrderByDescending(x => x.CreationDate)
                                    .Select(x => new GroupTypeGridVM
                                    {
                                        Id = x.Id,
                                        Name = x.Name,
                                        Description=x.Description,
                                        CreationDate = x.CreationDate,
                                        ModificationDate=x.ModificationDate,
                                        Status=x.Status,
                                       
                                    })
                                    .PaginateAsync(param.Pagination.Number, param.Pagination.Start);

            return result;


        }

        public async Task<bool> Publish(long GroupTypeId)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();
            var groupType = _CustomerGroupTypeRepository.Query()
                                                        .Where(x => x.Id == GroupTypeId && x.SiteId == siteId)
                                                        .FirstOrDefault();

            if (groupType == null)
            {
                throw new Exception("Customer group type not found");
            }


            groupType.Status = EStatus.Published;
            groupType.ModifiedById = userId;
            groupType.ModificationDate = DateTimeOffset.Now;
            _CustomerGroupTypeRepository.SaveChanges();

            return true;
        }

        public async Task<bool> UnPublish(long GroupTypeId)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();
            var groupType = _CustomerGroupTypeRepository.Query()
                                                        .Where(x => x.Id == GroupTypeId && x.SiteId == siteId)
                                                        .FirstOrDefault();

            if (groupType == null)
            {
                throw new Exception("Customer group type not found");
            }


            groupType.Status = EStatus.Unpublished;
            groupType.ModifiedById = userId;
            groupType.ModificationDate = DateTimeOffset.Now;
            _CustomerGroupTypeRepository.SaveChanges();

            return true;
        }

        public async Task<bool> Delete(long GroupTypeId)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();
            var groupType = _CustomerGroupTypeRepository.Query()
                                                        .Where(x => x.Id == GroupTypeId && x.SiteId == siteId)
                                                        .Include(x=>x.CustomerGroups)
                                                        .FirstOrDefault();

            if (groupType == null)
            {
                throw new Exception("Customer group type not found");
            }

            if(groupType.CustomerGroups.Any(x=>x.IsActive))
                throw new Exception("The customer group type cannot be removed because it is related to active customer groups");


            groupType.Status = EStatus.Deleted;
            groupType.ModifiedById = userId;
            groupType.ModificationDate = DateTimeOffset.Now;
            _CustomerGroupTypeRepository.SaveChanges();

            return true;
        }

    }
}
