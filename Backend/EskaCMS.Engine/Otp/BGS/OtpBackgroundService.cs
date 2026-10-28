using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;

namespace EskaCMS.Engine.Otp.BGS
{
    public class OtpBackgroundService : BaseBackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        public readonly ILogger<OtpBackgroundService> _logger;
        private readonly IConfiguration _configuration;

        public OtpBackgroundService(IServiceProvider serviceProvider
            , ILogger<OtpBackgroundService> logger
            , IConfiguration configuration)
            : base(nameof(OtpBackgroundService), logger, serviceProvider, configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public override async Task<bool> Execute()
        {
            using var scope = _serviceProvider.CreateScope();
            var userOTPRepository = scope.ServiceProvider.GetRequiredService<IRepository<UserOTP>>();
            var time = DateTimeOffset.Now.AddMinutes(-60);
            var OtpData = userOTPRepository.Query().Where(x => x.CreationDate < time).ToList();
            if (OtpData.Any())
            {
                userOTPRepository.RemoveRange(OtpData);
                await userOTPRepository.SaveChangesAsync();
            }
            return true;

        }

    }
}
