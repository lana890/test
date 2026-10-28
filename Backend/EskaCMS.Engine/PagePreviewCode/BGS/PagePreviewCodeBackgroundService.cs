using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Engine.PagePreviewCode.BGS
{
    public class PagePreviewCodeBackgroundService : BaseBackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        public readonly ILogger<PagePreviewCodeBackgroundService> _logger;
        private readonly IConfiguration _configuration;

        public PagePreviewCodeBackgroundService(IServiceProvider serviceProvider
            , ILogger<PagePreviewCodeBackgroundService> logger
            , IConfiguration configuration)
            : base(nameof(PagePreviewCodeBackgroundService), logger, serviceProvider, configuration)
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
                var pagePreviewCodesRepository = scope.ServiceProvider.GetRequiredService<IRepository<PagePreviewCodes>>();

                var time = DateTimeOffset.Now.AddMinutes(-30);
                var RemovedPagePreview = pagePreviewCodesRepository.Query().Where(x => x.CreationDate <= time).ToList();
                if (RemovedPagePreview.Any())
                {
                    pagePreviewCodesRepository.RemoveRange(RemovedPagePreview);
                    await pagePreviewCodesRepository.SaveChangesAsync();
                }

            }
            catch (Exception e)
            {
                using var scope = _serviceProvider.CreateScope();
                var dcmsLogService = scope.ServiceProvider.GetRequiredService<IdcmsLogService>();
                await dcmsLogService.AddError("PagePreview BackgroundService", "", e.Message, "", null);

            }
            return true;

        }
    }
}
