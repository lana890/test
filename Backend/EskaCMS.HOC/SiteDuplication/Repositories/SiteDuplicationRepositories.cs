using EskaCMS.Entities.Entity;
using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.DataAccess.Data;
//using EskaCMS.Entities.Entity.DynamicForms;


namespace EskaCMS.HOC.SiteDuplication.Repositories
{
    public class SiteDuplicationRepositories : ISiteDuplicationRepositories
    {
        //private readonly IRepository<FormItem> _pricingMatrixRepository;
        //private readonly IRepository<FormItemDetails> _pricingMatrixDetailsRepository;
        private readonly EskaDCMSDbContext _context;
        //private readonly IRepository<FormFieldGroup> specificationGroupRepository;
        //private readonly IRepository<Classifications> _classificationsRepository;
        //private readonly IRepository<FormField> _pricingSpecificationRepository;
        //private readonly IRepository<FormFieldDetails> _pricingSpecificationDetailsRepository;
        //private readonly IRepository<Criteria> _criteriaRepository;
        //private readonly IRepository<FormFieldGroup> _SpecificationGroupRepository;
        private readonly IRepositoryWithTypedId<Country, string> _CountryRepository;
        private readonly IRepository<Address> _addressRepository;
        private readonly IRepository<UserAddress> _userAddressRepository;
        private readonly IRepository<Posts> _postRepository;
        private readonly IRepository<PostsCategories> _postsCategoriesRepository;
        private readonly IRepository<PostMedias> _postMediasRepository;
        private readonly IRepository<EskaCMS.Entities.Entity.Core.Media> _mediaRepository;
        private readonly ISlugServices _slugServices;
        private readonly IRepository<PostsAuther> _postAuther;

        public SiteDuplicationRepositories(
                                        //IRepository<FormItem> pricingMatrixRepository,
                                        //IRepository<FormItemDetails> pricingMatrixDetailsRepository,
                                        EskaDCMSDbContext context,
                                        //IRepository<FormFieldGroup> SpecificationGroupRepository,
                                        //IRepository<Classifications> ClassificationsRepository,
                                        //IRepository<FormField> PricingSpecificationRepository,
                                        //IRepository<FormFieldDetails> PricingSpecificationDetailsRepository,
                                        //IRepository<Criteria> CriteriaRepository,
                                        IRepositoryWithTypedId<Country, string> CountryRepository,
                                        IRepository<Address> AddressRepository,
                                        IRepository<UserAddress> UserAddressRepository,
                                        IRepository<Posts> postRepository,
                                        IRepository<PostsCategories> PostsCategoriesRepository,
                                        IRepository<PostMedias> PostMediasRepository,
                                        IRepository<EskaCMS.Entities.Entity.Core.Media> MediaRepository,
                                        IRepository<Tags> TagsRepository,
                                        ISlugServices SlugServices,
                                        IRepository<PostsAuther> PostAuther



)
        {
            //_pricingMatrixRepository = pricingMatrixRepository;
            //_pricingMatrixDetailsRepository = pricingMatrixDetailsRepository;
            _context = context;
            //_SpecificationGroupRepository = SpecificationGroupRepository;
            //_classificationsRepository = ClassificationsRepository;
            //_pricingSpecificationRepository = PricingSpecificationRepository;
            //_pricingSpecificationDetailsRepository = PricingSpecificationDetailsRepository;
            //_criteriaRepository = CriteriaRepository;
            _CountryRepository = CountryRepository;
            _addressRepository = AddressRepository;
            _userAddressRepository = UserAddressRepository;
            _postRepository = postRepository;
            _postsCategoriesRepository = PostsCategoriesRepository;
            _postMediasRepository = PostMediasRepository;
            _mediaRepository = MediaRepository;
            _slugServices = SlugServices;
            _postAuther = PostAuther;
        }

        //#region PricingQuotationsDublicate

        //public async Task<bool?> DublicatePricingQuotations(long OriginalSite, long DublicateSite)
        //{

        //    try
        //    {

        //        await CreatePricingMatrix(OriginalSite, DublicateSite);
        //        return true;


        //    }
        //    catch (Exception ex)
        //    {

        //        throw ex;
        //    }
        //}

        //private async Task<bool> CreatePricingMatrix(long originalSite, long dublicateSite)
        //{
        //    using (var DbTransfer = _context.Database.BeginTransaction()) {

        //        try
        //        {
        //            List<FormItem> DublicatePricingMatrix = new();

        //            var Query = await _pricingMatrixRepository.Query().Include(x => x.Specifications)
        //                                                              .Include(x => x.SpecificationGroups)
        //                                                              .Include(x => x.FormItemDetails)
        //                                                              .Include(x => x.Classifications)
        //                                                              .ThenInclude(x => x.CriteriaList)
        //                                                              .ThenInclude(x => x.Specification)
        //                                                              .ThenInclude(x => x.FormFieldDetails)
        //                                                              .Where(x => originalSite == x.SiteId)
        //                                                              .ToListAsync();

        //            foreach (var pricing in Query)
        //            {
        //                var ObjPricingMatrix = new FormItem();
        //                IList<FormItemDetails> FormItemDetails = new List<FormItemDetails>();
        //                IList<FormFieldGroup> specificationGroups = new List<FormFieldGroup>();

        //                ObjPricingMatrix.ThumbnailId = pricing.ThumbnailId;
        //                ObjPricingMatrix.Icon = pricing.Icon;
        //                ObjPricingMatrix.CreatedById = pricing.CreatedById;
        //                ObjPricingMatrix.CreationDate = pricing.CreationDate;
        //                ObjPricingMatrix.Status = pricing.Status;
        //                ObjPricingMatrix.SiteId = dublicateSite;
        //                DublicatePricingMatrix.Add(ObjPricingMatrix);


        //                await _pricingMatrixRepository.SaveChangesAsync();
        //                foreach (var PricingMatrixItem in pricing.SpecificationGroups)
        //                {
        //                    specificationGroups.Add(new FormFieldGroup()
        //                    {
        //                        MatrixId = ObjPricingMatrix.Id,
        //                        Name = PricingMatrixItem.Name,
        //                        Status = ObjPricingMatrix.Status
        //                    });


        //                }
        //                _SpecificationGroupRepository.AddRange(specificationGroups);

        //                await _SpecificationGroupRepository.SaveChangesAsync();

        //                await CreateClassifications(pricing.Classifications, ObjPricingMatrix.Id, dublicateSite, ObjPricingMatrix.Id);

        //                foreach (var PricingMatrixItem in pricing.FormItemDetails)
        //                {
        //                    FormItemDetails.Add(new FormItemDetails()
        //                    {
        //                        Description = PricingMatrixItem.Description,
        //                        Name = PricingMatrixItem.Name,
        //                        PricingMatrixId = ObjPricingMatrix.Id,
        //                        CultureId = PricingMatrixItem.CultureId
        //                    });


        //                }


        //                _pricingMatrixDetailsRepository.AddRange(FormItemDetails);

        //                await _pricingMatrixDetailsRepository.SaveChangesAsync();

        //                DbTransfer.Commit();


        //            }
        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            DbTransfer.Rollback();

        //            throw ex;
        //        }
        //    }
        //}

        //private async Task<bool> CreateClassifications(IList<Classifications> classifications, long id, long DublicateSite, long ObjPricingMatrix)
        //{
        //    try
        //    {
        //        foreach (var classification in classifications)
        //        {
        //            var ObjClassificationItem = new Classifications();

        //            ObjClassificationItem.CreationDate = DateTime.Now;
        //            ObjClassificationItem.Status = classification.Status;
        //            ObjClassificationItem.CreatedById = classification.CreatedById;
        //            ObjClassificationItem.Name = classification.Name;
        //            ObjClassificationItem.Description = classification.Description;
        //            ObjClassificationItem.CreationDate = classification.CreationDate;
        //            ObjClassificationItem.PricingMatrixId = id;
        //            ObjClassificationItem.SiteId = DublicateSite;


        //            _classificationsRepository.Add(ObjClassificationItem);
        //            await _classificationsRepository.SaveChangesAsync();

        //            await CreateCritierList(classification.CriteriaList, ObjClassificationItem.Id, DublicateSite, ObjPricingMatrix);
        //        }
        //        return true;

        //    }
        //    catch (Exception ex)
        //    {

        //        throw ex;
        //    }
        //}

        //private async Task<bool> CreateCritierList(IList<Criteria> criteriaList, long id, long SiteId, long MatrixId)
        //{
        //    try
        //    {
        //        foreach (var criteria in criteriaList)
        //        {
        //            FormField pricingSpecification = new FormField();

        //            pricingSpecification.SiteId = SiteId;
        //            pricingSpecification.OneOfOptionsType = criteria.Specification.OneOfOptionsType;
        //            pricingSpecification.Status = criteria.Specification.Status;
        //            pricingSpecification.InputType = criteria.Specification.InputType;
        //            pricingSpecification.PricingMatrixId= MatrixId;
        //            _pricingSpecificationRepository.Add(pricingSpecification);
        //            await _pricingSpecificationRepository.SaveChangesAsync();
        //            foreach (var SpecificationDetails in criteria.Specification.FormFieldDetails)
        //            {
        //                var SpecificationDetail = new FormFieldDetails
        //                {
        //                    PlaceHolder = SpecificationDetails.PlaceHolder,
        //                    AdditionalInfo = SpecificationDetails.AdditionalInfo,
        //                    ClassesNames = SpecificationDetails.ClassesNames,
        //                    CultureId = SpecificationDetails.CultureId,
        //                    GroupId = SpecificationDetails.GroupId,
        //                    StaticApiLink = SpecificationDetails.StaticApiLink,
        //                    CategoryId = SpecificationDetails.CategoryId,
                          
        //                    DataSourceId = SpecificationDetails.DataSourceId,
        //                    PricingSpecificationId = pricingSpecification.Id,

        //                };
        //                _pricingSpecificationDetailsRepository.Add(SpecificationDetail);
        //                await _pricingSpecificationDetailsRepository.SaveChangesAsync();
        //            }


        //            var ObjCriteria = new Criteria();

        //            ObjCriteria.ClassificationId = id;
        //            ObjCriteria.CreatedById = criteria.CreatedById;
        //            ObjCriteria.CreationDate = DateTime.Now;
        //            ObjCriteria.SiteId = SiteId;
        //            ObjCriteria.Status = criteria.Status;
        //            ObjCriteria.Name = criteria.Name;
        //            ObjCriteria.Value = criteria.Value;
        //            ObjCriteria.ConditionsType = criteria.ConditionsType;
        //            ObjCriteria.OperationType = criteria.OperationType;
        //         //   ObjCriteria.SubCategoryId = criteria.SubCategoryId == 0 ? null : criteria.SubCategoryId;
        //            ObjCriteria.Type = criteria.Type;
        //            ObjCriteria.CategoryId = criteria.CategoryId;
        //            ObjCriteria.SpecificationId = criteria.SpecificationId;


        //            _criteriaRepository.Add(ObjCriteria);
        //            await _criteriaRepository.SaveChangesAsync();

        //        }
        //        return true;

        //    }

        //    catch (Exception ex)
        //    {

        //        throw ex;
        //    }
        //}

        //#endregion

        #region  Address
        public async Task<bool> DublicateAddress(long OriginalSite, long DublicateSite)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var DbTrans = await _context.Database.BeginTransactionAsync();
            try
            {

                    await DublicateAddressById(OriginalSite, DublicateSite);
                    await DbTrans.CommitAsync();
                    return true;


                }
                catch (Exception EX)
                {
                    await DbTrans.RollbackAsync();

                    throw EX;
                }
            });
        }

        private async Task<bool> DublicateAddressById(long originalSite, long dublicateSite)
        {
            try
            {
                var ListQuery = await _addressRepository.Query()
                                                        .Where(x => x.SiteId == originalSite)
                                                        .Include(x => x.Country)
                                                        .ThenInclude(x => x.StatesOrProvinces)
                                                        .Include(x => x.District)
                                                        .ThenInclude(x => x.StateOrProvince)
                                                        .AsNoTracking()
                                                        .ToListAsync();
                var address = new List<Address>();

                foreach (var Address in ListQuery)
                {
                
                    var InsertAddress =new Address();

                    InsertAddress.AddressLine1=Address.AddressLine1;
                    InsertAddress.AddressLine2 = Address.AddressLine2;
                    InsertAddress.CountryId = Address.CountryId;
                    InsertAddress.Latitude = Address.Latitude;
                    InsertAddress.SiteId = dublicateSite;
                    InsertAddress.Longitude = Address.Longitude;
                    InsertAddress.City=Address.City;
                    InsertAddress.ContactName=Address.ContactName;
                    InsertAddress.Phone = Address.Phone;
                    InsertAddress.StateOrProvince = Address.StateOrProvince;
                    InsertAddress.StateOrProvinceId=Address.StateOrProvinceId;
                    InsertAddress.District=Address.District;
                    InsertAddress.DistrictId = Address.DistrictId;
                    InsertAddress.Country = Address.Country;
                    InsertAddress.ZipCode = Address.ZipCode;

                    address.Add(InsertAddress);

                }
                _addressRepository.AddRange(address);
                await _addressRepository.SaveChangesAsync();

                await CreateUserAddressDublicate(address, ListQuery);

                return true;
                                                        
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<bool> CreateUserAddressDublicate(List<Address> addressesDublicate,List<Address> AddressOriginal)
        {
            try
            {
                List<UserAddress> userAddresses = new();

                var Query =await _userAddressRepository.Query().Where(x => AddressOriginal.Any(v => v.Id == x.AddressId)).ToListAsync();

                foreach (var item in Query.Zip(addressesDublicate, (a, b) => new { A = a, B = b })) 
                {
                    var Useraddress = new UserAddress();
                    Useraddress.AddressId = item.B.Id;
                    Useraddress.UserId = item.A.UserId;
                    Useraddress.AddressType=item.A.AddressType;
                    Useraddress.LastUsedOn = item.A.LastUsedOn;

                    userAddresses.Add(Useraddress);

                }
                _userAddressRepository.AddRange(userAddresses);
              await  _userAddressRepository.SaveChangesAsync();

                return true;
               
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #endregion

        #region  BlogArchived
        public async Task<bool> DublicateBlogArchived(long originalSite, long dublicateSite)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var DbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                    var Query = await _postRepository.Query().Where(x => x.SiteId == originalSite)
                                                      .Include(x => x.Thumbnail)
                                                      .Include(x => x.SlugInfo)
                                                      .Include(x => x.PostMedias)
                                                      .Include(x => x.PostsAuthers)
                                                      .Include(x => x.PostTemplate)
                                                      .AsNoTracking()
                                                      .ToListAsync();
                    long ParentId = 0;

                    List<Posts> posts = new List<Posts>();
                    List<Posts> RelativePosts = new List<Posts>();
                    foreach (var post in Query)
                    {

                        var DublicatePost = new Posts();

                        Core.Models.SlugManagementVm slugManagementVm = new()
                        {
                            Id = post.SlugInfoId,

                            Slug = post.Slug,

                            CultureId = post.CultureId,
                            SiteId = dublicateSite,

                            Entity = Entities.Enum.GeneralEnums.EEntity.Post,

                        };
                        DublicatePost.SlugInfoId = await _slugServices.UpsertAndGetId(slugManagementVm);

                        DublicatePost.Title = string.IsNullOrEmpty(post.Title) ? RelativePosts[0].Title : post.Title;
                        ;
                        DublicatePost.IsPrivate = post.IsPrivate;
                        DublicatePost.SiteId = dublicateSite;
                        DublicatePost.ExpiryDate = post.ExpiryDate;
                        DublicatePost.ThumbnailId = post.ThumbnailId;
                        DublicatePost.Settings = post.Settings;
                        DublicatePost.PostTemplateId =  post.PostTemplateId != 0 ? post.PostTemplateId : RelativePosts[0].PostTemplateId;
                        DublicatePost.CultureId = post.CultureId;
                        DublicatePost.PublishDate = post.PublishDate;


                        
                        DublicatePost.ParentId = ParentId == 0 ? DublicatePost.Id : ParentId;
                        ParentId = ParentId == 0 ? DublicatePost.Id : ParentId;
                    }
                    _postRepository.AddRange(posts);


                    return true;

                }
                catch (Exception ex)
                {
                    await DbTrans.RollbackAsync();

                    throw ex;
                }
            });
        }
        #endregion
    }
}
