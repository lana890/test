using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Engine
{
    public class ContentPublishBackgroundService : BaseBackgroundService
    {

        private readonly System.IServiceProvider _serviceProvider;
        public readonly ILogger<ContentPublishBackgroundService> _logger;
        private readonly IConfiguration _configuration;

        public ContentPublishBackgroundService(IServiceProvider serviceProvider
            , ILogger<ContentPublishBackgroundService> logger
            , IConfiguration configuration)
            : base(nameof(ContentPublishBackgroundService), logger, serviceProvider, configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }


        public override async Task<bool> Execute()
        {

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dcmsLogService = scope.ServiceProvider.GetRequiredService<IdcmsLogService>();
                var productsRepository = scope.ServiceProvider.GetRequiredService<IRepository<Product>>();
                var categoriesRepository = scope.ServiceProvider.GetRequiredService<IRepository<Category>>();
                var pagesRepository = scope.ServiceProvider.GetRequiredService<IRepository<Page>>();
                var postsRepository = scope.ServiceProvider.GetRequiredService<IRepository<Posts>>();
                var brandsRepository = scope.ServiceProvider.GetRequiredService<IRepository<Brand>>();

                await dcmsLogService.AddInfo("Content Publish BackgroundService", "PagePreview BackgroundServicee Start", "", null);

                await PublishContent(pagesRepository, postsRepository, brandsRepository, categoriesRepository, productsRepository);

                await UnPublishContent(pagesRepository, postsRepository, brandsRepository, categoriesRepository, productsRepository);

            }
            catch (Exception e)
            {
                using var scope = _serviceProvider.CreateScope();
                var dcmsLogService = scope.ServiceProvider.GetRequiredService<IdcmsLogService>();
                await dcmsLogService.AddError("Content Publish BackgroundService", "", e.Message, "", null);

            }
            return true;

        }

        private async Task<bool> PublishContent(
            IRepository<Page> pagesRepository,
            IRepository<Posts> postsRepository,
            IRepository<Brand> brandsRepository,
            IRepository<Category> categoriesRepository,
            IRepository<Product> productsRepository)
        {
            var today = DateTimeOffset.Now.Date;


            var pagesShouldPublish = await pagesRepository.Query()
                                                            .Where(x => x.PublishDate.HasValue && x.PublishDate.Value.Date == today && x.Status == EStatus.Unpublished && (!x.ExpiryDate.HasValue || x.ExpiryDate > DateTimeOffset.Now))
                                                            .AsNoTracking()
                                                            .ToListAsync();

            var postsShouldPublish = await postsRepository.Query()
                                                            .Where(x => x.PublishDate.HasValue && x.PublishDate.Value.Date == today && x.Status == EStatus.Unpublished && (!x.ExpiryDate.HasValue || x.ExpiryDate > DateTimeOffset.Now))
                                                            .AsNoTracking()
                                                            .ToListAsync();


            var brandsShouldPublish = await brandsRepository.Query()
                                                            .Where(x => x.PublishDate.HasValue && x.PublishDate.Value.Date == today && x.Status == EStatus.Unpublished && (!x.ExpiryDate.HasValue || x.ExpiryDate > DateTimeOffset.Now))
                                                            .AsNoTracking()
                                                            .ToListAsync();

            var categoriesShouldPublish = await categoriesRepository.Query()
                                                                    .Where(x => x.PublishDate.HasValue && x.PublishDate.Value.Date == today && x.Status == EStatus.Unpublished && (!x.ExpiryDate.HasValue || x.ExpiryDate > DateTimeOffset.Now))
                                                                    .AsNoTracking()
                                                                    .ToListAsync();


            var productsShouldPublish = await productsRepository.Query()
                                                                .Where(x => x.PublishDate.HasValue && x.PublishDate.Value.Date == today && x.Status == EStatus.Unpublished && (!x.ExpiryDate.HasValue || x.ExpiryDate > DateTimeOffset.Now))
                                                                .AsNoTracking()
                                                                .ToListAsync();


            if (pagesShouldPublish.Any())
            {
                pagesShouldPublish.ForEach(page => page.Status = EStatus.Published);
                pagesRepository.UpdateRange(pagesShouldPublish); 
                await pagesRepository.SaveChangesAsync();
               
             

            }
            if (postsShouldPublish.Any())
            {
                postsShouldPublish.ForEach(post => post.Status = EStatus.Published);
                postsRepository.UpdateRange(postsShouldPublish);
                await postsRepository.SaveChangesAsync();
            }
            if (brandsShouldPublish.Any())
            {
                brandsShouldPublish.ForEach(brand => brand.Status = EStatus.Published);
                brandsRepository.UpdateRange(brandsShouldPublish);
                await brandsRepository.SaveChangesAsync();
            }
            if (categoriesShouldPublish.Any())
            {
                categoriesShouldPublish.ForEach(category => category.Status = EStatus.Published);
                categoriesRepository.UpdateRange(categoriesShouldPublish);
                await categoriesRepository.SaveChangesAsync();
            }
            if (productsShouldPublish.Any())
            {
                productsShouldPublish.ForEach(product => product.Status = EStatus.Published);
                productsRepository.UpdateRange(productsShouldPublish);
                await productsRepository.SaveChangesAsync();
            }


            return true;
        }

        private async Task<bool> UnPublishContent(
            IRepository<Page> pagesRepository,
            IRepository<Posts> postsRepository,
            IRepository<Brand> brandsRepository,
            IRepository<Category> categoriesRepository,
            IRepository<Product> productsRepository)
        {

            var pagesShouldUnPublish = await pagesRepository.Query()
                                                             .Where(x => x.ExpiryDate.HasValue && x.ExpiryDate < DateTimeOffset.Now && x.Status == EStatus.Published)
                                                             .AsNoTracking()
                                                             .ToListAsync();

            var postsShouldUnPublish = await postsRepository.Query()
                                                             .Where(x => x.ExpiryDate.HasValue && x.ExpiryDate < DateTimeOffset.Now && x.Status == EStatus.Published)
                                                             .AsNoTracking()
                                                            .ToListAsync();

            var brandsShouldUnPublish = await brandsRepository.Query()
                                                               .Where(x => x.ExpiryDate.HasValue && x.ExpiryDate < DateTimeOffset.Now && x.Status == EStatus.Published)
                                                               .AsNoTracking()
                                                               .ToListAsync();

            var categoriesShouldUnPublish = await categoriesRepository.Query()
                                                                       .Where(x => x.ExpiryDate.HasValue && x.ExpiryDate < DateTimeOffset.Now && x.Status == EStatus.Published)
                                                                       .AsNoTracking()
                                                                       .ToListAsync();

            var productsShouldUnPublish = await productsRepository.Query()
                                                                   .Where(x => x.ExpiryDate.HasValue && x.ExpiryDate < DateTimeOffset.Now && x.Status == EStatus.Published)
                                                                   .AsNoTracking()
                                                                   .ToListAsync();


            if (pagesShouldUnPublish.Any())
            {
                pagesShouldUnPublish.ForEach(page => page.Status = EStatus.Published);
                pagesRepository.UpdateRange(pagesShouldUnPublish);
                await pagesRepository.SaveChangesAsync();

            }
            if (postsShouldUnPublish.Any())
            {
                postsShouldUnPublish.ForEach(post => post.Status = EStatus.Published);
                postsRepository.UpdateRange(postsShouldUnPublish);
                await postsRepository.SaveChangesAsync();
            }
            if (brandsShouldUnPublish.Any())
            {
                brandsShouldUnPublish.ForEach(brand => brand.Status = EStatus.Published);
                brandsRepository.UpdateRange(brandsShouldUnPublish);
                await brandsRepository.SaveChangesAsync();
            }
            if (categoriesShouldUnPublish.Any())
            {
                categoriesShouldUnPublish.ForEach(category => category.Status = EStatus.Published);
                categoriesRepository.UpdateRange(categoriesShouldUnPublish);
                await categoriesRepository.SaveChangesAsync();
            }

            if (productsShouldUnPublish.Any())
            {
                productsShouldUnPublish.ForEach(product => product.Status = EStatus.Published);
                productsRepository.UpdateRange(productsShouldUnPublish);
                await productsRepository.SaveChangesAsync();
            }

            return true;
        }

    }
}
