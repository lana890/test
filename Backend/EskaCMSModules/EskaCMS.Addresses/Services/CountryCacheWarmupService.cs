using EskaCMS.Addresses.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EskaCMS.Addresses.Services
{
    public class CountryCacheWarmupService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<CountryCacheWarmupService> _logger;

        public CountryCacheWarmupService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<CountryCacheWarmupService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var countryApiService = scope.ServiceProvider.GetRequiredService<ICountryApiService>();
                await countryApiService.GetAllCountries();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warm country cache during startup.");
            }
        }
    }
}
