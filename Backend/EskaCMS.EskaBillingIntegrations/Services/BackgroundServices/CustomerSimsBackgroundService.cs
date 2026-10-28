//using EskaCMS.Core.BaseBackgroundServices;
//using EskaCMS.EskaBilling.Configurations;
//using EskaCMS.EskaBilling.Configurations.Interface;
//using EskaCMS.Infrastructure.Data;
//using EskaCMS.Integrations.EskaBilling.Entities;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using static EskaCMS.EskaBilling.Enums.IOTEnums;

//namespace EskaCMS.EskaBilling.Services.BackgroundServices
//{
//    public class CustomerSimsBackgroundService : BaseBackgroundService
//    {

//        public readonly ILogger<CustomerSimsBackgroundService> _logger;
//        private readonly System.IServiceProvider _serviceProvider;
//        private IServiceScope scope;
//        private IConfiguration _Configuration;
//        private readonly IRepository<CustomerSims> _customerSimsRepository;
//        private readonly IRepository<CustomerSimsHistory> _customerSimsHistoryRepository;
//        private readonly ISIMsActionsService _customerSimsAction;
//        private readonly IConfigProvider _configProvider;

//        public CustomerSimsBackgroundService(ILogger<CustomerSimsBackgroundService> logger,
//            System.IServiceProvider serviceProvider,
//            IConfiguration Configuration): base(nameof(CustomerSimsBackgroundService), logger, serviceProvider, Configuration)
//        {
//            scope = serviceProvider.CreateScope();

//            _logger = logger;
//            _serviceProvider = serviceProvider;
//            _Configuration = Configuration;
//            _customerSimsRepository = scope.ServiceProvider.GetRequiredService<IRepository<CustomerSims>>();
//            _customerSimsHistoryRepository = scope.ServiceProvider.GetRequiredService<IRepository<CustomerSimsHistory>>();
//            _customerSimsAction = scope.ServiceProvider.GetRequiredService<ISIMsActionsService>();
//            _configProvider = scope.ServiceProvider.GetRequiredService<IConfigProvider>();
            
//        }

//        public override async Task<bool> Execute()
//        {
//            try
//            {

//               await ActiveWaitingSims();


//                var SusbendedSim = await _customerSimsRepository.Query()
//                                                                .Where(x => x.Status == Enums.IOTEnums.EIOTSIMStatus.Suspended && !x.IsDeleted)
//                                                                .Select(x=>x.Id)
//                                                                .ToListAsync();


//                var CandidateSuspendedSimIds =  await  _customerSimsAction.GetSIMsThatCanNotBeSuspendedAnyMore(SusbendedSim);

//                if (CandidateSuspendedSimIds.Count>0)
//                {
//                    int ActivateSuspensionAfterDays = Convert.ToInt32(_configProvider.GetValue("BillingBGSs:ActivationBGS:ActivateSuspensionAfterDays"));
//                    await _customerSimsAction.ChangeSimsStatus(CandidateSuspendedSimIds, Enums.IOTEnums.ESIMsAction.RESUME, $"Resume Customer sims which it has  Susbended status {ActivateSuspensionAfterDays} day ago");
                       
//                }



//                return true;

//            }
//            catch (Exception)
//            {
//                _logger.LogInformation("Failed CustomerSimBackgroundService");
//                return false;

//            }


//        }


//        public async Task<bool> ActiveWaitingSims() {

//            var days = Convert.ToInt64(_configProvider.GetValue("BillingBGSs:ActivationBGS:ActivateReadyForActivateAfterDays"));
//            var ConditionTime = DateTimeOffset.Now.AddDays(-days);
//            var WaitingForActivationSimIds = await _customerSimsRepository.Query()
//                                                                        .Where(x => x.Status == Enums.IOTEnums.EIOTSIMStatus.WaitingForActivation
//                                                                         && x.CreationDate < ConditionTime
//                                                                         && !x.IsDeleted)
//                                                                        .Select(x => x.Id).ToListAsync();







//            if (WaitingForActivationSimIds.Any())
//            {
//                await _customerSimsAction.ChangeSimsStatus(WaitingForActivationSimIds, Enums.IOTEnums.ESIMsAction.ACTIVATE, "Active Customer sims which it has Waiting For Active status a day ago");
//            }

//            return true;
//        }


//        ~CustomerSimsBackgroundService()
//        {
//            scope.Dispose();
//        }

//    }
//}
