using EskaCMS.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Administration;
using System;
using EskaCMS.Entities.Entity;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Core.Services;

namespace EskaCMS.Engine
{
    public class CypherParamsBackgroundService : BaseBackgroundService
    {
        private long EXPIRATION_PRIOD_IN_DAYS;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _Configration;
        private readonly ILogger _logger;


        public CypherParamsBackgroundService(IServiceProvider serviceProvider
            , ILogger<ContentPublishBackgroundService> logger
            , IConfiguration configuration)
            : base(nameof(CypherParamsBackgroundService), logger, serviceProvider, configuration)
        {
            _serviceProvider = serviceProvider;

            _logger = logger;
            _Configration = configuration;
            EXPIRATION_PRIOD_IN_DAYS = Convert.ToInt32(_Configration.GetSection("CypherSecurity:EXPIRATION_PERIOD_IN_DAYS").Value);

        }

        public override async Task<bool> Execute()
        {
            try
            {


                await Task.Delay(TimeSpan.FromDays(EXPIRATION_PRIOD_IN_DAYS));

                using var scope = _serviceProvider.CreateScope();
                var ParamRepository = scope.ServiceProvider.GetRequiredService<IRepositoryWithTypedId<CypherParam, string>>();
                var ExpiredParams = await ParamRepository.Query().Where(p => DateTimeOffset.Now.CompareTo(p.CreationDate.AddDays(EXPIRATION_PRIOD_IN_DAYS)) > 0).ToListAsync();

                if (ExpiredParams.Count > 0)
                {
                    foreach (var param in ExpiredParams)
                    {
                        ParamRepository.Remove(param);

                    }
                    ParamRepository.SaveChanges();
                }



            }
            catch (Exception e)
            {
                using var scope = _serviceProvider.CreateScope();
                var dcmsLogService = scope.ServiceProvider.GetRequiredService<IdcmsLogService>();
                await dcmsLogService.AddError("Cypher Params BackgroundService", "", e.Message, "", null);

            }
            return true;
        }
    }


}
