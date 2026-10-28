using AutoMapper;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.WorkFlow;
using EskaCMS.Infrastructure.Data;
using EskaCMS.WorkFlow.Models.Request;
using EskaCMS.WorkFlow.Models.Response;
using EskaCMS.WorkFlow.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.WorkFlow.Services
{
    public class StageServices : IStageServices
    {

        private readonly IRepository<Stage> _stageRepository;
        private readonly IRepository<StageDetails> _stageDetailsRepository;
        private readonly IWorkContext _workContext;
        private readonly IRepository<WorkFlowSetup> _workFlowSetupRepository;
        private readonly IRepository<WorkFlowItem> _workFlowItemRepository;
        private readonly IMapper _mapper;
        public StageServices(IRepository<Stage> stageRepository,
                             IWorkContext workContext,
                             IRepository<WorkFlowSetup> workFlowSetupRepository,
                             IRepository<WorkFlowItem> workFlowItemRepository,
                             IRepository<StageDetails> stageDetailsRepository,
                             IMapper mapper)
        {
            _stageRepository = stageRepository;
            _workContext = workContext;
            _workFlowSetupRepository = workFlowSetupRepository;
            _workFlowItemRepository = workFlowItemRepository;
            _stageDetailsRepository = stageDetailsRepository;
            _mapper=mapper; 

        }

        public async Task<bool> ValidateStage(Stage model, long siteId)
        {
            var allStages = _workFlowSetupRepository.Query()
                                                        .Where(x =>x.Id == model.WorkFlowId && x.Status == EStatus.Published && x.SiteId == siteId)
                                                        .Include(x=>x.Stages)
                                                        .Select(x=>x.Stages)
                                                        .FirstOrDefault();




            if(allStages != null)
            {

                if (allStages.Any(x => x.Order == model.Order && x.Id != model.Id))
                    throw new Exception("Conflicting between stages detected because another stage has  the same order");
            }

            if (model.Type == EStageType.RoleBased && !model.RoleId.HasValue)
                throw new Exception("Please select role for this stage");
            
            if (model.Type == EStageType.RoleBased && model.CustomerGroupId.HasValue)
                throw new Exception("Please just  select role for this stage becuase it has role based type");
            
            if (model.Type == EStageType.CustomerGroup && !model.CustomerGroupId.HasValue)
                throw new Exception("Please select CustomerGroup for this stage");

            if (model.Order <= 0 )
                throw new Exception("Please add valid order for this stage");
            
           

            return true;
        }
        public async Task<bool> CreateStage(CreateStageDto model)
        {
            
            var userId = await _workContext.GetCurrentUserId();
            var siteId = _workContext.GetCurrentSiteId();

            var lastStage = _stageRepository.Query()
                                            .Where(x => x.WorkFlowId == model.WorkFlowId && x.WorkFlow.SiteId == siteId)
                                            .OrderBy(x => x.Order)
                                            .LastOrDefault();
            var stageOrder = 1;

            if(lastStage != null)
            {
                stageOrder = lastStage.Order + 1;
            }

            var stage = new Stage
            { 
                Type = model.Type,
                StageDetails=model.StageDetails.Select(x=> new StageDetails { CultureId = x.CultureId,Description=x.Description}).ToList(),
                CustomerGroupId=model.CustomerGroupId,
                RoleId=model.RoleId,
                WorkFlowId=model.WorkFlowId,
                CreationDate = DateTimeOffset.Now,
                CreatedById = userId,
                Order= stageOrder,
                Status= EStatus.Published,
                IsOverridable=model.IsOverridable,
                RejectionBehavior= model.RejectionBehavior.Value,
                ApprovalStep= model.ApprovalStep.HasValue? model.ApprovalStep.Value: null,
                
            };

            
               await ValidateStage(stage,siteId);

            
            _stageRepository.Add(stage);
            await _stageRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> EditMultipleOrderStage(List<StageOrderDto> stages)
        {

            var userId = await _workContext.GetCurrentUserId();
            var siteId = _workContext.GetCurrentSiteId();

            if (stages.Select(x => x.Order).Distinct().Count() != stages.Count)
            {
                
                throw new Exception("Duplicate order values are not allowed.");
            }

            var stageIds = stages.Select(x=>x.Id).ToList();

            var targetStages = _stageRepository.Query()
                                            .Where(x => stageIds.Contains(x.Id))
                                            .ToList();


            foreach (var stageInfo in stages)
            {

                var stage = targetStages.Where(x => x.Id == stageInfo.Id).FirstOrDefault();

                if(stage != null)
                {
                    stage.Order = stageInfo.Order;
                }

            }

            
            await _stageRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateStage(EditStageDto model)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();
                var siteId = _workContext.GetCurrentSiteId();

                var stage = _stageRepository.Query()
                                            .Where(x => x.Id == model.Id)
                                            .Include(x=>x.StageDetails)
                                            .FirstOrDefault();



                if (stage == null)
                {
                    throw new Exception("Stage Doesn't Exist");
                }

               // stage.Description = model.Description;
                stage.CustomerGroupId = model.CustomerGroupId;
                stage.RoleId = model.RoleId;
                stage.Type = model.Type;
                stage.ModifiedById = userId;
                stage.IsOverridable = model.IsOverridable;
                stage.RejectionBehavior = model.RejectionBehavior;
                stage.ApprovalStep = model.ApprovalStep.HasValue ? model.ApprovalStep.Value : null;


                await ValidateStage(stage, siteId);

                await UpdateStageDetails(stage.StageDetails,model.StageDetails,model.Id);

                //await _stageRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception e)
            {

                throw;
            }

           
        }

        public async Task<bool> UpdateStageDetails(List<StageDetails> savedStageDetails, List<StageCultureDetailsDto> updatedStageDetails, long stageId)
        {


            foreach (var item in updatedStageDetails)
            {
                var stageDetails = savedStageDetails.Where(x => x.CultureId == item.CultureId).FirstOrDefault();
                if (stageDetails != null)
                {
                    stageDetails.Description = item.Description;


                    _stageDetailsRepository.Update(stageDetails);
                }
                else
                {
                    var neweventLocationDetails = new StageDetails
                    {
                        Description = item.Description,
                        CultureId = item.CultureId,

                        StageId = stageId

                    };

                    _stageDetailsRepository.Add(neweventLocationDetails);
                }
            }
            await _stageDetailsRepository.SaveChangesAsync();

            await _stageRepository.SaveChangesAsync();
            return true;

        }


        public async Task<bool> DeleteStage(long Id)
        {

            var userId = await _workContext.GetCurrentUserId();
            var siteId = _workContext.GetCurrentSiteId();
            
            if (_workFlowItemRepository.Query().Any(x=>x.CurrentStageId == Id))
                throw new Exception("Cannot delete this stage because there is workflow item on it");

            var stage = _stageRepository.Query()
                                        .Where(x => x.Id == Id)
                                        .FirstOrDefault();

            if (stage == null)
            {
                throw new Exception("Stage Doesn't Exist");
            }

            

            await ValidateStage(stage, siteId);

            stage.Status = EStatus.Deleted;
            stage.ModifiedById = userId;
            await _stageRepository.SaveChangesAsync();

            return true;
        }

        public async Task<StageDetailsDto> GetStageDetails(long Id)
        {

            var userId = await _workContext.GetCurrentUserId();
            var siteId = _workContext.GetCurrentSiteId();

            if (_workFlowItemRepository.Query().Any(x => x.CurrentStageId == Id))
                throw new Exception("Cannot delete this stage because there is workflow item on it");

            var stage = _stageRepository.Query()
                                        .Where(x => x.Id == Id)
                                        .Include(x=>x.CreatedBy)
                                        .Include(x=>x.ModifiedBy)
                                        .FirstOrDefault();

            if (stage == null)
            {
                throw new Exception("Stage Doesn't Exist");
            }


            var stageDetails = _mapper.Map<StageDetailsDto>(stage);
            

            return stageDetails;
        }

      
    }
}
