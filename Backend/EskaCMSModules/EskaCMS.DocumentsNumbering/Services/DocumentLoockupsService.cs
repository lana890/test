using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.ViewModels;
using EskaCMS.DocumentsNumbering.Interfaces;
using EskaCMS.DocumentsNumbering.Models;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.DocumentsNumbering;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.DocumentsNumbering.Services
{
    public class DocumentLoockupsService : IDocumentLoockupsService
    {
        private readonly DateTimeOffset TimeNow = DateTimeOffset.Now;

        private readonly IRepository<DocumentType> _documentTypeRepository;
        private readonly IRepository<DocumentClassification> _documentClassificationRepository;
        private readonly IRepository<Site> _siteRepository;
        private readonly IRepository<CustomerGroup> _groupRepository;
        private readonly IWorkContext _workContext;
        public readonly IConfiguration _configuration;
        public readonly UserManager<User> _userManager;
        private readonly IRepositoryWithTypedId<Country, string> _documentCountryRepository;
        public string baseApiUrl = "";

        public DocumentLoockupsService(
            IRepository<DocumentType> documentTypeRepository,
            IRepository<DocumentClassification> documentClassificationRepository,
            IRepository<Site> siteRepository,
            IRepository<CustomerGroup> groupRepository,
            IWorkContext workContext,
            IConfiguration configuration,
            UserManager<User> userManager,
                        IRepositoryWithTypedId<Country, string> documentCountryRepository
            )
        {
            _documentClassificationRepository = documentClassificationRepository;
            _documentTypeRepository = documentTypeRepository;
            _workContext = workContext;
            _siteRepository = siteRepository;
            _groupRepository = groupRepository;
            _configuration = configuration;
            _userManager = userManager;
            _documentCountryRepository = documentCountryRepository;
            baseApiUrl = _configuration.GetValue<string>("EskaCore:BaseApiUrl");
        }
        public async Task<long> CreateType(DocumentTypeAddVM request)
        {
            DocumentType documentType = new DocumentType
            {
                CreatedById = await _workContext.GetCurrentUserId(),
                CreationDate = TimeNow,
                Description = request.Description,
                Name = request.Name
            };
            _documentTypeRepository.Add(documentType);
            await _documentTypeRepository.SaveChangesAsync();
            return documentType.Id;
        }
        public async Task<long> CreateClassification(DocumentClassificationAddVM request)
        {
            DocumentClassification classification = new DocumentClassification
            {
                CreatedById = await _workContext.GetCurrentUserId(),
                CreationDate = TimeNow,
                Description = request.Description,
                Name = request.Name
            };
            _documentClassificationRepository.Add(classification);
            await _documentClassificationRepository.SaveChangesAsync();
            return classification.Id;
        }

        public async Task<DocumentTypeVM> GetDocumentType(long id)
        {
            DocumentTypeVM documentType = await _documentTypeRepository.Query().Where(x => x.Id == id)
                .Select(x => new DocumentTypeVM
                {
                    Id = x.Id,
                    CreatedById = x.CreatedById,
                    Description = x.Description,
                    Name = x.Name,
                    CreatedBy = x.CreatedBy.FullName
                }).FirstOrDefaultAsync();

            return documentType;
        }

        public async Task<List<DocumentTypeVM>> GetAllDocumentTypes()
        {
            List<DocumentTypeVM> documentTypes = await _documentTypeRepository.Query()
                .Select(x => new DocumentTypeVM
                {
                    Id = x.Id,
                    CreatedById = x.CreatedById,
                    Description = x.Description,
                    Name = x.Name,
                    CreatedBy = x.CreatedBy.FullName
                }).ToListAsync();

            return documentTypes;
        }
        public async Task<List<DocumentClassificationVM>> GetAllDocumentClassifications()
        {
            List<DocumentClassificationVM> classifications = await _documentClassificationRepository.Query()
                .Select(x => new DocumentClassificationVM
                {
                    Id = x.Id,
                    CreatedById = x.CreatedById,
                    Description = x.Description,
                    Name = x.Name,
                    CreatedBy = x.CreatedBy.FullName
                }).ToListAsync();

            return classifications;
        }
        public async Task<DocumentClassificationVM> GetDocumentClassification(long id)
        {
            DocumentClassificationVM classification = await _documentClassificationRepository.Query().Where(x => x.Id == id)
                .Select(x => new DocumentClassificationVM
                {
                    Id = x.Id,
                    CreatedById = x.CreatedById,
                    Description = x.Description,
                    Name = x.Name,
                    CreatedBy = x.CreatedBy.FullName
                }).FirstOrDefaultAsync();

            return classification;
        }

        public async Task<List<CustomerGroupVM>> GetAuthorizedDepartments(string username)
        {
            User user = await _userManager.Users.Where(x => x.UserName == username).Include(g => g.CustomerGroups).ThenInclude(c => c.CustomerGroup).FirstOrDefaultAsync();
            long? groupId = null;
            var cultureId = _workContext.GetCurrentCultureId();

            var CustomerGroups = user.CustomerGroups.SelectMany(x => x.CustomerGroup.customerGroupDetails).ToList();
            if (CustomerGroups.Count()>0 && CustomerGroups.Any(x => x.Name != "Mr Nael"))
            {
                groupId = user.CustomerGroups.Where(x => x.CustomerGroup.FireBaseTopic == "ADDepartment").FirstOrDefault().CustomerGroupId;
            }
            List<CustomerGroupVM> customerGroups = await _groupRepository.Query()
                .Where(x =>
                x.FireBaseTopic == "ADDepartment" &&
                (!groupId.HasValue || x.Id == (long)groupId) &&
                x.IsActive)
                .Select(d => new CustomerGroupVM
                {
                    Id = d.Id,
                    FireBaseTopic = d.FireBaseTopic,
                    Name = d.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                    Description = d.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),

                    Code = FetchDepartmentCode(d.AdditionalInfo),
                    DepartmentId = FetchDepartmentId(d.AdditionalInfo),
                    Email = d.Email,
                }).ToListAsync();
            return customerGroups;
        }
        public async Task<List<CustomerGroupVM>> GetManagementDepartments()
        {
            try
            {
                var cultureId = _workContext.GetCurrentCultureId();



                List<CustomerGroupVM> customerGroups = await _groupRepository.Query().Include(x => x.customerGroupDetails)
                    .Where(x =>
                    x.FireBaseTopic == "ManagementDepartment" &&
                    x.IsActive)
                    .Select(d => new CustomerGroupVM
                    {
                        Id = d.Id,
                        FireBaseTopic = d.FireBaseTopic,
                        Name = d.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                        Description = d.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),

                        Code = FetchDepartmentCode(d.AdditionalInfo),
                        DepartmentId = FetchDepartmentId(d.AdditionalInfo),
                        Email = d.Email
                    }).ToListAsync();
                return customerGroups;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private static string FetchDepartmentCode(string additionalInfo)
        {
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                CustomerGroupAdditionalInfoVM additionalInfoVM = JsonSerializer.Deserialize<CustomerGroupAdditionalInfoVM>(additionalInfo);
                return additionalInfoVM.DepartmentCode;
            }
            return "";
        }
        private static int FetchDepartmentId(string additionalInfo)
        {
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                CustomerGroupAdditionalInfoVM additionalInfoVM = JsonSerializer.Deserialize<CustomerGroupAdditionalInfoVM>(additionalInfo);
                return additionalInfoVM.DepartmentId;
            }
            return 0;
        }
        public async Task<List<CustomerGroupVM>> GetAllDepartments()
        {
            var cultureId = _workContext.GetCurrentCultureId();

            List<CustomerGroupVM> customerGroups = await _groupRepository.Query().Include(x=>x.customerGroupDetails)
                .Where(x =>
                x.FireBaseTopic == "ADDepartment" &&
                x.IsActive)
                .Select(d => new CustomerGroupVM
                {
                    Id = d.Id,
                    FireBaseTopic = d.FireBaseTopic,
                    Name = d.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                    Description = d.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),

                    Code = FetchDepartmentCode(d.AdditionalInfo),
                    DepartmentId = FetchDepartmentId(d.AdditionalInfo),
                    Email = d.Email
                }).ToListAsync();
            return customerGroups;
        }
        public async Task<List<UserListVM>> GetAllUsers()
        {
            List<UserListVM> users = await _userManager.Users
                .Include(x => x.CustomerGroups)
                .ThenInclude(x => x.CustomerGroup)
                .Where(x => x.CustomerGroups.Any(g => g.CustomerGroup.FireBaseTopic == "ADDepartment" || g.CustomerGroup.customerGroupDetails.Any(k=>k.Name == "Mr Nael")))
                .Select(d => new UserListVM
                {
                    Id = d.Id,
                    Email = d.Email,
                    FullName = d.FullName,
                    Username = d.UserName
                }).ToListAsync();
            return users;
        }
        public async Task<List<UserListVM>> GetUsersByParentUsername(string username, bool? isManagerIncluded)
        {
            IList<User> users = await _userManager.Users
                .Include(x => x.Parent).ToListAsync();
            User currentUser = users.Where(x => x.UserName == username).FirstOrDefault();
            List<UserListVM> userEmployees = users.Where(x => x.ParentId == currentUser.Id || x.Id == currentUser.Id)
                .Select(d => new UserListVM
                {
                    Id = d.Id,
                    Email = d.Email,
                    FullName = d.FullName,
                    Username = d.UserName
                }).ToList();
            if (isManagerIncluded.HasValue)
            {
                if (currentUser.Parent != null && (bool)isManagerIncluded)
                {
                    UserListVM mnanager = new UserListVM
                    {
                        Id = currentUser.Parent.Id,
                        Email = currentUser.Parent.Email,
                        FullName = currentUser.Parent.FullName,
                        Username = currentUser.Parent.UserName
                    };
                    userEmployees.Add(mnanager);
                }
            }


            return userEmployees;
        }
        public async Task<PreparedByVM> GetUserAndManager()
        {
            long CurrentUserId = await _workContext.GetCurrentUserId();
            PreparedByVM user = await _userManager.Users
                .Include(x => x.Parent)
                .Where(x => x.Id == CurrentUserId)
                .Select(d => new PreparedByVM
                {
                    PreparedById = d.Id,
                    PreparedByName = d.FullName,
                    ApprovedById = d.ParentId,
                    ApprovedBy = d.Parent.FullName
                }).FirstOrDefaultAsync();
            return user;
        }
        public async Task<List<DocumentCountryVM>> GetAllCountries()
        {
            var cultureId = _workContext.GetCurrentCultureId();
            List<DocumentCountryVM> countriea = await _documentCountryRepository.Query().Include(x => x.CountryDetails)
                .Select(d => new DocumentCountryVM
                {
                    Id = d.Id,
                    Name = d.CountryDetails.Where(x => x.CultureId == cultureId).FirstOrDefault().Name,
                    Code = d.Code3,
                }).ToListAsync();
            return countriea;

        }
    }
}