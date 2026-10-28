using EskaCMS.Infrastructure.Data;
using EskaCMS.WorkFlow.Services.Interface;
using EskaCMS.Entities.Entity.WorkFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Core.Extensions;
using EskaCMS.WorkFlow.Models.Request;
using static EskaCMS.Entities.Enum.GeneralEnums;
using Microsoft.EntityFrameworkCore;
using EskaCMS.WorkFlow.Models.Response;
using EskaCMS.Infrastructure.Web.SmartTable;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;
using EskaCMS.Infrastructure.Pagination;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace EskaCMS.WorkFlow.Services
{
    public class WorkFlowService : IWorkFlowService
    {
        private readonly IRepository<WorkFlowSetup> _workFlowSetupRepository;
        private readonly IRepository<WorkFlowDetails> _workFlowDetailsRepository;
        private readonly IStageServices _stageServices;
        private readonly IWorkContext _workContext;
        private readonly IMapper _mapper;
        public WorkFlowService(IRepository<WorkFlowSetup> workFlowSetupRepository,
                               IWorkContext workContext,
                                IMapper mapper,
                                IRepository<WorkFlowDetails> workFlowDetailsRepository,
                                IStageServices stageServices)
        {
            _workFlowSetupRepository = workFlowSetupRepository;
            _workContext = workContext;
            _mapper = mapper;
            _workFlowDetailsRepository = workFlowDetailsRepository;
            _stageServices = stageServices;

        }
        //public async Task<bool> ValidateWorkFlow(WorkFlowSetup model , long siteId)
        //{
        //    var allWorkFlows = _workFlowSetupRepository.Query()
        //                                                .Where(x => x.Status == EStatus.Active && x.SiteId == siteId)
        //                                                .ToList();

        //    if (allWorkFlows.Any(x => x.Type == model.Type && x.Id != model.Id))
        //        throw new Exception("Conflicting WorkFlow detected because another setup with same type is active");



        //    return true;
        //}

        #region WorkFlowSetup

        public async Task<bool> CreateWorkFlow(CreateWorkFlowDto model)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();

            var workFlow = new WorkFlowSetup
            {
                Type = model.Type,
                WorkFlowDetails = model.WorkFlowDetails.Select(x => new WorkFlowDetails { CultureId = x.CultureId, Name = x.Name }).ToList(),
                SiteId = siteId,
                Status = EStatus.Published,
                CreationDate = DateTimeOffset.Now,
                CreatedById = userId,
            };


            _workFlowSetupRepository.Add(workFlow);
            await _workFlowSetupRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> EditWorkFlow(EditWorkFlowDto model)
        {
            var siteId = _workContext.GetCurrentSiteId();

            var userId = await _workContext.GetCurrentUserId();
            var WorkFlow = _workFlowSetupRepository.Query()
                                                        .Where(x => x.Id == model.Id && x.SiteId == siteId)
                                                        .Include(x => x.WorkFlowDetails)
                                                        .FirstOrDefault();
            if (WorkFlow == null)
                throw new Exception("WorkFlow Doesn't Exist");


            WorkFlow.Type = model.Type;
            WorkFlow.ModifiedById = userId;

            //await ValidateWorkFlow(WorkFlow, siteId);

            //await _workFlowSetupRepository.SaveChangesAsync();

            await UpdateWorkFlowDetails(WorkFlow.WorkFlowDetails, model.WorkFlowDetails, WorkFlow.Id);

            return true;
        }


        public async Task<bool> UpdateWorkFlowDetails(List<WorkFlowDetails> savedWorkFlowDetails, List<WorkFlowCutlureDetailsDto> updatedWorkFlowDetails, long workFlowId)
        {


            foreach (var item in updatedWorkFlowDetails)
            {
                var locationDetails = savedWorkFlowDetails.Where(x => x.CultureId == item.CultureId).FirstOrDefault();
                if (locationDetails != null)
                {
                    locationDetails.Name = item.Name;


                    _workFlowDetailsRepository.Update(locationDetails);
                }
                else
                {
                    var neweventLocationDetails = new WorkFlowDetails
                    {
                        Name = item.Name,
                        CultureId = item.CultureId,

                        WorkFlowId = workFlowId

                    };

                    _workFlowDetailsRepository.Add(neweventLocationDetails);
                }
            }
            await _workFlowDetailsRepository.SaveChangesAsync();

            await _workFlowSetupRepository.SaveChangesAsync();
            return true;

        }


        public async Task<bool> ActiveWorkFlow(long Id)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();

            var WorkFlow = _workFlowSetupRepository.Query()
                                                        .Where(x => x.Id == Id && x.SiteId == siteId)
                                                        .FirstOrDefault();
            if (WorkFlow == null)
                throw new Exception("WorkFlow Doesn't Exist");

            WorkFlow.Status = EStatus.Published;
            WorkFlow.ModifiedById = userId;

            //await ValidateWorkFlow(WorkFlow, siteId);

            await _workFlowSetupRepository.SaveChangesAsync();

            return true;
        }

        public async Task<WorkFlowDetailsDto> GetWorkFlowDetails(long Id)
        {
            var siteId = _workContext.GetCurrentSiteId();

            var cultureId = _workContext.GetCurrentCultureId();


            var WorkFlow = _workFlowSetupRepository.Query().AsNoTracking()
                                                        .Where(x => x.Id == Id && x.SiteId == siteId && x.Status != EStatus.Deleted)
                                                        .Include(x => x.WorkFlowDetails)
                                                        .ThenInclude(x => x.Culture)
                                                        .Include(x => x.Stages)
                                                        .ThenInclude(x => x.StageDetails)
                                                        .Include(x => x.CreatedBy)
                                                        .Include(x => x.ModifiedBy)
                                                        .Include("Stages.CreatedBy")
                                                        .Include("Stages.ModifiedBy")
                                                        .FirstOrDefault();

            if (WorkFlow == null)
                throw new Exception("WorkFlow Doesn't Exist");


            var result = new WorkFlowDetailsDto
            {
                CreatedBy = WorkFlow.CreatedBy.FullName,
                CreationDate = WorkFlow.CreationDate,
                Id = WorkFlow.Id,
                ModifiedBy = WorkFlow.ModifiedById.HasValue ? WorkFlow.ModifiedBy.FullName : "",
                Name = WorkFlow.WorkFlowDetails.Where(x => x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                WorkFlowDetails = WorkFlow.WorkFlowDetails.Select(y => new WorkFlowCutlureDetailsDto { CultureId = y.CultureId, Name = y.Name, CutlureCode = y.Culture.CultureId }).ToList(),
                Status = WorkFlow.Status,
                Type = WorkFlow.Type,
                Stages = WorkFlow.Stages.Where(x => x.Status == EStatus.Published)
                                        .OrderBy(s => s.Order)
                                        .Select(y => new StageDetailsDto
                                        {
                                            Id = y.Id,
                                            RoleId = y.RoleId,
                                            CustomerGroupId = y.CustomerGroupId,
                                            IsOverridable = y.IsOverridable,
                                            CreationDate = y.CreationDate,
                                            ModifiedBy = y.ModifiedById.HasValue ? y.ModifiedBy.FullName : "",
                                            Description = y.StageDetails.Where(x => x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                                            CreatedBy = y.CreatedBy.FullName,
                                            Order = y.Order,
                                            ApprovalStep=y.ApprovalStep,
                                            Type = y.Type,
                                            RejectionBehavior = y.RejectionBehavior,
                                            StageDetails = y.StageDetails.Select(d => new StageCultureDetailsDto { CultureCode = d.Culture.CultureId, CultureId = d.CultureId, Description = d.Description }).ToList()
                                        }).ToList(),

                
            };


            return result;
        }

        public async Task<PaginationResult<WorkFlowDetailsDto>> GetAllWorkFlows(SmartTableParam param)
        {

            var siteId = _workContext.GetCurrentSiteId();

            var cultureId = _workContext.GetCurrentCultureId();

            var query = _workFlowSetupRepository.Query()
                                                    .Where(x => x.SiteId == siteId &&
                                                            x.Status != EStatus.Deleted)
                                                    .Include(x => x.WorkFlowDetails)
                                                    .AsNoTrackingWithIdentityResolution();





            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;



                if (search.Name != null)
                {
                    string Name = search.Name;
                    string normalizedName = Name.ToLower();
                    query = query.Where(x => x.WorkFlowDetails.Any(x => x.Name.ToLower().Contains(normalizedName)));
                }


                if (search.Status != null)
                {
                    var status = (EStatus)search.Status;
                    query = query.Where(x => x.Status == status);
                }

                if (search.Type != null)
                {
                    var WorkFlowType = (EWorkFlowType)search.Type;
                    query = query.Where(x => x.Type == WorkFlowType);
                }


            }

            if (!query.Any())
            {
                return new PaginationResult<WorkFlowDetailsDto>(new List<WorkFlowDetailsDto>(), 0, 0);
            }






            var result = await query.Select(x => new WorkFlowDetailsDto
            {
                Id = x.Id,
                Stages = new List<StageDetailsDto>(),
                Name = x.WorkFlowDetails.Where(x => x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                WorkFlowDetails = x.WorkFlowDetails.Select(d => new WorkFlowCutlureDetailsDto { Name = d.Name, CultureId = d.CultureId, CutlureCode = d.Culture.CultureId }).ToList(),
                CreatedBy = x.CreatedBy.FullName,
                CreationDate = x.CreationDate,
                Type = x.Type,
                Status = x.Status,

            }).PaginateAsync(param.Pagination.Number, param.Pagination.Start);




            return result;

        }

        public async Task<bool> DeActivateWorkFlow(long Id)
        {
            var siteId = _workContext.GetCurrentSiteId();

            var userId = await _workContext.GetCurrentUserId();
            var WorkFlow = _workFlowSetupRepository.Query()
                                                        .Where(x => x.Id == Id && x.SiteId == siteId)
                                                        .FirstOrDefault();
            if (WorkFlow == null)
                throw new Exception("WorkFlow Doesn't Exist");

            WorkFlow.Status = EStatus.Unpublished;
            WorkFlow.ModifiedById = userId;


            await _workFlowSetupRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteWorkFlow(long Id)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();

            var workflow = _workFlowSetupRepository.Query()
                                                        .Where(x => x.Id == Id && x.SiteId == siteId)
                                                        .Include(x => x.Stages)
                                                        .Include(x => x.Stages)
                                                        .ThenInclude(x => x.WorkFlowItems)
                                                        .FirstOrDefault();
            if (workflow == null)
                throw new Exception("WorkFlow Doesn't Exist");


            var workflowItems = workflow.Stages.SelectMany(x => x.WorkFlowItems).ToList();

            if (workflowItems.Any())
            {
                throw new Exception("Cannot delete this workflow because there is workflow item on it");
            }

            workflow.Status = EStatus.Deleted;

            workflow.ModifiedById = userId;
            await _workFlowSetupRepository.SaveChangesAsync();

            return true;
        }

        #endregion








        #region Helpers


        #endregion



    }
}
