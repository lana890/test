using EskaCMS.Engine.Elastic.Services.Interfaces;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Models.ElasticVM;
using Microsoft.EntityFrameworkCore;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Engine.Elastic.BGS;

public class ProductElasticSearchBackgroundService : BaseBackgroundService
{
    public readonly ILogger<ProductElasticSearchBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private readonly System.IServiceProvider _serviceProvider;

    public ProductElasticSearchBackgroundService(ILogger<ProductElasticSearchBackgroundService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
       : base(nameof(ProductElasticSearchBackgroundService), logger, serviceProvider, configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public async override Task<bool> Execute()
    {
        using (var scope = _serviceProvider.CreateScope())
        {


            try
            {
                await AddUpdateProductCatalog(scope);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Product Elastic Search Background Service {ex.ToString()}");

            }
            return true;
        }

    }

    private async Task AddUpdateProductCatalog(IServiceScope scope)
    {
        var products = await GetProductsCatalog(scope);
        var elsticService = scope.ServiceProvider.GetRequiredService<IElasticHelperService>();
        await elsticService.AddUpdateBulk(products);
    }
    private async Task<IEnumerable<ProductElasticModel>> GetProductsCatalog(IServiceScope scope)
    {
        try
        {
            var products = await scope.ServiceProvider.GetRequiredService<IRepository<Product>>()
                                           .Query()
                                           .AsNoTracking()
                                           .Where(x => x.Status == EStatus.Published)
                                           .Include(x => x.ProductDetails)
                                           .ThenInclude(x => x.SlugInfo)
                                           .Include(x => x.Categories)
                                           .Include("Categories.Category")
                                           .Include("Categories.Category.CategoryDetails")
                                           .Include(x => x.ProductLinks)
                                           .Include("ProductLinks.LinkedProduct")
                                           .Include("ProductLinks.LinkedProduct.ProductDetails")
                                           .Include("ProductLinks.LinkedProduct.ProductDetails.SlugInfo")
                                           .Include("ProductLinks.LinkedProduct.ThumbnailImage")
                                           .Include(x => x.ThumbnailImage)
                                           .Include(x => x.AttributeValues)
                                           .Include("AttributeValues.Attribute")
                                           .Include("AttributeValues.AttributeValueDetails")
                                           .Include("AttributeValues.Attribute.AttributeDetails")
                                           .Include(x => x.Brand)
                                           .Include("Brand.BrandDetails")
                                           .ToListAsync();


            var result = products.SelectMany(x=>x.ProductDetails
                .Select(pem=> new ProductElasticModel
                {
                    Id = x.Id,
                    SiteId = x.SiteId,
                    ElasticId = $"{x.Id}-{nameof(EEntity.Product)}-{pem.CultureId}",
                    CultureId = pem.CultureId,
                    ProductDetails = new EDetails()
                    {
                        Name = pem.Name,
                        Description = pem.Description,
                        ShortDescription = pem.ShortDescription,
                        NormalizedName = pem.NormalizedName,
                        Specification = pem.Specification,
                        addtionalMetaTags = pem.AddtionalMetaTags,
                        Slug = pem.SlugInfo.Slug ?? "",
                        ThumbnailImageUrl = x.ThumbnailImage == null ? "" : x.ThumbnailImage.PublicUrl,
                    },

                    Attribute = x.AttributeValues.Select(at => new EProductAttribute
                    {
                        Id = at.AttributeId,
                        Name = at.Attribute.AttributeDetails.Where(ad => ad.CultureId == pem.CultureId).Select(n => n.Name).FirstOrDefault(),
                        Value = at.AttributeValueDetails.Where(ad => ad.CultureId == pem.CultureId).Select(n => n.Value).FirstOrDefault()
                    }).ToList(),

                    BrandDetails = x.Brand == null ? new EBrandDetails() : x.Brand.BrandDetails.Where(b => b.CultureId == pem.CultureId)
                      .Select(bd => new EBrandDetails
                      {
                          BrandId = bd.BrandId,
                          Name = bd.Name,
                          Description = bd.Description,
                      }).FirstOrDefault(),
                    CategoryDetails = x.Categories.Select(c =>
                    {
                        var categoryDetilas = c.Category.CategoryDetails.FirstOrDefault(cd => cd.CultureId == pem.CultureId);

                        return new ECategoryDetails
                        {
                            CategoryId = c.CategoryId,
                            Name = categoryDetilas?.Name,
                            Description = categoryDetilas?.Description,
                        };

                    }).ToList(),

                    ProductLink = x.ProductLinks.Select(pl =>
                    {
                        var producDetilas = pl.LinkedProduct.ProductDetails.Where(pld => pld.CultureId == pem.CultureId).FirstOrDefault();

                        return new EDetails
                        {
                            Name = producDetilas?.Name,
                            Description = producDetilas?.Description,
                            ShortDescription = producDetilas?.ShortDescription,
                            NormalizedName = producDetilas?.NormalizedName,
                            Specification = producDetilas?.Specification,
                            addtionalMetaTags = producDetilas?.AddtionalMetaTags,
                            Slug = producDetilas?.SlugInfo.Slug ?? "",
                            ThumbnailImageUrl = pl?.LinkedProduct.ThumbnailImage == null ? "" : pl?.LinkedProduct.ThumbnailImage.PublicUrl

                        };
                    }).ToList()

                }).ToList()

            ).ToList();

            return result;

        }
        catch (Exception ex)
        {

            throw;
        }

    }
}
