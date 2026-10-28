
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Core.ViewModels;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Core.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IRepository<Organization> _organizationRepository;
        private readonly IWorkContext _workContext;
        public OrganizationService(IRepository<Organization> organizationRepository, IWorkContext workContext)
        {
            _organizationRepository = organizationRepository;
            _workContext = workContext;

        }
        public async Task<long> Create(OrganizationAddVM organizationReq)
        {
            Organization organization = new Organization
            {
                AddressId = organizationReq.AddressId,
                CategoryId = organizationReq.CategoryId,
                ContactEmail = organizationReq.ContactEmail,
                ContactMobileNo = organizationReq.ContactMobileNo,
                CoreCompanyId = organizationReq.CoreCompanyId,
                CountryId = organizationReq.CountryId,
                CreatedById = await _workContext.GetCurrentUserId(),
                CreationDate = DateTimeOffset.Now,
                Description = organizationReq.Description,
                ThumbnailId = organizationReq.ThumbnailId,
                Name = organizationReq.Name,
                Status = EStatus.Active,
                StatusDate = DateTimeOffset.Now
            };

            _organizationRepository.Add(organization);
            await _organizationRepository.SaveChangesAsync();
            return organization.Id;

        }
        public async Task<long> Update(OrganizationUpdateVM organizationReq)
        {

            Organization organization = _organizationRepository.Query().Where(x => x.Id == organizationReq.Id).FirstOrDefault();

            organization.AddressId = organizationReq.AddressId;
            organization.CategoryId = organizationReq.CategoryId;
            organization.ContactEmail = organizationReq.ContactEmail;
            organization.ContactMobileNo = organizationReq.ContactMobileNo;
            organization.CoreCompanyId = organizationReq.CoreCompanyId;
            organization.CountryId = organizationReq.CountryId;
            organization.ModifiedById = await _workContext.GetCurrentUserId();
            organization.ModificationDate = DateTimeOffset.Now;
            organization.Description = organizationReq.Description;
            organization.ThumbnailId = organizationReq.ThumbnailIdId;
            organization.Name = organizationReq.Name;
            organization.Status = organizationReq.Status;
            organization.StatusDate = DateTimeOffset.Now;


            _organizationRepository.Update(organization);
            await _organizationRepository.SaveChangesAsync();
            return organization.Id;

        }
        public async Task<long> Delete(long id)
        {
             Organization organization = _organizationRepository.Query().Where(x=>x.Id==id).FirstOrDefault();
            organization.Status = EStatus.Deleted;
            organization.StatusDate = DateTime.Now;
            organization.ModifiedById = await _workContext.GetCurrentUserId();
            _organizationRepository.Update(organization);
           await _organizationRepository.SaveChangesAsync();
            return id;
        }

        public async Task<OrganizationVM> GetById(long organizationId)
        {
            return await _organizationRepository.Query()
                 .Where(x => x.Id == organizationId)
                 .Include(x => x.Category)
                 .ThenInclude(x=>x.CategoryDetails)
                 .Include(x => x.Sites)
                 .Include(x=>x.Thumbnail)
                 .Select(p => new OrganizationVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    AddressId = p.AddressId,
                    CategoryId = p.CategoryId,
                     Category = new CategoryOrganzationViewModel
                     {
                         Id = p.Category.Id,
                         Status = p.Category.Status
                        ,
                         CategoryDetails = p.Category.CategoryDetails==null?new List<CategoryDetailsOrganzationViewModel>() : p.Category.CategoryDetails.Select(x => new CategoryDetailsOrganzationViewModel
                         {
                             CultureId = x.CultureId,
                             Name = x.Name
                         }).ToList()
                     },
                     AddressName = "",
                    ContactEmail = p.ContactEmail,
                    ContactMobileNo = p.ContactMobileNo,
                    CoreCompanyId = p.CoreCompanyId,
                    CountryId = p.CountryId,
                    CountryName = "",
                    CreatedBy = p.CreatedBy.FullName,
                    CreatedById = p.CreatedById,
                    CreationDate = p.CreationDate,
                    ThumbnailId = p.ThumbnailId,
                    ThumbnailURL=p.Thumbnail.PublicUrl,
                    ModificationDate = p.ModificationDate,
                    ModifiedBy = p.ModifiedBy.FullName,
                    ModifiedById = p.ModifiedById,
                    Sites = p.Sites.Select(s => new OrganizationsSitesViewModel
                    {
                        Id = s.Id,
                        OrganizationId = s.OrganizationId,
                        Description = s.Description,
                        Name = s.Name,
                        ThumbnailURL = s.Thumbnail.PublicUrl,
                        ThumbnailId=s.ThumbnailId,
                    }).ToList(),
                    StatusDate = p.StatusDate
                }).FirstOrDefaultAsync();


        }
        public async Task<OrganizationsGridOutputVM<OrganizationVM>> GetList(OrganizationsSearchTableParam<OrganizationsListSearchVM> param)
        {
            OrganizationsListSearchVM search = param.Search;
            List<Organization> organizationsQuery = await _organizationRepository.Query().AsNoTracking()
                .Include(x => x.Category)
                .ThenInclude(x=>x.CategoryDetails)
                .Include(x => x.ModifiedBy)
                .Include(x => x.CreatedBy)
                .Include(x => x.Thumbnail)
                .Include(x => x.Sites).ThenInclude(x => x.Thumbnail)
                .Where(org =>
                  (string.IsNullOrEmpty(search.Name) || (org.Name.ToUpper().Contains(search.Name.ToUpper()) || org.ContactMobileNo.ToUpper().Contains(search.Name.ToUpper()) || org.ContactEmail.ToUpper().Contains(search.Name.ToUpper())))
                  && ((search.Status == null || search.Status == 0) || org.Status == search.Status)
                  && (string.IsNullOrEmpty(search.CountryId) || org.CountryId.ToUpper().Contains(search.CountryId.ToUpper()))
                  && (search.CreationDateFrom == null || org.CreationDate >= search.CreationDateFrom)
                  && (search.CreationDateTo == null || org.CreationDate <= search.CreationDateTo))
                   .OrderByDescending(x => x.CreationDate).ToListAsync();
            OrganizationsGridOutputVM<OrganizationVM> organizations = new OrganizationsGridOutputVM<OrganizationVM>
            {
                Items = organizationsQuery.Select(p => new OrganizationVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    AddressId = p.AddressId,
                    CategoryId = p.CategoryId,
                    Category = new CategoryOrganzationViewModel
                    {
                        Id = p.Category.Id,
                        Status = p.Category.Status
                        ,
                        CategoryDetails=p.Category.CategoryDetails == null ? new List<CategoryDetailsOrganzationViewModel>() : p.Category.CategoryDetails.Select(x=>new CategoryDetailsOrganzationViewModel { 
                        CultureId= x.CultureId,
                        Name=x.Name
                        }).ToList()
                    },
                    ContactEmail = p.ContactEmail,
                    ContactMobileNo = p.ContactMobileNo,
                    CoreCompanyId = p.CoreCompanyId,
                    CountryId = p.CountryId,
                    CreatedBy = p.CreatedBy.FullName,
                    CreatedById = p.CreatedById,
                    CreationDate = p.CreationDate,
                    ThumbnailURL = p.ThumbnailId != null ? p.Thumbnail.PublicUrl: null,
                    ThumbnailId=p.ThumbnailId,
                    Sites = p.Sites.Select(s => new OrganizationsSitesViewModel
                    {
                        Id = s.Id,
                        OrganizationId = s.OrganizationId,
                        Description = s.Description,
                        Name = s.Name,
                        ThumbnailURL = s.Thumbnail != null ?s.Thumbnail.PublicUrl: null,
                        ThumbnailId=s.ThumbnailId,
                    }).ToList(),
                    StatusDate = p.StatusDate
                }).Skip(param.Pagination.Number * (param.Pagination.Start - 1)).Take(param.Pagination.Number).ToList(),
                TotalRecord = organizationsQuery.Count
            };
            return organizations;
        }
        public async Task<List<OrganizationGetAllVM>> GetAll()
        {
            List<OrganizationGetAllVM> organizations = await _organizationRepository.Query()
                .Where(org => org.Status == EStatus.Active || org.Status == EStatus.Published)
                .Include(x=>x.Thumbnail)
                .OrderByDescending(x => x.CreationDate)
                .Select(p => new OrganizationGetAllVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    ThumbnailURL = p.Thumbnail.PublicUrl,
                    ThumbnailId=p.ThumbnailId,
                    Description=p.Description
                })
                .ToListAsync();
            return organizations;
        }
    }
}
