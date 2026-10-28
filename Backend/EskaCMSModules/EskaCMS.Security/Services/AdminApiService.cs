using EskaCMS.BlogArchive.Models;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services.Interface;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Pages.Models;
using EskaCMS.Pages.Services.Interfaces;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using EskaCMS.Sites.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using MimeDetective.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Services
{


    public class AdminApiService : IAdminApi
    {
        private readonly IRepository<SitesHostNames> _SitesHostNameRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Entities.Entity.Core.Category> _categoryRepository;
        private readonly IRepository<ProductDetails> productDetailsRepository;
        private readonly IRepository<BrandDetails> brandDetailsRepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository;
        private readonly IRepository<CategoryDetails> categoryDetailsRepository;
        private readonly IRepository<Brand> _brandRepository;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Page> _pageRepository;
        private readonly IRepository<Posts> _PostRepository;
        private readonly IPages _pageServices;
        private readonly ISlugServices _slugServices;
        private readonly ISites _SitesService;

        public AdminApiService(ISites SitesService, IPages pageServices, IRepository<Posts> PostRepository,
                               IRepository<SitesHostNames> SitesHostNameRepository,
                               IRepository<Product> ProductRepository,
                               IRepository<Entities.Entity.Core.Category> CategoryRepository,
                               IRepository<ProductDetails> ProductDetailsRepository,
                               IRepository<BrandDetails> BrandDetailsRepository, IRepositoryWithTypedId<SiteSettings,
                string> siteSettingsRepository,
                               IRepository<Entities.Entity.Core.CategoryDetails> CategoryDetailsRepository,
                               IRepository<Brand> brandRepository,
                               IWorkContext workContext, IRepository<Page> pageRepository, ISlugServices slugServices)

        {
            _SitesHostNameRepository = SitesHostNameRepository;
            _productRepository = ProductRepository;
            _categoryRepository = CategoryRepository;
            productDetailsRepository = ProductDetailsRepository;
            brandDetailsRepository = BrandDetailsRepository;
            this.siteSettingsRepository=siteSettingsRepository;
            categoryDetailsRepository = CategoryDetailsRepository;
            _brandRepository = brandRepository;
            _workContext = workContext;
            _pageRepository = pageRepository;
            _PostRepository = PostRepository;
            _pageServices = pageServices;
            _slugServices = slugServices;
            _SitesService = SitesService;
        }

        public async Task<SiteMapViewModel> GetSiteDetailByHostName(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                return null;

            hostName = hostName.Trim().ToLower();

            // 1️⃣ Get site by hostname
            var siteHost = await _SitesHostNameRepository.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.HostName.ToLower() == hostName);

            if (siteHost == null)
                return null;

            var siteId = siteHost.SiteId;

            // 2️⃣ Get all hostnames + determine culture
            var hostNames = await _SitesService.GetHostNameBySiteId(siteId, hostName);

            long cultureId = hostNames.Count > 1
                ? hostNames.Where(x => x.HostNames.ToLower() == hostName)
                           .Select(x => x.CultureId)
                           .FirstOrDefault()
                : hostNames.Select(x => x.CultureId).FirstOrDefault();

            var siteMap = new SiteMapViewModel
            {
                HostNames = hostNames
            };
            var settingValue = await siteSettingsRepository.Query()
              .Where(x => x.Id == "SeoExcludedPageIds")
              .Select(x => x.Value)
              .FirstOrDefaultAsync();
            var excludedIds = string.IsNullOrWhiteSpace(settingValue)
              ? new List<long>()
              : settingValue
                  .Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(x => long.Parse(x.Trim()))
                  .ToList();
         
            var pagesQuery = _pageRepository.Query().Include(x=>x.Culture).ThenInclude(x=>x.Culture).Include(x=>x.SlugInfo)
            .AsNoTracking()
            .Where(x => x.SiteId == siteId
             && x.Status == EStatus.Published
             && x.CultureId == cultureId);

            if (excludedIds.Any())
            {
                pagesQuery = pagesQuery
                    .Where(x => !excludedIds.Contains(x.Id))
                    .Where(x => x.ParentId == null || !excludedIds.Contains(x.ParentId.Value));
            }

            // 3️⃣ Pages
            siteMap.Pages = await pagesQuery
                 .Select(x => new PageSiteMapVM
                 {
                     ParentId = x.ParentId ?? 0,
                     CultureId = x.CultureId,
                     PageSlug = x.SlugInfo.Slug,
                     Name = x.Name,
                     Culture = x.Culture.Culture.Name,
                     ModificationDate = x.ModificationDate
                 })
                 .ToListAsync();


            // 4️⃣ Posts
            siteMap.Posts = await _PostRepository.Query()
                .AsNoTracking()
                .Where(x => x.SiteId == siteId
                         && x.Status == EStatus.Published
                         && x.CultureId == cultureId)
                .Select(x => new PostSiteMapVM
                {
                    ParentId = x.ParentId ?? 0,
                    PostSlug = x.SlugInfo != null ? x.SlugInfo.Slug : string.Empty,
                    CultureId = x.CultureId,
                    ModificationDate = x.ModificationDate
                })
                .ToListAsync();

            // 5️⃣ Products
            var products = await _productRepository.Query()
                .AsNoTracking()
                .Where(x => x.SiteId == siteId
                         && x.Status == EStatus.Published)
                .Where(x => !x.LinkedProductLinks.Any(p => p.LinkedProductId == x.Id && p.LinkType == ItemLinkType.Sub))
                .Select(x => new EcommerceSiteMapVM
                {
                    ParentId = x.Id,
                    Slug = x.ProductDetails
                             .Where(d => d.CultureId == cultureId)
                             .Select(d => d.SlugInfo.Slug)
                             .FirstOrDefault(),
                    RelativeUrl = x.Categories
                             .SelectMany(c => c.Category.CategoryDetails)
                             .Where(d => d.CultureId == cultureId)
                             .Select(d => d.SlugInfo.Slug)
                             .FirstOrDefault(),
                    CultureId = x.ProductDetails
                                 .Where(d => d.CultureId == cultureId)
                                 .Select(d => d.CultureId)
                                 .FirstOrDefault(),
                    ModificationDate = x.ModificationDate
                })
                .ToListAsync();

            siteMap.Products = products
                .Select(x =>
                {
                    x.RelativeUrl = BuildProductSitemapUrl(x.RelativeUrl, x.Slug, x.ParentId);
                    return x;
                })
                .ToList();

            // 6️⃣ Categories
            siteMap.Categories = await _categoryRepository.Query()
                .AsNoTracking()
                .Where(x => x.SiteId == siteId
                         && x.Status == EStatus.Published
                         && x.CategoryDetails.Any(cd => cd.SlugInfoId.HasValue))
                .Select(x => new EcommerceSiteMapVM
                {
                    ParentId = x.Id,
                    Slug = x.CategoryDetails
                             .Where(cd => cd.CultureId == cultureId)
                             .Select(cd => cd.SlugInfo.Slug)
                             .FirstOrDefault(),
                    CultureId = x.CategoryDetails
                                 .Where(cd => cd.CultureId == cultureId)
                                 .Select(cd => cd.CultureId)
                                 .FirstOrDefault(),
                    ModificationDate = x.ModificationDate
                })
                .ToListAsync();

            // 7️⃣ Brands
            siteMap.Brands = await _brandRepository.Query()
                .AsNoTracking()
                .Where(x => x.SiteId == siteId
                         && x.Status == EStatus.Published)
                .Select(x => new EcommerceSiteMapVM
                {
                    ParentId = x.Id,
                    Slug = x.BrandDetails
                             .Where(d => d.CultureId == cultureId)
                             .Select(d => d.SlugInfo.Slug)
                             .FirstOrDefault(),
                    CultureId = x.BrandDetails
                                 .Where(d => d.CultureId == cultureId)
                                 .Select(d => d.CultureId)
                                 .FirstOrDefault(),
                    ModificationDate = x.ModificationDate
                })
                .ToListAsync();

            return siteMap;
        }


        public async Task<SiteMapDetailsVM> GetSiteDetailsByHostName(string HostName)
        {

            SiteMapDetailsVM siteMapDetailsVM = new SiteMapDetailsVM();
            long? PageCategory = null;
            var SitesHostnames = _SitesHostNameRepository.Query()
                      .Where(x => x.HostName.ToLower() == HostName.ToLower())
                      .Select(x => new { x.Site.Id }).ToList();
            siteMapDetailsVM.HostNames = await _SitesService.GetHostNameBySiteId(SitesHostnames.FirstOrDefault().Id,HostName);
            siteMapDetailsVM.Pages = await _pageServices.GetAllPagesBysiteId(SitesHostnames.FirstOrDefault().Id, EStatus.Active, PageCategory = null);



            siteMapDetailsVM.Posts = _PostRepository
                                        .Query()
                                        .Where(post => post.SiteId == SitesHostnames.FirstOrDefault().Id)
                                        .Include(x => x.SlugInfo)
                                        .Select(post => new PostsViewModel
                                        {
                                            Id = post.Id,
                                            Slug = post.SlugInfo == null ? string.Empty : post.SlugInfo.Slug,
                                            CultureId = post.CultureId
                                        }).ToList();
            return siteMapDetailsVM;


        }
        // ===============================
        // 🌐 Related Entities for Sitemap
        // ===============================

        public async Task<List<PageCultureDetailsVM>> GetRelatedPages()
        {
            var settingValue = await siteSettingsRepository.Query()
                .Where(x => x.Id == "SeoExcludedPageIds")
                .Select(x => x.Value)
                .FirstOrDefaultAsync();

            // ✅ If setting does not exist or empty → no exclusions
            var excludedIds = string.IsNullOrWhiteSpace(settingValue)
                ? new List<long>()
                : settingValue
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => long.Parse(x.Trim()))
                    .ToList();

            var query = _pageRepository.Query()
                .AsNoTracking()
                .Where(x => x.Status != EStatus.Deleted);

            // ✅ Apply exclusion ONLY if setting exists
            if (excludedIds.Any())
            {
                query = query
                    .Where(x => !excludedIds.Contains(x.Id))
                    .Where(x => x.ParentId == null || !excludedIds.Contains(x.ParentId.Value));
            }

            return await query
                .Select(x => new PageCultureDetailsVM
                {
                    Slug = x.SlugInfo.Slug,
                    ParentId = x.ParentId,
                    CultureCode = x.Culture.CultureId,
                    CultureId = x.CultureId
                })
                .ToListAsync();
        }

        public async Task<List<PostCultureDetailsVm>> GetRelatedPosts()
        {
            return await _PostRepository.Query()
                .AsNoTracking()
                .Where(x => x.Status != EStatus.Deleted )
                .Select(x => new PostCultureDetailsVm
                {
                    ParentId = x.ParentId,
                    Slug = x.SlugInfo.Slug,
                    CultureCode = x.Culture.CultureId,
                    CultureId = x.CultureId
                })
                .ToListAsync();
        }

        public async Task<List<EcommerceDetailsSiteMapCiewModel>> GetRelatedProducts()
        {
            var products = await productDetailsRepository.Query()
                .AsNoTracking()
                .Where(x =>  x.Product.Status != EStatus.Deleted)
                .Select(x => new EcommerceDetailsSiteMapCiewModel
                {
                    ParentId = x.ProductId,
                    Slug = x.SlugInfo.Slug,
                    RelativeUrl = x.Product.Categories
                             .SelectMany(c => c.Category.CategoryDetails)
                             .Where(d => d.CultureId == x.CultureId)
                             .Select(d => d.SlugInfo.Slug)
                             .FirstOrDefault(),
                    CultureCode = x.Culture.CultureId,
                    CultureId = x.CultureId
                })
                .ToListAsync();

            return products
                .Select(x =>
                {
                    x.RelativeUrl = BuildProductSitemapUrl(x.RelativeUrl, x.Slug, x.ParentId);
                    return x;
                })
                .ToList();
        }

        private static string BuildProductSitemapUrl(string categorySlug, string productSlug, long? productId)
        {
            if (string.IsNullOrWhiteSpace(categorySlug) || string.IsNullOrWhiteSpace(productSlug) || !productId.HasValue)
                return productSlug;

            return $"product/{categorySlug}/{productSlug}?code={productId.Value}";
        }

        public async Task<List<EcommerceDetailsSiteMapCiewModel>> GetRelatedCategory()
        {
            return await categoryDetailsRepository.Query()
                .AsNoTracking()
                .Where(x => x.SlugInfoId.HasValue && x.Category.Status != EStatus.Deleted)
                .Select(x => new EcommerceDetailsSiteMapCiewModel
                {
                    ParentId = x.CategoryId,
                    Slug = x.SlugInfo.Slug,
                    CultureCode = x.Culture.CultureId,
                    CultureId = x.CultureId
                })
                .ToListAsync();
        }

        public async Task<List<EcommerceDetailsSiteMapCiewModel>> GetRelatedBrands()
        {
            return await brandDetailsRepository.Query()
                .AsNoTracking()
                .Where(x =>  x.Brand.Status != EStatus.Deleted)
                .Select(x => new EcommerceDetailsSiteMapCiewModel
                {
                    ParentId = x.BrandId,
                    Slug = x.SlugInfo.Slug,
                    CultureCode = x.Culture.CultureId,
                    CultureId = x.CultureId
                })
                .ToListAsync();
        }

        //public async Task<List<EcommerceRelatedSiteMapVM>> GetRelatedProduct(long? ParentProductId)
        //{
        //    try
        //    {
        //        List<EcommerceRelatedSiteMapVM> product = await _productRepository.
        //      Query().
        //      .Include(x => x.Culture)
        //      .Include(x => x.SlugInfo)
        //      .Select(x => new EcommerceRelatedSiteMapVM
        //      {
        //          Slug = x.SlugInfo.Slug,
        //          CultureCode = x.Culture.CultureId,
        //          CultureId = x.CultureId,
        //      }).ToListAsync();
        //        return product;
        //    }
        //    catch (System.Exception ex)
        //    {

        //        throw ex;
        //    }
        //}

        //public async Task<List<EcommerceRelatedSiteMapVM>> GetRelatedBrand(long? parentId)
        //{
        //    try
        //    {
        //        List<EcommerceRelatedSiteMapVM> Brand = await _brandRepository.
        //   Query().
        //   Where(x => x.ParentId == parentId.Value)
        //   .Include(x => x.BrandsCulture)
        //   .Include(x => x.SlugInfo)
        //   .Select(x => new EcommerceRelatedSiteMapVM
        //   {
        //       Slug = x.SlugInfo.Slug,
        //       CultureCode = x.BrandsCulture.CultureId,
        //       CultureId = x.BrandsCultureId,
        //   }).ToListAsync();
        //        return Brand;
        //    }
        //    catch (System.Exception ex)
        //    {

        //        throw ex;
        //    }
        //}
    }
}
