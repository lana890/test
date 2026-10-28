using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Integrations.EskaBilling.Models;
using EskaCMS.Core.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.EskaBilling.Enums;
using static EskaCMS.EskaBilling.Enums.IOTEnums;
using Microsoft.AspNetCore.Http;
using EskaCMS.Infrastructure.Helpers;
using AutoMapper;
using EskaCMS.EskaBilling.Models.response;
using AutoMapper.QueryableExtensions;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Core.Entities;
using EskaCMS.Infrastructure.Helpers.HttpHelper;
using EskaCMS.EskaBilling.Models;
using Newtonsoft.Json;
using EskaCMS.EskaBilling.Configurations;
//using EskaCMS.Entitys.Entity.Catalog;
using System.Data;
using Microsoft.Extensions.Configuration;
using EskaCMS.EskaBilling.Configurations.Interface;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.DataAccess.Data;
using Npgsql;
using EskaCMS.Infrastructure.DbFactory;
using EskaCMS.Infrastructure.Enums;
using EskaCMS.Entities.Entity;

namespace EskaCMS.EskaBilling.Services
{
    public class SIMsActionsService : ISIMsActionsService
    {

        private readonly IWorkContext _workContext;
        private readonly IRepository<ProductAttributeValue> _ProductAttributeValueRepository;
        private readonly IRepository<CustomerSims> _customerSimsRepository;
        private readonly IRepository<CustomerSimsHistory> _customerSimsHistoryRepository;
        private readonly IRepositoryWithTypedId<User, long> _userRepository;
        private readonly IRepository<ChangePlanRequest> _changePlanRequestRepository;
        private readonly EskaDCMSDbContext _context;
        private readonly IMapper mapper;
        private readonly IConfiguration _config;
        private readonly IHttpHelper httpHelper;
        private const string ServiceProfile = "ServiceProfile";
        private readonly IConfigProvider _configProvider;
        private EProvider Provider;
        public SIMsActionsService(IWorkContext workContext,
                         IConfiguration config
                        , IConfigProvider configProvider
                        , IRepository<CustomerSims> customerSimsVMRepository
                        , IRepositoryWithTypedId<User, long> userRepository
                        , EskaDCMSDbContext context
                        , IMapper mapper
                        , IRepository<ChangePlanRequest> changePlanRequestRepository
                        , IRepository<CustomerSimsHistory> customerSimsHistoryVMRepository
                        , IHttpHelper httpHelper, IRepository<ProductAttributeValue> _ProductAttributeValueRepository)
        {
            _workContext = workContext;
            _customerSimsRepository = customerSimsVMRepository;
            _context = context;
            _userRepository = userRepository;
            this.mapper = mapper;
            _customerSimsHistoryRepository = customerSimsHistoryVMRepository;
            this.httpHelper = httpHelper;
            this._ProductAttributeValueRepository = _ProductAttributeValueRepository;
            _config = config;
            _changePlanRequestRepository = changePlanRequestRepository;
            _configProvider=configProvider;
            Provider = DbProviderHelper.ParseProvider(config.GetValue<string>("ConnectionStrings:DatabaseProvider"));
        }



        public async Task<List<long>> GetSIMsThatCanNotBeSuspendedAnyMore(List<long> Ids)
        {
            try
            {

                var MaxDays = Convert.ToInt64(_configProvider.GetValue("BillingBGSs:ActivationBGS:ActivateSuspensionAfterDays"));
                var SimsHistory = await _customerSimsHistoryRepository.Query()
                                                               .Where(x => Ids.Any(y => y == x.CustomerSimsId) && x.CreationDate.Year == DateTimeOffset.Now.Year)
                                                               .GroupBy(x => x.CustomerSimsId)
                                                               .Select(x => new { CustomerSimId = x.Key, HistoryPerSim = x.OrderBy(x => x.CreationDate).ToList() })
                                                               .ToListAsync();


                var SIMsTotalSuspensionTime = new Dictionary<long, TimeSpan>();

                DateTimeOffset StartOfYear = new DateTimeOffset(DateTimeOffset.Now.Year, 1, 1, 0, 0, 0, new TimeSpan());



                foreach (var Item in SimsHistory)
                {
                    if (!Item.HistoryPerSim.Any())
                    {

                        var period = DateTimeOffset.Now - StartOfYear;

                        UpdateTotalSuspensionTime(SIMsTotalSuspensionTime, Item.CustomerSimId, period);
                        continue;
                    }


                    for (int i = 0; i < Item.HistoryPerSim.Count; i++)
                    {
                        var period = new TimeSpan();
                        if (i == 0 && Item.HistoryPerSim[i].OldStatus == EIOTSIMStatus.Suspended)
                        {


                            period = Item.HistoryPerSim[i].CreationDate - StartOfYear;

                            UpdateTotalSuspensionTime(SIMsTotalSuspensionTime, Item.CustomerSimId, period);

                        }
                        else if (Item.HistoryPerSim[i].OldStatus == EIOTSIMStatus.Suspended && Item.HistoryPerSim[i - 1].NewStatus == EIOTSIMStatus.Suspended)
                        {
                            period = Item.HistoryPerSim[i].CreationDate - Item.HistoryPerSim[i - 1].CreationDate;

                            UpdateTotalSuspensionTime(SIMsTotalSuspensionTime, Item.CustomerSimId, period);

                        }
                        else if (i == (Item.HistoryPerSim.Count - 1) && Item.HistoryPerSim[i].NewStatus == EIOTSIMStatus.Suspended)
                        {
                            period = DateTimeOffset.Now - Item.HistoryPerSim[i].CreationDate;
                            UpdateTotalSuspensionTime(SIMsTotalSuspensionTime, Item.CustomerSimId, period);
                        }

                    }
                }



                return SIMsTotalSuspensionTime.Where(x => x.Value.Days > MaxDays).Select(x => x.Key).ToList();



            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<bool> ChangeSimsStatus(List<long> Ids, ESIMsAction Action, string Note)
        {
            try
            {


                var TargetSIMs = await _customerSimsRepository.Query()
                    .Where(x => Ids.Any(id => id == x.Id))
                    .Include(s => s.Product)
                    .ThenInclude(s => s.AttributeValues)
                    .ThenInclude(s => s.Attribute)
                    .ToListAsync();


                var TargetSIMsSets = TargetSIMs
                    .GroupBy(s => s.ProductId)
                    .Select(x => new ChangeSIMsInfoVM()
                    {
                        //ServiceProfile = x.ToList().Select(s => s.Product.AttributeValues.FirstOrDefault(a => a.Attribute.AttributeDetails.Any(x=>x.Name == ServiceProfile)).Value).FirstOrDefault(),
                        SIMs = x.ToList()
                    })
                    .ToList();

                await ValidateSIMAction(TargetSIMs, Action);

                await AddSIMsUsage(TargetSIMs.Select(x => new SIMWithIdentifiers()
                {
                    Id = x.Id,
                    ICCID = x.ICCID,
                    IdentifierType = x.IdentifierType,
                    IMSI = x.IMSI
                }).ToList());
                await ChangeBillingStatus(SIMsActions.GetProperActionCode(Action), TargetSIMsSets);


                await UpdateStatus(TargetSIMs, SIMsActions.GetProperSIMStatus(Action), Note);
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }

        } 

        public async Task<bool> RequestChangeSIMsPlan(ChangeSIMsPlanVM Model , long customerId)
        {
            try
            {

                var simIds = Model.Ids.Select(x => x).ToList();

                var allSims = _customerSimsRepository.Query()
                                       .Where(x => simIds.Contains(x.Id))
                                       .ToList();


               
                    var simHasWaitingStatus = allSims.Where(x => x.Status == EIOTSIMStatus.WaitingForActivation).ToList();

                    if (simHasWaitingStatus.Any())
                    {

                        await UpdateProduct(simHasWaitingStatus, Model.ProductId, Model.Note);

                    }
               

               


                var simWillRequestPlan = allSims.Where(x => x.Status != EIOTSIMStatus.WaitingForActivation);

                var newPlans = simWillRequestPlan.Select(x=> new ChangePlanRequest 
                                            { CustomerSimsId = x.Id , 
                                            CreatedById = customerId ,
                                             CreationDate=DateTimeOffset.Now,
                                             Note=Model.Note,
                                             ProductId=Model.ProductId,
                                             Status=ChangePlanStatus.Waiting,
                                            })
                                            .ToList();

                bool hasDuplicatePlan = false;

                foreach (var newPlan in newPlans)
                {
                    bool hasDuplicateForNewPlan = _changePlanRequestRepository.Query()
                        .Any(existingPlan => existingPlan.CreationDate.Month == newPlan.CreationDate.Month && 
                                            existingPlan.CreationDate.Year == newPlan.CreationDate.Year && 
                                            existingPlan.Status != ChangePlanStatus.Deleted &&
                                            existingPlan.CustomerSimsId== newPlan.CustomerSimsId);
                           
                    if (hasDuplicateForNewPlan)
                    {
                        hasDuplicatePlan = true;
                        break; 
                    }
                }


                if (hasDuplicatePlan)
                {
                    throw new Exception("Please remove the previously scheduled plan before adding a new one.");
                }
                if (newPlans.Any())
                {
                    _changePlanRequestRepository.AddRange(newPlans);
                   await  _changePlanRequestRepository.SaveChangesAsync();
                    return true;

                }
                return false;
            }
            catch (Exception e)
            {
                throw;
               
            }
        }

        public async Task<bool> ChangeSIMsPlan(ChangeSIMsPlanVM Model)
        {
            try
            {


                var TargetSIMs = await _customerSimsRepository.Query()
                    .Where(x => Model.Ids.Any(id => id == x.Id))
                    .ToListAsync();

                
                //var TargetServiceProfile = _ProductAttributeValueRepository
                //     .Query()
                //     .Include(prop => prop.Attribute)
                //     .Where(p => p.ProductId == Model.ProductId && p.Attribute.Name == ServiceProfile)
                //     .Select(x => x.Value)
                //     .FirstOrDefault();

                var SIMsSet = new ChangeSIMsInfoVM()
                {
                    //ServiceProfile = TargetServiceProfile,
                    ServiceProfile = "",
                    SIMs = TargetSIMs
                };

                await ChangeBillingPlan(SIMsActions.GetProperActionCode(ESIMsAction.CHANGE_PLAN), SIMsSet);

                await UpdateProduct(TargetSIMs, Model.ProductId, Model.Note);
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }


        public async Task AddSIMsUsage(List<SIMWithIdentifiers> SIMs)
        {
            try
            {
                  string connectionString = _config.GetConnectionString("DefaultConnection"); 
                foreach (var SIM in SIMs)
                {
                    var Usage = await GetBillingUsage(SIM);
                   

                    
                    using (var connection = dbFactory.CreateConnection(connectionString,Provider))
                    {

                        
                            connection.Open();

                            using (var command = dbFactory.CreateCommand("INSERT_SIM_USAGE", connection,Provider))
                            {

                            try
                            {
                                command.CommandType = CommandType.StoredProcedure;

                                command.Parameters.Add((dbFactory.CreateParameter(Provider,"@P_dataTotal", Usage.dataTotal)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_roamingDataConsumed", Usage.roamingDataConsumed)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_dataOut", Usage.dataOut)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_cost", Usage.cost)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_smsOutgoing", Usage.smsOutgoing)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_dataCharged", Usage.dataCharged)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_smsUsage", Usage.smsUsage)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_numberOfVoiceCalls", Usage.numberOfVoiceCalls)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_roamingDataOut", Usage.roamingDataOut)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_numberOfRoamingSms", Usage.numberOfRoamingSms)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_roamingVoiceOutgoing", Usage.roamingVoiceOutgoing)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_roamingSmsOutgoing", Usage.roamingSmsOutgoing)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_voiceIncoming", Usage.voiceIncoming)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_voiceOutgoing", Usage.voiceOutgoing)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_numberOfRoamingCalls", Usage.numberOfRoamingCalls)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_roamingVoiceIncoming", Usage.roamingVoiceIncoming)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_dataIn", Usage.dataIn)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_smsIncoming", Usage.smsIncoming)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_roamingSmsIncoming", Usage.roamingSmsIncoming)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_roamingDataIn", Usage.roamingDataIn)));
                                command.Parameters.Add((dbFactory.CreateParameter(Provider, "@P_SIM_ID", SIM.Id)));



                                command.ExecuteNonQuery();
                            }
                            catch (Exception e )
                            {
                                connection.Close();
                                command.Dispose();
                                throw e;
                            }
                             
                                
                            }

                            connection.Close();
                       
                    }
                }

            }
            catch (Exception ex)
            {
                
                throw ex;
            }

        }

       

        public async Task<bool> UpdateSTCIPAddress(SIMWithIdentifiers SIM, string IPAdress)
        {
            try
            {
                var Payload = await GetUpdateIPAddressPayloadPayload(SIM, IPAdress);
                await httpHelper.Post(Payload);

                return true;
            }
            catch (Exception e)
            {

                throw e;
            }

        }

        #region Helpers



        #region Billing Status
        private async Task UpdateStatus(List<CustomerSims> SIMs, EIOTSIMStatus NewStatus, string Note)
        {

            long CurrentUserId = await _workContext.GetCurrentUserId();
            bool IsBGS = CurrentUserId == 0;

            

            foreach (var SIM in SIMs)
            {
                EIOTSIMStatus OldStatus = SIM.Status;
                try
                {
                    SIM.Status = NewStatus;
                    SIM.ModificationDate = DateTimeOffset.Now;
                    SIM.ModifiedById = IsBGS ? 1 : CurrentUserId;

                    if (NewStatus == EIOTSIMStatus.Active && await IsFirstActivation(SIM))
                    {
                        SIM.FirstActivationDate = DateTimeOffset.Now;
                    }
                    await _customerSimsRepository.SaveChangesAsync();
                    try
                    {
                        await AddSIMsHistory(SIM, OldStatus, Note, IsBGS ? 1 : CurrentUserId);

                    }
                    catch
                    {

                    }
                }
                catch (Exception e)
                {

                    await AddSIMsHistory(SIM, OldStatus, "Updating Faild to Change Status to from " + OldStatus + " to be " + NewStatus + " becouse of " + e.Message, IsBGS ? 1 : CurrentUserId);
                }


            }
        }

        private async Task<BillingAPIPayload<ChangeSimStatusParameter>> GenerateChangeStatusPayload(List<ChangeSIMsInfoVM> SIMsSets, string Action)
        {
            var Parameters = new List<ChangeSimStatusParameter>();

            foreach (var SIMSet in SIMsSets)
            {
                var IMSI_SIMs = SIMSet.SIMs.Where(s => GetSIMIdentifierType(s) == SIMsIdentifiers.IMSI).Select(s => GetSIMIdentifier(s)).ToList();
                if (IMSI_SIMs.Count > 0)
                {
                    Parameters.Add(new ChangeSimStatusParameter(SIMsIdentifiers.IMSI, IMSI_SIMs, SIMSet.ServiceProfile));

                }

                var ICCID_SIMs = SIMSet.SIMs.Where(s => GetSIMIdentifierType(s) == SIMsIdentifiers.ICCID).Select(s => GetSIMIdentifier(s)).ToList();

                if (ICCID_SIMs.Count > 0)
                {
                    Parameters.Add(new ChangeSimStatusParameter(SIMsIdentifiers.ICCID, ICCID_SIMs, SIMSet.ServiceProfile));

                }
            }


            BillingAPIPayload<ChangeSimStatusParameter> payload = new BillingAPIPayload<ChangeSimStatusParameter>(Action, Parameters);
            return payload;
        }


        private async Task<PostRequestVM> GenerateBillingChangeStatusPayload(string Action, List<ChangeSIMsInfoVM> SIMsSets)
        {
            var Payload = await GenerateChangeStatusPayload(SIMsSets, Action);
            return new PostRequestVM()
            {
                BaseUrl = _configProvider.GetValue("BillingAPIs:BaseUrl"),
                SerializedContent = JsonConvert.SerializeObject(Payload),

            };

        }


        private async Task<bool> ChangeBillingStatus(string Action, List<ChangeSIMsInfoVM> SIMsSets)
        {
            try
            {
                var Payload = await GenerateBillingChangeStatusPayload(Action, SIMsSets);
                var res = await httpHelper.Post(Payload);

                return true;
            }
            catch (Exception e)
            {

                throw e;
            }

        }

        #endregion

        #region Billing Plan

        private async Task UpdateProduct(List<CustomerSims> SIMs, long NewProductId, string Note)
        {

            long CurrentUserId = await _workContext.GetCurrentUserId();
            bool IsBGS = CurrentUserId == 0;

            foreach (var SIM in SIMs)
            {
                long OldProdcutId = SIM.ProductId;
                try
                {
                    SIM.ProductId = NewProductId;
                    SIM.ModificationDate = DateTimeOffset.Now;
                    SIM.ModifiedById = IsBGS ? 1 : CurrentUserId;

                    await _customerSimsRepository.SaveChangesAsync();
                    try
                    {
                        await AddSIMsHistory(SIM, SIM.Status, Note, IsBGS ? 1 : CurrentUserId);

                    }
                    catch
                    {

                    }
                }
                catch (Exception e)
                {

                    await AddSIMsHistory(SIM, SIM.Status, "Updating Faild to Change ProductId to from " + OldProdcutId + " to be " + NewProductId + " becouse of " + e.Message, IsBGS ? 1 : CurrentUserId);
                }


            }
        }
        private async Task<BillingAPIPayload<ChangeSimPlanParameter>> GenerateChangePlanPayload(ChangeSIMsInfoVM SIMSet, string Action)
        {
            var Parameters = new List<ChangeSimPlanParameter>();

            var IMSI_SIMs = SIMSet.SIMs.Where(s => GetSIMIdentifierType(s) == SIMsIdentifiers.IMSI).Select(s => GetSIMIdentifier(s)).ToList();
            if (IMSI_SIMs.Count > 0)
            {
                Parameters.Add(new ChangeSimPlanParameter(SIMsIdentifiers.IMSI, IMSI_SIMs, Convert.ToInt64(SIMSet.ServiceProfile)));

            }

            var ICCID_SIMs = SIMSet.SIMs.Where(s => GetSIMIdentifierType(s) == SIMsIdentifiers.ICCID).Select(s => GetSIMIdentifier(s)).ToList();

            if (ICCID_SIMs.Count > 0)
            {
                Parameters.Add(new ChangeSimPlanParameter(SIMsIdentifiers.ICCID, ICCID_SIMs, Convert.ToInt64(SIMSet.ServiceProfile)));

            }



            BillingAPIPayload<ChangeSimPlanParameter> payload = new BillingAPIPayload<ChangeSimPlanParameter>(Action, Parameters);
            return payload;
        }

        private async Task<PostRequestVM> GenerateBillingChangePlanPayload(string Action, ChangeSIMsInfoVM SIMsSets)
        {
            var Payload = await GenerateChangePlanPayload(SIMsSets, Action);
            return new PostRequestVM()
            {
                BaseUrl = _configProvider.GetValue("BillingAPIs:BaseUrl"),
                SerializedContent = JsonConvert.SerializeObject(Payload),

            };

        }


        private async Task<bool> ChangeBillingPlan(string Action, ChangeSIMsInfoVM SIMsSet)
        {
            try
            {
                var Payload = await GenerateBillingChangePlanPayload(Action, SIMsSet);
                var res = await httpHelper.Post(Payload);

                return true;
            }
            catch (Exception e)
            {

                throw e;
            }

        }


        #endregion

        #region Get Billing Usage
        private async Task<BillingAPIPayload<GetBillingUsageParameter>> GenerateGetBillingUsagePayload(SIMWithIdentifiers SIM, string Action)
        {
            var Parameters = new List<GetBillingUsageParameter>();

            Parameters.Add(new GetBillingUsageParameter(GetSIMIdentifierType(SIM), GetSIMIdentifier(SIM)));

            BillingAPIPayload<GetBillingUsageParameter> payload = new BillingAPIPayload<GetBillingUsageParameter>(Action, Parameters);
            return payload;
        }

        private async Task<PostRequestVM> GenerateBillingGetUsagePayload(string Action, SIMWithIdentifiers SIM)
        {
            var Payload = await GenerateGetBillingUsagePayload(SIM, Action);
            return new PostRequestVM()
            {
                BaseUrl = _configProvider.GetValue("BillingAPIs:BaseUrl"),
                SerializedContent = JsonConvert.SerializeObject(Payload),

            };

        }
        private async Task<SIMsUsageResponseVM> GetBillingUsage(SIMWithIdentifiers SIM)
        {
            try
            {
                var Payload = await GenerateBillingGetUsagePayload(SIMsActions.GET_SIM_USAGE, SIM);
                string SerilazedRes = await httpHelper.Post(Payload);

                return JsonConvert.DeserializeObject<APIResultVM<SIMsUsageResponseVM>>(SerilazedRes).Data;
            }
            catch (Exception e)
            {

                throw e;
            }

        }


        #endregion


        #region Update SIM IPAddress
        private async Task<BillingAPIPayload<UpdateSTC_IP>> GenerateUpdateIPAddressPayload(SIMWithIdentifiers SIM, string IPAdress)
        {
            var Parameters = new List<UpdateSTC_IP>();

            Parameters.Add(new UpdateSTC_IP(GetSIMIdentifierType(SIM), GetSIMIdentifier(SIM),IPAdress));

            BillingAPIPayload<UpdateSTC_IP> payload = new BillingAPIPayload<UpdateSTC_IP>(SIMsActions.UPDATE_SIM_DATA, Parameters);
            return payload;
        }

        private async Task<PostRequestVM> GetUpdateIPAddressPayloadPayload(SIMWithIdentifiers SIM, string IPAdress)
        {
            var Payload = await GenerateUpdateIPAddressPayload(SIM, IPAdress);
            return new PostRequestVM()
            {
                BaseUrl = _configProvider.GetValue("BillingAPIs:BaseUrl"),
                SerializedContent = JsonConvert.SerializeObject(Payload),

            };

        }
       


        #endregion
        private void UpdateTotalSuspensionTime(Dictionary<long, TimeSpan> dict, long key, TimeSpan period)
        {
            if (dict.ContainsKey(key))
            {
                // Update the value if the key already exists
                dict[key] += period;
            }
            else
            {
                // Add a new key-value pair if the key doesn't exist
                dict.Add(key, period);
            }
        }

        private string GetSIMIdentifierType(CustomerSims SIM)
        {
            if (SIM.IdentifierType == ESIMsIdentifiers.IMSI || SIM.IdentifierType == ESIMsIdentifiers.ANY)
            {
                return SIMsIdentifiers.IMSI.ToString();
            }
            else if (SIM.IdentifierType == ESIMsIdentifiers.ICCID)
            {
                return SIMsIdentifiers.ICCID.ToString();

            }

            throw new Exception("SIM Does Not Have Identitifire");
        }
        private string GetSIMIdentifierType(SIMWithIdentifiers SIM)
        {
            if (SIM.IdentifierType == ESIMsIdentifiers.IMSI || SIM.IdentifierType == ESIMsIdentifiers.ANY)
            {
                return SIMsIdentifiers.IMSI.ToString();
            }
            else if (SIM.IdentifierType == ESIMsIdentifiers.ICCID)
            {
                return SIMsIdentifiers.ICCID.ToString();

            }

            throw new Exception("SIM Does Not Have Identitifire");
        }


        private string GetSIMIdentifier(CustomerSims SIM)
        {
            string SIMIdentifierType = GetSIMIdentifierType(SIM);
            if (SIMIdentifierType == SIMsIdentifiers.IMSI)
            {
                return SIM.IMSI.Value.ToString();
            }
            else if (SIMIdentifierType == SIMsIdentifiers.ICCID)
            {
                return SIM.ICCID.Value.ToString();

            }
            throw new Exception("SIM Does Not Have Identitifire");

        }
        private string GetSIMIdentifier(SIMWithIdentifiers SIM)
        {
            string SIMIdentifierType = GetSIMIdentifierType(SIM);
            if (SIMIdentifierType == SIMsIdentifiers.IMSI)
            {
                return SIM.IMSI.Value.ToString();
            }
            else if (SIMIdentifierType == SIMsIdentifiers.ICCID)
            {
                return SIM.ICCID.Value.ToString();

            }
            throw new Exception("SIM Does Not Have Identitifire");

        }

        private async Task AddSIMsHistory(CustomerSims SIM, EIOTSIMStatus OldStatus, string Note, long CreatedById)
        {
            try
            {
                CustomerSimsHistory History = new CustomerSimsHistory(SIM, OldStatus, Note, CreatedById);
                _customerSimsHistoryRepository.Add(History);
                await _customerSimsHistoryRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        private async Task<(bool, string)> AlreadySuspendedThisMonth(List<long> Sims)
        {
            var ThisYear = DateTimeOffset.Now.ToUniversalTime().Year;
            var ThisMonth = DateTimeOffset.Now.ToUniversalTime().Month;

            var HasSuspensionHistory = await _customerSimsHistoryRepository.Query()
                .Include(x => x.CustomerSims)
                .Where(x =>
                         Sims.Contains(x.CustomerSimsId) &&
                           x.NewStatus == EIOTSIMStatus.Suspended &&
                           x.CreationDate.Month == ThisMonth && x.CreationDate.Year == ThisYear)
                .FirstOrDefaultAsync();




            if (HasSuspensionHistory is not null)
            {
                return (false, "SIM " + HasSuspensionHistory.CustomerSims.SIMName + " Already Been Suspended This Month");
            }

            return (true, string.Empty);
        }
        private async Task ValidateSIMAction(List<CustomerSims> TargetSIMs, ESIMsAction Action)
        {

            var HaveInvalidSIM = TargetSIMs.FirstOrDefault(s => !SIMsActions.CanMakeTheAction(Action, s.Status));

            if (HaveInvalidSIM is not null)
            {
                throw new Exception("You Can Not " + Enum.GetName(typeof(ESIMsAction), Action) + " " + Enum.GetName(typeof(EIOTSIMStatus), HaveInvalidSIM.Status) + " SIM.");
            }

            if (Action == ESIMsAction.SUSPEND)
            {

                #region One Suspension per Month

                var CheckIsSuspendedThisMonth = await AlreadySuspendedThisMonth(TargetSIMs.Select(x => x.Id).ToList());


                if (!CheckIsSuspendedThisMonth.Item1)
                {
                    throw new Exception(CheckIsSuspendedThisMonth.Item2);
                }

                #endregion

                var PandOfSespention = await GetSIMsThatCanNotBeSuspendedAnyMore(TargetSIMs.Select(x => x.Id).ToList());

                if (PandOfSespention.Any())
                {
                    throw new Exception("Some SIMs Exceded Total Suspenstion Period Per Year");
                }
            }


        }


        private async Task<bool> IsFirstActivation(CustomerSims Sim)
        {
            var simHistory = await _customerSimsHistoryRepository.Query().AsNoTracking()
                                                        .Where(x => x.CustomerSimsId == Sim.Id &&
                                                        x.NewStatus == EIOTSIMStatus.Active).FirstOrDefaultAsync();



            if (simHistory is null)
                return true;

            return false;
        }
        #endregion
    }

}
