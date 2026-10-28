using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.DocumentsNumbering.Enums;
using EskaCMS.DocumentsNumbering.Interfaces;
using EskaCMS.DocumentsNumbering.Models;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.DocumentsNumbering;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Core.Services;
using EskaCMS.Core.Areas.Core.ViewModels;

namespace EskaCMS.DocumentsNumbering.Services
{
    public class DocumentNumberService : IDocumentNumberService
    {
        private readonly DateTimeOffset TimeNow = DateTimeOffset.Now;

        private readonly IRepositoryWithTypedId<DocumentsNumbers, string> _documentNumberRepository;
        private readonly IRepository<DocumentNumberHistory> _documentNumberHistoryRepository;
        private readonly IRepository<CustomerGroup> _groupRepository;
        private readonly IWorkContext _workContext;
        private readonly ISiteSettingsService _SiteSettingsService;
        public readonly UserManager<User> _userManager;

        public DocumentNumberService(
            IRepositoryWithTypedId<DocumentsNumbers, string> documentNumberRepository,
            IRepository<DocumentNumberHistory> documentNumberHistoryRepository,
            IRepository<CustomerGroup> groupRepository,
            IWorkContext workContext,
            UserManager<User> userManager
,
            ISiteSettingsService siteSettingsService)
        {
            _documentNumberRepository = documentNumberRepository;
            _workContext = workContext;
            _documentNumberHistoryRepository = documentNumberHistoryRepository;
            _userManager = userManager;
            _SiteSettingsService = siteSettingsService;
            _groupRepository = groupRepository;
        }
        public async Task<DocumentsNumbersResponseVM> Create(DocumentsNumbersAddVM request)
        {
            try
            {
                const string departmentGroupTypeId = "DepartmentsGroupTypeId";
                long SiteId = await _workContext.GetCurrentSiteIdAsync();
                long CultureId = _workContext.GetCurrentCultureId();
                var Settings = await _SiteSettingsService.GetSiteSettingsByModule("DocumentNumber", SiteId);
                long DepartmentsGroupTypeId = long.Parse(Settings.FirstOrDefault(x => x.Id.ToLower() == departmentGroupTypeId.ToLower())?.Value);
                string CompanyCode = Settings.FirstOrDefault(x => x.Id.ToLower() == "companycode")?.Value;
                var CustomerGroup = await _groupRepository.Query().Include(x => x.customerGroupDetails).Where(x => x.GroupTypeId == DepartmentsGroupTypeId && x.Users.Any(u => u.UserId == request.PreparedById)).FirstOrDefaultAsync();
                var AdditionalInfo = CustomerGroup.AdditionalInfo;


                string groupCode = FetchDepartmentCode(AdditionalInfo);



                DocumentsNumbers documentNumber = new DocumentsNumbers
                {
                    CreatedById = await _workContext.GetCurrentUserId(),
                    CreationDate = TimeNow,
                    Name = request.Name,
                    ApprovedById = request.ApprovedById != null ? (long)request.ApprovedById : request.PreparedById,
                    CountryId = request.CountryId,
                    GroupId = CustomerGroup.Id,
                    DocumentClassificationId = request.DocumentClassificationId,
                    DocumentTypeId = request.DocumentTypeId,
                    Language = request.Language,
                    PreparedById = request.PreparedById,
                    Reference = request.Reference,
                    RevisionNumber = request.RevisionNumber,
                    SiteId = SiteId
                };
                documentNumber.SequenceNumber = GenerateSequenceNumber(CustomerGroup.Id);
                documentNumber.Id = CompanyCode + "/" + groupCode + "-" + request.CountryCode + "-" + TimeNow.Year.ToString().Substring(2) + ":" + documentNumber.SequenceNumber + " " + documentNumber.Language;
                _documentNumberRepository.Add(documentNumber);
                await _documentNumberRepository.SaveChangesAsync();
                await LogHistory(documentNumber);
                return new DocumentsNumbersResponseVM
                {
                    Id = documentNumber.Id,
                    ApprovedById = documentNumber.ApprovedById,
                    PreparedById = documentNumber.PreparedById
                };
            }
            catch (Exception e)
            {

                throw;
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
        public async Task<string> GetDocumentAuthorizedBy()
        {
            long userId = await _workContext.GetCurrentUserId();
            const string departmentGroupTypeId = "DepartmentsGroupTypeId";
            long SiteId = await _workContext.GetCurrentSiteIdAsync();
            long CultureId = _workContext.GetCurrentCultureId();
            var Settings = await _SiteSettingsService.GetSiteSettingsByModule("DocumentNumber", SiteId);
            long DepartmentsGroupTypeId = long.Parse(Settings.FirstOrDefault(x => x.Id.ToLower() == departmentGroupTypeId.ToLower())?.Value);
            string CompanyCode = Settings.FirstOrDefault(x => x.Id.ToLower() == "companycode")?.Value;
            var CustomerGroup = await _groupRepository.Query().Include(x => x.customerGroupDetails).Where(x => x.GroupTypeId == DepartmentsGroupTypeId && x.Users.Any(u => u.UserId == userId)).FirstOrDefaultAsync();


            var Description = CustomerGroup.customerGroupDetails.Where(x => CultureId == 0 || x.CultureId == CultureId).Select(x => x.Description).FirstOrDefault();
            return CompanyCode + "/" + Description;
        }

        public async Task<LatestDocumentNumberVM> GetUserLatestDocumentNo()
        {
            long userId = await _workContext.GetCurrentUserId();
            DocumentsNumbers document = await _documentNumberRepository.Query().AsNoTracking()
                .Where(doc => doc.CreatedById == userId)
                .Include(h => h.DocumentNumberHistory)
                .OrderByDescending(date => date.CreationDate).FirstOrDefaultAsync();
            if (document == null)
            {
                return new LatestDocumentNumberVM { };
            }
            else
            {
                DocumentNumberHistory history = document.DocumentNumberHistory.LastOrDefault();
                return new LatestDocumentNumberVM { Id = history?.DocumentNumberId ??document.Id, SequenceNumber = document.SequenceNumber };

            }

        }
        public async Task<string> Update(DocumentsNumbersUpdateVM request)
        {
            DocumentsNumbers document = _documentNumberRepository.Query().Include(x => x.Group).Include(x => x.Country).Where(x => x.Id == request.Id).FirstOrDefault();

            if (request.ApprovedById.HasValue)
            {
                document.ApprovedById = (long)request.ApprovedById;
            }
            //if (request.GroupId.HasValue)
            //{
            //    document.GroupId =(long) request.GroupId;
            //}
            if (!string.IsNullOrEmpty(request.CountryId))
            {
                document.CountryId = request.CountryId;
            }
            if (!string.IsNullOrEmpty(request.Name))
            {
                document.Name = request.Name;
            }
            if (!string.IsNullOrEmpty(request.RevisionNumber))
            {
                document.RevisionNumber = request.RevisionNumber;
            }
            if (!string.IsNullOrEmpty(request.Reference))
            {
                document.Reference = request.Reference;
            }

            if (request.SoftCopyId.HasValue)
            {
                document.SoftCopyId = request.SoftCopyId;
            }

            document.ModificationDate = TimeNow;
            document.ModifiedById = await _workContext.GetCurrentUserId();
            _documentNumberRepository.Update(document);
            await _documentNumberRepository.SaveChangesAsync();
            await LogHistory(document);
            return document.Id;
        }
        public async Task<ListResponseVM<DocumentsNumbersResponseVM>> GetList(ListRequestVM<DocumentsNumbersListRequestVM> param)
        {
            long userId = await _workContext.GetCurrentUserId();
            const string departmentGroupTypeId = "DepartmentsGroupTypeId";
            long SiteId = await _workContext.GetCurrentSiteIdAsync();
            long CultureId = _workContext.GetCurrentCultureId();
            var Settings = await _SiteSettingsService.GetSiteSettingsByModule("DocumentNumber", SiteId);
            long DepartmentsGroupTypeId = long.Parse(Settings.FirstOrDefault(x => x.Id.ToLower() == departmentGroupTypeId.ToLower())?.Value);
            string CompanyCode = Settings.FirstOrDefault(x => x.Id.ToLower() == "companycode")?.Value;
            var CustomerGroup = await _groupRepository.Query().Include(x => x.customerGroupDetails).Where(x => x.GroupTypeId == DepartmentsGroupTypeId && x.Users.Any(u => u.UserId == userId)).FirstOrDefaultAsync();


            DocumentsNumbersListRequestVM search = param.SearchParams;
            User user = await _userManager.Users.Where(x => x.Id == userId).FirstOrDefaultAsync();
            var Personnel = (await GetUserPersonnel(false)).Select(x=>x.Id).ToList();
            // by yazan
            List<DocumentsNumbers> docQuery = await _documentNumberRepository.Query().AsNoTracking()
                                      .Where(doc =>  
                                          ((string.IsNullOrEmpty(search.Name) || doc.Name.ToUpper().Contains(search.Name.ToUpper())) ||
                                          (string.IsNullOrEmpty(search.Name) || doc.Id.ToUpper().Contains(search.Name.ToUpper())) )&&
                                          (string.IsNullOrEmpty(search.RevisionNumber) || doc.RevisionNumber.ToUpper().Contains(search.RevisionNumber.ToUpper())) &&
                                          (string.IsNullOrEmpty(search.Language) || doc.Language.ToUpper().Contains(search.Language.ToUpper())) &&
                                          (string.IsNullOrEmpty(search.Year) || doc.CreationDate.Year.ToString().ToUpper().Contains(search.Year.ToUpper())) &&
                                          (string.IsNullOrEmpty(search.CountryId) || doc.CountryId.ToUpper().Contains(search.CountryId.ToUpper())) &&
                                          ((search.ApprovedById == null || search.ApprovedById == 0) || doc.ApprovedById == search.ApprovedById) &&
                                          ((search.PreparedById == null || search.PreparedById == 0) || doc.PreparedById == search.PreparedById) &&
                                          ((search.GroupId == null || search.GroupId == 0) || doc.GroupId == search.GroupId) &&
                                          ((search.DocumentTypeId == null || search.DocumentTypeId == 0) || doc.DocumentTypeId == search.DocumentTypeId) &&
                                          ((search.DocumentClassificationId == null || search.DocumentClassificationId == 0) || doc.DocumentClassificationId == search.DocumentClassificationId))
                                      .Where(x =>
                                        (CustomerGroup == null || x.GroupId == CustomerGroup.Id) &&
                                        (Personnel.Count == 0 || Personnel.Contains(x.PreparedById) || Personnel.Contains(x.ApprovedById))
                                        )
                                      .Include(x => x.PreparedBy)
                                      .Include(x => x.ApprovedBy)
                                      .Include(x => x.Group)
                                      .ThenInclude(x => x.customerGroupDetails)
                                      .Include(x => x.SoftCopy)
                                      .OrderByDescending(d => d.CreationDate)
                                      .ToListAsync();
            ListResponseVM<DocumentsNumbersResponseVM> documents = new ListResponseVM<DocumentsNumbersResponseVM>
            {
                Items = docQuery
                .Select(x => new DocumentsNumbersResponseVM
                {
                    Id = x.Id,
                    ApprovedById = x.ApprovedById,
                    ApprovedBy = x.ApprovedBy.FullName,
                    GroupId = x.GroupId,
                    Group = x.Group.customerGroupDetails.Where(x => CultureId == 0 || x.CultureId == CultureId).Select(x => x.Name).FirstOrDefault(),
                    PreparedById = x.PreparedById,
                    PreparedBy = x.PreparedBy.FullName,
                    Name = x.Name,
                    CurrentDocumentNumberId = x.Id,
                    DocumentPublicURL = x.SoftCopyId.HasValue ? x.SoftCopy.PublicUrl : string.Empty,
                    DocumentId = x.SoftCopyId
                }).Skip(param.PageSize * param.PageIndex).Take(param.PageSize).ToList(),

                TotalRecord = docQuery.Count()
            };

            return documents;

        }
        public async Task<DocumentsNumbersResponseVM> Get(string id)
        {
            var cultureId = _workContext.GetCurrentCultureId();

            DocumentsNumbersResponseVM document = await _documentNumberRepository.Query()
                .Where(doc => doc.Id == id)

                .Include(x => x.DocumentClassification)
                .Include(x => x.DocumentType)
                .Include(x => x.PreparedBy)
                .Include(x => x.Group)
                .ThenInclude(x => x.customerGroupDetails)
                .Include(x => x.ApprovedBy)
                .AsNoTracking()
                .Select(x => new DocumentsNumbersResponseVM
                {
                    Id = x.Id,
                    CountryId = x.CountryId,
                    CreationDate = x.CreationDate,
                    DocumentClassificationId = x.DocumentClassificationId,
                    DocumentClassification = x.DocumentClassification.Name,
                    DocumentType = x.DocumentType.Name,
                    DocumentTypeId = x.DocumentTypeId,
                    Language = x.Language,
                    ModificationDate = x.ModificationDate,
                    Name = x.Name,
                    Reference = x.Reference,
                    RevisionNumber = x.RevisionNumber,
                    ApprovedById = x.ApprovedById,
                    ApprovedBy = x.ApprovedBy.FullName,
                    GroupId = x.GroupId,
                    Group = x.Group.customerGroupDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                    PreparedById = x.PreparedById,
                    PreparedBy = x.PreparedBy.FullName,
                    DocumentPublicURL = x.SoftCopyId.HasValue ? x.SoftCopy.PublicUrl : string.Empty,
                    DocumentId = x.SoftCopyId
                })
                .FirstOrDefaultAsync();

            return document;

        }

        public async Task<List<UserDDLVM>> GetUserPersonnel(bool withManager)
        {

            long SiteId = _workContext.GetCurrentSiteId();
            long CurrentUserId = await _workContext.GetCurrentUserId();

            var AllUsers = await _userManager.Users.Include(x => x.UserRoles)
                .AsNoTracking()
                .Select(x => new UserDDLVM
                {
                    FullName = x.FullName,
                    Id = x.Id,
                    ParentId = x.ParentId
                })
                .ToListAsync();


            var Personnel = await GetPersonnel(AllUsers, CurrentUserId);
            var CurrentUser = AllUsers.Where(x => x.Id == CurrentUserId).FirstOrDefault();

            bool isManager = AllUsers.Any(x => x.ParentId == CurrentUserId);
            if (isManager)
            {
                var subordinates = AllUsers.Where(x => x.ParentId == CurrentUserId).ToList();
                Personnel.AddRange(subordinates);

                if (CurrentUser.ParentId.HasValue && withManager)
                {
                    var manager = AllUsers.FirstOrDefault(x => x.Id == CurrentUser.ParentId.Value);
                    if (manager != null)
                        Personnel.Add(manager);
                }
                else if (subordinates.Any())
                {

                    Personnel.AddRange(subordinates);
                }

                Personnel.Add(CurrentUser);
            }
            else
            {
                Personnel.Add(CurrentUser);

            }



            return Personnel;



        }

        private async Task<List<UserDDLVM>> GetPersonnel(List<UserDDLVM> AllUsers, long TargetManagerId)
        {
            var Users = AllUsers.Where(x => x.ParentId == TargetManagerId).ToList();
            var Children = new List<UserDDLVM>();
            Children.AddRange(Users);
            foreach (var user in Children)
            {
                var Grandchildren = await GetPersonnel(AllUsers, user.Id);
                if (Grandchildren.Count > 0)
                {
                    Users.Concat(Grandchildren);

                }
            }

            return Users;
        }
        public async Task LinkSoftCopy(LinkSoftCopyVM Model)
        {
            var DocNumber = await _documentNumberRepository.Query().Where(x => x.Id == Model.DocumentNumberId).FirstOrDefaultAsync();
            long CurrntUserId = await _workContext.GetCurrentUserId();
            if (DocNumber == null)
            {
                throw new Exception("Document Not Found");
            }
            DocNumber.SoftCopyId = Model.SoftCopyId;
            DocNumber.ModifiedById = CurrntUserId;
            DocNumber.ModificationDate = DateTimeOffset.Now;
            await _documentNumberRepository.SaveChangesAsync();
            await LogHistory(DocNumber);

        }
        #region Helper Methods
        private string GenerateSequenceNumber(long groupId)
        {
            DocumentsNumbers lastAddedDoc = _documentNumberRepository.Query()
    .Where(x => x.GroupId == groupId && x.CreationDate != null)
    .OrderBy(x => x.CreationDate)
    .LastOrDefault();
            string newSequenceNumber = "";
            if (lastAddedDoc != null)
            {
                int lastAddedDocSeq = Convert.ToInt32(lastAddedDoc.SequenceNumber);

                if (lastAddedDoc.CreationDate.Year < TimeNow.Year)
                {
                    newSequenceNumber = "0001";
                    return newSequenceNumber;
                }
                else
                {
                    lastAddedDocSeq = lastAddedDocSeq + 1;
                    switch (lastAddedDocSeq.ToString().Length)
                    {
                        case 1:
                            {
                                newSequenceNumber = "000" + lastAddedDocSeq.ToString();
                                break;
                            }
                        case 2:
                            {
                                newSequenceNumber = "00" + lastAddedDocSeq.ToString();
                                break;
                            }
                        case 3:
                            {
                                newSequenceNumber = "0" + lastAddedDocSeq.ToString();
                                break;
                            }
                        case 4:
                            {
                                newSequenceNumber = lastAddedDocSeq.ToString();
                                break;
                            }
                        default:
                            break;
                    }

                    return newSequenceNumber;
                }
            }
            else
            {
                newSequenceNumber = "0001";
                return newSequenceNumber;
            }
        }
        private async Task LogHistory(DocumentsNumbers DocNumber)
        {

            var History = new DocumentNumberHistory(DocNumber);
            _documentNumberHistoryRepository.Add(History);
            await _documentNumberHistoryRepository.SaveChangesAsync();
        }



        #endregion


    }
}