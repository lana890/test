using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.HOC.RelatedSlug.Services.Interfcaes;
using EskaCMS.HOC.RelatedSlug.ViewModel;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Pages.Models;
using EskaCMS.Entities.Entity.Catalog;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Sites.Models;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Core.Services;

namespace EskaCMS.HOC.RelatedSlug.Services
{
    public class RelatedSlugServices : IRelatedSlugServices
    {
        private readonly IRepository<Product> _ProductRepository;
        private readonly IRepository<Brand> _BrandRepository;
        private readonly IRepository<Category> _CategoryRepository;
        private readonly IWorkContext _WorkContext;
        private readonly IRepository<SitesHostNames> _SitesHostNamesRepo;
        private readonly IdcmsLogService idcmsLogService;
        private readonly IRepository<Page> _PageRepository;
        private readonly IRepository<SlugInfo> _slugInfoRepo;
        private readonly IRepository<Posts> _postsRepository;

        public RelatedSlugServices(
            IWorkContext workContext
            , IRepository<Product> ProductRepositiory
            , IRepository<Brand> BrandRepositiory
            , IRepository<Category> CategoryRespository,
            IRepository<SitesHostNames> SitesHostNamesRepo,
            IdcmsLogService idcmsLogService,
            IRepository<Page> PageRepository,
            IRepository<SlugInfo> SlugInfoRepo,
            IRepository<Posts> PostsRepository)
        {

            _ProductRepository = ProductRepositiory;
            _CategoryRepository = CategoryRespository;
            _BrandRepository = BrandRepositiory;
            _WorkContext = workContext;
            _SitesHostNamesRepo = SitesHostNamesRepo;
            this.idcmsLogService=idcmsLogService;
            _PageRepository = PageRepository;
            _slugInfoRepo = SlugInfoRepo;
            _postsRepository = PostsRepository;
        }



        public async Task<List<SlugResultVm>> GetRelatedSlugsInfoBySlug(string slug)
        {
            try
            {

                var CurrentSiteId = _WorkContext.GetCurrentSiteId();
                var userId =await _WorkContext.GetCurrentUserId();

                var CurrentCulture = _WorkContext.GetCurrentCultureId();
                var hostname = _WorkContext.GetCurrentHostname();
                long? parentId = 0;
                var SlugTarget = await _slugInfoRepo.Query().Where(x => x.Slug == slug && x.SiteId == CurrentSiteId && x.CultureId == CurrentCulture).FirstOrDefaultAsync();

                parentId = await GetParentId(SlugTarget);

                List<SlugResultVm> SlugRelated = await GetRelatedSlug(CurrentSiteId, parentId, SlugTarget.Entity);



                var SiteHostNames = await _SitesHostNamesRepo.Query()
                    .Where(h => h.SiteId == CurrentSiteId && h.IsActive)
                    .OrderBy(h => h.Id)
                    .Include(x => x.Culture)
                    .ThenInclude(x => x.Culture)

                    .Select(x => new SitesHostNamesResultVM
                    {
                        CultureId = x.CultureId,
                        Prefix = x.Prefix,
                        Hostname = x.HostName,
                        CultureName = x.Culture.Culture.Name

                    }).ToListAsync();

                // A culture can have more than one active hostname. Prefer the row whose
                // HostName matches the current request host so the language switcher stays on
                // the same domain; otherwise fall back to the oldest active row deterministically,
                // instead of DistinctBy's arbitrary, order-dependent pick.
                SiteHostNames = SiteHostNames
                    .GroupBy(x => x.CultureId)
                    .Select(g => g.FirstOrDefault(h => !string.IsNullOrWhiteSpace(hostname)
                                                       && string.Equals(h.Hostname?.Trim(), hostname, StringComparison.OrdinalIgnoreCase))
                                 ?? g.First())
                    .ToList();

                System.Collections.Generic.List<SlugResultVm> Result = new System.Collections.Generic.List<SlugResultVm>();

                foreach (var HS in SiteHostNames)
                {
                    if (HS.CultureId == CurrentCulture)
                    {

                        continue;
                    }
                    var ResultItem = new SlugResultVm();

                    ResultItem.HostName = HS.Hostname;
                    ResultItem.Prefix = HS.Prefix;
                    ResultItem.CultureId = HS.CultureId;
                    ResultItem.CultureName = HS.CultureName;
                    var SlugInfo = SlugRelated.Where(x => x.CultureId == HS.CultureId).Select(x => new SlugsInfo
                    {
                        Name = x.Name,
                        Slug = x.Slug,
                    }).FirstOrDefault();
                    ResultItem.Name = SlugInfo == null ? "" : SlugInfo.Name;
                    ResultItem.Slug = SlugInfo == null ? "" : SlugInfo.Slug;
                    Result.Add(ResultItem);

                }
                return Result;

            }

            catch (Exception E)
            {
                throw E;
            }


        }



        public async Task<long?> GetParentId(SlugInfo slugInfo)
        {
            try
            {
                switch (slugInfo.Entity)
                {
                    case EEntity.Product:
                        return await _ProductRepository.Query()
                       .Include(x=>x.ProductDetails)
                       .Where(x => x.ProductDetails.Any(o=>o.SlugInfoId == slugInfo.Id) && x.SiteId == slugInfo.SiteId && x.Status == EStatus.Published)
                       .Select(x => x.Id)
                       .FirstOrDefaultAsync();

                        break;
                  

                    case EEntity.Brand:
                        return await _BrandRepository.Query()
                            .Where(x => x.BrandDetails.Any(o => o.SlugInfoId == slugInfo.Id) && x.SiteId == slugInfo.SiteId && x.Status == EStatus.Published)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();
                        break;

                    case EEntity.Post:
                        return await _postsRepository.Query()
                              .Where(x => x.SlugInfoId == slugInfo.Id && x.SiteId == slugInfo.SiteId && x.Status == EStatus.Published)
                    .Select(x => x.ParentId)
                    .FirstOrDefaultAsync();
                        break;

                    case EEntity.Page:
                        return await _PageRepository.Query()
                             .Where(x => x.SlugInfoId == slugInfo.Id && x.SiteId == slugInfo.SiteId && x.Status == EStatus.Published)
                    .Select(x => x.ParentId)
                    .FirstOrDefaultAsync();
                        break;

                    case EEntity.Category:
                        return await _CategoryRepository.Query().Include(x => x.CategoryDetails).ThenInclude(x => x.SlugInfo)
                             .Where(x => x.CategoryDetails.Any(d => d.SlugInfoId == slugInfo.Id) && x.SiteId == slugInfo.SiteId && x.Status == EStatus.Published)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();
                        break;

                    default:
                        return 0;
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        private async Task<List<SlugResultVm>> GetRelatedSlug(long SiteId, long? parentId, EEntity Slugentity)
        {
            try
            {
               var CurrentCulture = _WorkContext.GetCurrentCultureId();

                //Note If ParentId Will Send As SlugInfoId If Was Brand Or Product

                switch (Slugentity)
                {

                    case EEntity.Product:
                        return await _ProductRepository.Query()
                              .Include(x=>x.ProductDetails)
                              .Where(x =>x.SiteId == SiteId && x.Status == EStatus.Published && x.ProductDetails.Any(x => x.ProductId == parentId))
                              .Include("ProductDetails.SlugInfo")
                              .Include("ProductDetails.Culture")
                              .SelectMany(x=>x.ProductDetails)
                              .Select(x => new SlugResultVm { Slug = x.SlugInfo.Slug, CultureId = x.CultureId, CultureName = x.Culture.CultureId, Name = x.Name })
                              .ToListAsync();
                        break;

                    case EEntity.Post:
                        return await _postsRepository.Query()
                              .Where(x => x.ParentId == parentId && x.SiteId == SiteId && x.Status == EStatus.Published)
                             .Select(x => new SlugResultVm { Slug = x.SlugInfo.Slug, CultureId = x.CultureId, CultureName = x.Culture.CultureId, Name = x.ShortDescription })
                              .ToListAsync();
                        break;

                    case EEntity.Brand:
                        return await _BrandRepository.Query()
                            .Include(x => x.BrandDetails)
                            .Where(x => x.SiteId == SiteId && x.Status == EStatus.Published && x.BrandDetails.Any(x => x.BrandId==parentId))
                            .Include("BrandDetails.SlugInfo")
                            .Include("BrandDetails.Culture")
                            .SelectMany(x=>x.BrandDetails)
                            .Select(x => new SlugResultVm { Slug = x.SlugInfo.Slug, CultureId = x.CultureId, CultureName = x.Culture.CultureId, Name = x.Name })
                              .ToListAsync();
                        break;

                    case EEntity.Page:
                        return await _PageRepository.Query()
                                                          .Where(x => x.ParentId == parentId && x.SiteId == SiteId && x.Status == EStatus.Published)

                           .Select(x => new SlugResultVm { Slug = x.SlugInfo.Slug, CultureId = x.CultureId, CultureName = x.Culture.CultureId, Name = x.Name })
                              .ToListAsync();
                        break;

                    case EEntity.Category:
                        return await _CategoryRepository.Query().Include(x => x.CategoryDetails)
                                                          .Include("CategoryDetails.SlugInfo")
                                                          .Include("CategoryDetails.Culture")
                                                          .Where(x => x.SiteId == SiteId && x.Status == EStatus.Published && (parentId==null||x.CategoryDetails.Any(x => x.CategoryId == parentId)))
                                                          .SelectMany(x => x.CategoryDetails)

                           .Select(x => new SlugResultVm { Slug = x.SlugInfo.Slug, CultureId = x.CultureId.Value, CultureName = x.Culture == null ? "" : x.Culture.CultureId, Name = x.Name })
                              .ToListAsync();
                        break;

                    default:
                        return new List<SlugResultVm>();

                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
