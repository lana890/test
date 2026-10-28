using AutoMapper;
using AutoMapper.QueryableExtensions;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.WorkFlow;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Localization.Services.Interfaces;
using EskaCMS.WorkFlow.Models;
using EskaCMS.WorkFlow.Models.Request;
using EskaCMS.WorkFlow.Models.Response;
using EskaCMS.WorkFlow.Services.Interface;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.WorkFlow.Services
{
    public class WorkFlowItemService : IWorkFlowItemService
    {

        private readonly IRepository<WorkFlowSetup> _workFlowSetupRepository;
        private readonly IRepository<Stage> _stageRepository;
        private readonly IRepository<UsersSites> _userSitesRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<WorkFlowItem> _workFlowItemRepository;
        private readonly IRepository<ItemHistory> _itemHitsoryRepository;
        private readonly IMapper _mapper;
        private readonly IWorkContext _workContext;
        private readonly ISiteSettingsService _siteSettings;
        private readonly ITestNotifier _testNotifier;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILocalizationServices _localizationServices;

        public WorkFlowItemService(IRepository<WorkFlowSetup> workFlowSetupRepository,
                                   IRepository<Stage> stageRepository,
                                   IRepository<WorkFlowItem> workFlowItemRepository,
                                   IMapper mapper,
                                   ILocalizationServices localizationServices,
                                   IRepository<User> userRepository,
                                   IWorkContext workContext,
                                   IRepository<UsersSites> userSitesRepository,
                                   IRepository<ItemHistory> itemHitsoryRepository,
                                   ISiteSettingsService siteSettings,
                                   IServiceScopeFactory serviceScopeFactory,
                                   ITestNotifier testNotifier)
        {

            _workFlowSetupRepository = workFlowSetupRepository;
            _workFlowItemRepository = workFlowItemRepository;
            _stageRepository = stageRepository;
            _mapper = mapper;
            _workContext = workContext;
            _userSitesRepository = userSitesRepository;
            _itemHitsoryRepository = itemHitsoryRepository;
            _siteSettings = siteSettings;
            _testNotifier = testNotifier;
            _userRepository = userRepository;
            _serviceScopeFactory = serviceScopeFactory;
            _localizationServices = localizationServices;
        }

        public async Task<bool> ActionOnWorkFlowItem(ActionOnItemDto model)
        {

            var userId = await _workContext.GetCurrentUserId();
            var workflowItem = _workFlowItemRepository.Query()
                                                     .Where(x => x.Id == model.ItemId)
                                                     .Include(x => x.CurrentStage)
                                                     .ThenInclude(x => x.WorkFlow)
                                                     .ThenInclude(x => x.WorkFlowDetails)
                                                     .ThenInclude(x => x.Culture)
                                                     .Include(x => x.CreatedBy)
                                                     .FirstOrDefault();


            if (workflowItem.Status == EFlowStatus.Approved)
            {
                throw new Exception("This workflow item already approved");
            }


            if (workflowItem.Status == EFlowStatus.Rejected)
            {
                throw new Exception("This workflow item already rejected");
            }

            var authStages = await GetAccessibleStages(workflowItem.CurrentStage.WorkFlowId);


            if (!(await UserCanMakeAction(workflowItem, userId, authStages)))
                throw new Exception("You don't have permission to do this action");


            var stages = _stageRepository.Query()
                                         .Where(x => (x.WorkFlowId == workflowItem.CurrentStage.WorkFlowId) && x.Status == EStatus.Published)
                                         .OrderBy(x => x.Order)
                                         .ToList();


            if (model.Action == EActionType.Approve)
            {
                await Approve(userId, workflowItem, model.Note, stages, authStages);
            }
            else
            {
                await Reject(userId, workflowItem, model.Note, stages);
            }

            return true;


        }

        public async Task<bool> Approve(long userId, WorkFlowItem workflowItem, string note, List<Stage> stages, AuthorizedStageVM authorizedStage)
        {
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var currentStageId = workflowItem.CurrentStageId;

            if (IsOverridableStage(authorizedStage, workflowItem))
            {
                await HandleOverridableStage(workflowItem, orderedStages, authorizedStage);
            }

            else if (workflowItem.CurrentStage.Type == EStageType.Hierarchical)
            {
                await HandleHierarchicalStage(userId, workflowItem, orderedStages);
            }

            else if (IsLastStage(workflowItem, orderedStages))
            {
                await ApproveFinalStage(workflowItem);
            }
            else
            {
                await MoveToNextStage
                   (workflowItem, orderedStages);

            }


            await AddItemHistory(EActionType.Approve, note, userId, workflowItem.Id, null, currentStageId);
            return true;
        }

        public async Task<bool> Reject(long userId, WorkFlowItem workflowItem, string note, List<Stage> stages)
        {
            var orderdStages = stages.OrderBy(x => x.Order).ToList();



            var currentStageId = workflowItem.CurrentStageId;


            if (workflowItem.CurrentStage.Order != orderdStages.FirstOrDefault()?.Order && orderdStages.Count > 1 && workflowItem.CurrentStage.RejectionBehavior == ERejectionBehavior.BackToPreviousStage)
            {

                var currentIndex = orderdStages.FindIndex(x => x.Id == currentStageId);
                var previousStage = orderdStages[currentIndex - 1];
                workflowItem.CurrentStageId = previousStage.Id;

                await HandleAdminNotfication(previousStage.Id, workflowItem, string.Empty);

            }
            else
            {
                workflowItem.Status = EFlowStatus.Rejected;



                workflowItem.CurrentStageId = orderdStages.FirstOrDefault().Id;



                //await WorkflowItemStatusNotfication(workflowItem.CreatedById, workflowItem.Id, EFlowStatus.Rejected);

            }
            _workFlowItemRepository.Update(workflowItem);
            _workFlowItemRepository.SaveChanges();

            await AddItemHistory(EActionType.Reject, note, userId, workflowItem.Id, null, currentStageId);

            return true;

        }



        private bool IsOverridableStage(AuthorizedStageVM authorizedStage, WorkFlowItem workflowItem)
        {
            return authorizedStage.OverridableStages.Any(x => x.Id == workflowItem.CurrentStageId) &&
                   authorizedStage.AssignedStages.Any(x => x.Order > workflowItem.CurrentStage.Order);
        }


        private async Task<bool> HandleOverridableStage(WorkFlowItem workflowItem, List<Stage> orderedStages, AuthorizedStageVM authorizedStage)
        {
            var candidateStages = authorizedStage.AssignedStages.Where(x => x.Order > workflowItem.CurrentStage.Order)
                                                                 .OrderBy(x => x.Order)
                                                                 .ToList();
            var nextStage = candidateStages.FirstOrDefault();
            workflowItem.CurrentStageId = nextStage.Id;

            if (nextStage.Id == orderedStages.Last().Id)
            {

                await ApproveFinalStage(workflowItem);

                return true;
            }

            await UpdateWorkflowItem(workflowItem);

            await HandleAdminNotfication(nextStage.Id, workflowItem, string.Empty);

            return true;
        }

        private async Task<bool> HandleHierarchicalStage(long userId, WorkFlowItem workflowItem, List<Stage> orderedStages)
        {
            var currentRequiredApprove = _userRepository.Query()
                                                        .Include(x => x.Parent)
                                                        .FirstOrDefault(x => x.Id == userId);

            if (currentRequiredApprove is null)
            {
                throw new Exception("The User Should Approve doesn't exist");
            }

            if (currentRequiredApprove.ParentId.HasValue && workflowItem.CurrentStage.ApprovalStep != EApprovalStep.FirstStep)
            {
                workflowItem.RequiredApprovedId = currentRequiredApprove.ParentId.Value;
                await HandleAdminNotfication(workflowItem.CurrentStageId, workflowItem, currentRequiredApprove.Parent.Email);
            }
            else if ( workflowItem.CurrentStage.ApprovalStep == EApprovalStep.FirstStep && IsLastStage(workflowItem, orderedStages))
            {
                workflowItem.RequiredApprovedId = null;
                await ApproveFinalStage(workflowItem);
                return true;
            }
            else if (!currentRequiredApprove.ParentId.HasValue  && IsLastStage(workflowItem, orderedStages))
            {
                workflowItem.RequiredApprovedId = null;
                await ApproveFinalStage(workflowItem);
                return true;
            }
            else
            {
                await MoveToNextStageInOrder(workflowItem, orderedStages);
            }

            await UpdateWorkflowItem(workflowItem);
            return true;
        }

        private bool IsLastStage(WorkFlowItem workflowItem, List<Stage> orderedStages)
        {
            return workflowItem.CurrentStage.Order == orderedStages.Last().Order;
        }

        private async Task<bool> ApproveFinalStage(WorkFlowItem workflowItem)
        {
            workflowItem.Status = EFlowStatus.Approved;
            await UpdateWorkflowItem(workflowItem);

            await HandlePublishContent(workflowItem);

            await WorkflowItemNotfication(workflowItem.CreatedById, workflowItem, EFlowStatus.Approved);

            return true;
        }

        private async Task<bool> MoveToNextStage(WorkFlowItem workflowItem, List<Stage> orderedStages)
        {
            MoveToNextStageInOrder(workflowItem, orderedStages);
            await UpdateWorkflowItem(workflowItem);
            return true;
        }

        private async Task<bool> MoveToNextStageInOrder(WorkFlowItem workflowItem, List<Stage> orderedStages)
        {
            var currentIndex = orderedStages.FindIndex(x => x.Id == workflowItem.CurrentStageId);
            var nextStage = orderedStages[currentIndex + 1];
            workflowItem.CurrentStageId = nextStage.Id;



            await HandleAdminNotfication(nextStage.Id, workflowItem, string.Empty);

            return true;
        }

        private async Task UpdateWorkflowItem(WorkFlowItem workflowItem)
        {
            _workFlowItemRepository.Update(workflowItem);
            await _workFlowItemRepository.SaveChangesAsync();
        }

        private async Task<bool> HandlePublishContent(WorkFlowItem workflowItem)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var mediaRepository = scope.ServiceProvider.GetRequiredService<IRepository<Media>>();

                    if (workflowItem.MediaId.HasValue)
                    {
                        var mediaShouldUpdated = mediaRepository.Query().Where(x => x.Id == workflowItem.MediaId).FirstOrDefault();

                        if (mediaShouldUpdated is not null)
                        {
                            mediaShouldUpdated.Status = EStatus.Published;
                            mediaRepository.Update(mediaShouldUpdated);
                            await mediaRepository.SaveChangesAsync();
                        }


                    }


                }

                return true;
            }
            catch (Exception e)
            {

                throw;
            }
            
        }

        public async Task<bool> AddItemHistory(EActionType action, string note, long userId, long workflowItemId, long? previousStage, long? currentStage)
        {


            var itemHistory = new ItemHistory
            {
                ActionType = action,
                CreatedById = userId,
                Note = note,
                WorkFlowItemId = workflowItemId,
                CurrentStageId = currentStage ?? null,
                PreviousStageId = previousStage ?? null,
                CreationDate = DateTimeOffset.Now

            };

            _itemHitsoryRepository.Add(itemHistory);
            await _itemHitsoryRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> HandleAdminNotfication(long currentStageId, WorkFlowItem workflowItem, string adminEmail)
        {

            var siteId = _workContext.GetCurrentSiteId();
            var targetStage = _stageRepository.Query()
                                               .Where(x => x.Id == currentStageId && x.WorkFlow.SiteId == siteId)
                                               .FirstOrDefault();

            var users = new List<User>();
            var adminEmails = new List<string>();

            if (targetStage.Type == EStageType.RoleBased)
            {
                users = _userSitesRepository.Query()
                                            .Where(x => x.User.UserRoles.Any(x => x.RoleId == targetStage.RoleId) && x.SiteId == siteId)
                                            .Select(x => x.User)
                                            .ToList();

            }
            else if (targetStage.Type == EStageType.CustomerGroup)
            {
                users = _userSitesRepository.Query()
                                            .Where(x => x.User.CustomerGroups.Any(x => x.CustomerGroupId == targetStage.CustomerGroupId) && x.SiteId == siteId)
                                            .Select(x => x.User)
                                            .ToList();
            }
            else if (targetStage.Type == EStageType.Hierarchical)
            {
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    adminEmails.Add(adminEmail);

                }
                else
                {
                    adminEmails.Add(workflowItem.RequiredApproved.Email);

                }
            }



            adminEmails.AddRange(users.Select(x => x.Email).ToList());


            if (!adminEmails.Any())
                return false;

            await SendAdminNotfication(adminEmails, workflowItem);

            return true;
        }

        public async Task<bool> HandleUserNotfication(long userId)
        {

            var siteId = _workContext.GetCurrentSiteId();


            return true;
        }



        public async Task<AuthorizedStageVM> GetAccessibleStages(long workflowId)
        {

            var siteId = _workContext.GetCurrentSiteId();

            var userId = await _workContext.GetCurrentUserId();

            var user = _userRepository.Query().AsNoTracking()
                                       .Where(x => x.Id == userId)
                                       .Include(x => x.UserRoles)
                                       .Include(x => x.CustomerGroups)
                                       .FirstOrDefault();


            var Stages = await _stageRepository.Query().AsNoTracking()
                                               .Where(x => x.WorkFlowId == workflowId && x.WorkFlow.SiteId == siteId)
                                               .ToListAsync();


            var assignedStages = Stages.Where(x =>!(x.CustomerGroupId.HasValue ||x.RoleId.HasValue) ||  (user.CustomerGroups.Any(y => y.CustomerGroupId == x.CustomerGroupId) || user.UserRoles.Any(y => y.RoleId == x.RoleId)))
                                     .ToList();

            var overridableStage = Stages.Where(x => x.IsOverridable && assignedStages.Any(y => y.Id != x.Id || x.Order < y.Order))
                                         .ToList();

            var result = new AuthorizedStageVM { AssignedStages = assignedStages, OverridableStages = overridableStage };


            return result;
        }


        public async Task<bool> UserCanMakeAction(WorkFlowItem workflowItem, long userId, AuthorizedStageVM authStages)
        {

            var siteId = _workContext.GetCurrentSiteId();
            var user = _userSitesRepository.Query()
                                       .Where(x => x.UserId == userId && x.SiteId == siteId)
                                       .Include(x => x.User)
                                       .Include(x => x.User)
                                       .ThenInclude(x => x.UserRoles)
                                       .Include(x => x.User)
                                       .ThenInclude(x => x.CustomerGroups)
                                       .Select(x => x.User)
                                       .FirstOrDefault();






            if (workflowItem.CurrentStage.Type == EStageType.RoleBased)
            {
                if (user.UserRoles.Any(x => x.RoleId == workflowItem.CurrentStage.RoleId))
                    return true;
            }

            if (workflowItem.CurrentStage.Type == EStageType.CustomerGroup && !workflowItem.CurrentStage.RoleId.HasValue)
            {
                if (user.CustomerGroups.Any(x => x.CustomerGroupId == workflowItem.CurrentStage.CustomerGroupId))
                    return true;
            }
            if (workflowItem.CurrentStage.Type == EStageType.Hierarchical && (workflowItem.RequiredApprovedId.HasValue && user.Id == workflowItem.RequiredApprovedId.Value))
            {
                return true;

            }
            if (authStages.OverridableStages.Any(x => x.Id == workflowItem.CurrentStageId))
            {
                return true;

            }

            return false;
        }

        public async Task<long> CreateWorkFlowItem(CreateWorkFlowItemDto model)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var workflowSetup = _workFlowSetupRepository.Query()
                                                        .Where(x => x.Id == model.WorkFlowId && x.SiteId == siteId && x.Status == EStatus.Published)
                                                        .Include(x => x.Stages)
                                                        .FirstOrDefault();

            if (!model.UserId.HasValue)
            {
                model.UserId = await _workContext.GetCurrentUserId();
            }
            if (workflowSetup == null)
                throw new Exception("The WorkFlow doesn't Exist");


            var firstStage = workflowSetup.Stages.Where(x=>x.Status == EStatus.Published).MinBy(x => x.Order);

            if (firstStage == null)
                throw new Exception("Please add stages for the workflow");



            long? parentIdIfHirerchal = null;

            if (firstStage.Type == EStageType.Hierarchical)
            {
                var user = _userSitesRepository.Query()
                                  .Where(x => x.UserId == model.UserId && x.SiteId == siteId)
                                  .Include(x => x.User)
                                  .ThenInclude(x=>x.Parent)
                                  .Select(x => x.User)
                                  .FirstOrDefault();


                parentIdIfHirerchal = (user.Parent != null && user.Parent.Email.Contains("nael.salah@eskadenia.com"))
                                      ? user.Id
                                      : user.ParentId;



            }




            var workflowItem = new WorkFlowItem
            {
                SubmittedFormId = model.SubmittedFormId ?? null,
                CreatedById = model.UserId.Value,
                CurrentStageId = firstStage.Id,
                Status = EFlowStatus.InProgress,
                PageVersionId = model.PageVersionId ?? null,
                TemplateVersionId = model.TemplateVersionId ?? null,
                MediaId = model.MediaId ?? null,
                CreationDate = DateTimeOffset.Now,
                RequiredApprovedId = firstStage.Type == EStageType.Hierarchical ? parentIdIfHirerchal : model.UserId.Value
            };



            _workFlowItemRepository.Add(workflowItem);
            _workFlowItemRepository.SaveChanges();


            var addedWorkFlowItem = _workFlowItemRepository.Query()
                                                           .Where(x => x.Id == workflowItem.Id)
                                                           .Include(x => x.CurrentStage)
                                                           .ThenInclude(x => x.WorkFlow)
                                                           .ThenInclude(x => x.WorkFlowDetails)
                                                           .ThenInclude(x => x.Culture)
                                                           .Include(x => x.RequiredApproved)
                                                           .FirstOrDefault();
            
            await HandleAdminNotfication(firstStage.Id, addedWorkFlowItem, string.Empty);

            if (addedWorkFlowItem.RequiredApprovedId.HasValue && (addedWorkFlowItem.RequiredApprovedId == model.UserId))
            {
                await ActionOnWorkFlowItem(new ActionOnItemDto { Action = EActionType.Approve, ItemId = addedWorkFlowItem.Id, Note = "Automatic Approved By User" }); ;

            }

            return workflowItem.Id;


        }

        public async Task<bool> UpdateItemStatus(long itemId, long userId)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var workflowItem = _workFlowItemRepository.Query()
                                                        .Where(x => x.Id == itemId && x.CurrentStage.WorkFlow.SiteId == siteId && x.Status == EFlowStatus.Rejected)
                                                        .FirstOrDefault();
            if (workflowItem == null)
                throw new Exception("The WorkFlowItem doesn't Exist");


            workflowItem.Status = EFlowStatus.InProgress;
            workflowItem.CreationDate = DateTimeOffset.Now;
            var historyMessage = $"User re-submitted workflow item and Workflow item with Id {itemId} is now in progress at first stage";
            await AddItemHistory(EActionType.Approve, historyMessage, userId, itemId, workflowItem.CurrentStageId, workflowItem.CurrentStageId);

            _workFlowItemRepository.SaveChanges();

            return true;


        }

        public async Task<PaginationResult<WorkFlowItemList>> GetAllWorkItem(SmartTableParam param, long workFlowId)
        {

            var siteId = _workContext.GetCurrentSiteId();

            var cultureId = _workContext.GetCurrentCultureId();

            var userId = await _workContext.GetCurrentUserId();

            var authStages = await GetAccessibleStages(workFlowId);

            var authStagesIds = authStages.AssignedStages.Select(x => x.Id)
                                                         .Concat(authStages.OverridableStages
                                                         .Select(y => y.Id))
                                                         .ToList();


            var query = _workFlowItemRepository.Query()
                                               .Where(x => (x.CurrentStage.WorkFlowId == workFlowId &&
                                                           x.CurrentStage.WorkFlow.SiteId == siteId &&
                                                            authStagesIds.Contains(x.CurrentStageId)) || (x.RequiredApprovedId.HasValue ? x.RequiredApprovedId == userId :true))
                                               .Include(x => x.CurrentStage)
                                               .ThenInclude(x => x.StageDetails)
                                               .Include(x => x.CurrentStage)
                                               .ThenInclude(x => x.WorkFlow)
                                               .ThenInclude(x => x.WorkFlowDetails)
                                               .Include(x => x.CreatedBy)
                                               .Include(x => x.SubmittedForm)
                                               .ThenInclude(x => x.Medias)
                                               .ThenInclude(x => x.Media)
                                               .ThenInclude(x => x.Folder)
                                               .ThenInclude(x => x.MediaFoldersDetails)
                                               .Include(x => x.Media)
                                               .ThenInclude(x=>x.Folder)
                                               .ThenInclude(x=>x.MediaFoldersDetails)
                                               .AsNoTracking();


            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;

                if (search.WorkFlowId != null)
                {
                    long WorkFlowId = (long)search.WorkFlowId;
                    query = query.Where(x => x.CurrentStage.WorkFlowId == WorkFlowId);
                }

                if (search.StageId != null)
                {
                    long StageId = (long)search.StageId;
                    query = query.Where(x => x.CurrentStageId == StageId);
                }

                if (search.Status != null)
                {
                    var status = (EFlowStatus)search.Status;
                    query = query.Where(x => x.Status == status);
                }

                if (search.Type != null)
                {
                    var Type = (EWorkFlowType)search.Type;
                    query = query.Where(x => x.CurrentStage.WorkFlow.Type == Type);
                }


                if (search.From != null)
                {
                    DateTimeOffset From = search.From;
                    query = query.Where(x => x.CreationDate >= From);
                }


                if (search.To != null)
                {
                    DateTimeOffset To = search.To;
                    query = query.Where(x => x.CreationDate <= To);
                }

                if (search.Code != "" && search.Code != null)
                {
                    string Code = search.Code;
                    query = query.Where(x => x.SubmittedForm.Code.Contains(Code));
                }

                if (search.CreatedBy != null)
                {
                    string CreatedBy = search.CreatedBy;
                    var upperCreatedBy = CreatedBy.ToUpper();
                    query = query.Where(x => x.CreatedBy.FullName.ToUpper().Contains(upperCreatedBy) || x.CreatedBy.NormalizedUserName.Contains(upperCreatedBy) || x.CreatedBy.NormalizedEmail.Contains(upperCreatedBy));
                }

                if (search.Search != null)
                {
                    string Search = search.Search;
                    query = query.Where(x => x.CreatedBy.FullName.ToUpper().Contains(Search) || (x.MediaId.HasValue ? x.Media.Filename.ToUpper().Contains(Search) : true));
                }



            }

            if (!query.Any())
            {
                return new PaginationResult<WorkFlowItemList>(new List<WorkFlowItemList>(), 0, 0);
            }

            //var allWorkFlowItem = _mapper.Map<List<WorkFlowItemList>>(query);



            var result = await query.OrderByDescending(x => x.CreationDate).Select(x => new WorkFlowItemList
            {
                Id = x.Id,
                Code = x.SubmittedFormId.HasValue ? x.SubmittedForm.Code : "",
                Type = x.CurrentStage.WorkFlow.Type,
                CreatedBy = x.CreatedBy.FullName,
                CreationDate = x.CreationDate,
                WorkFlowDetails = x.CurrentStage.WorkFlow.WorkFlowDetails.Select(y => new WorkFlowCutlureDetailsDto { CultureId = y.CultureId, CutlureCode = y.Culture.CultureId, Name = y.Name }).ToList(),
                WorkFlowName = x.CurrentStage.WorkFlow.WorkFlowDetails.Where(x => x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                Media = x.MediaId.HasValue ? new AttachmentDto { Id = x.MediaId.Value, PublicUrl = x.Media.PublicUrl, ContentType = x.Media.ContentType ,FileName=x.Media.Filename,
                    ReferenceLocation = x.Media.Folder.MediaFoldersDetails.Where(x => x.CultureId == cultureId).Select(i => i.Title).FirstOrDefault()
                } : new AttachmentDto(),
                Status = x.Status,
                CurrentStage = new BasicStageInfoDto
                {
                    Id = x.CurrentStageId,
                    Order = x.CurrentStage.Order,
                    Description = x.CurrentStage.StageDetails.Where(x => x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                    StageDetails = x.CurrentStage.StageDetails.Select(y => new StageCultureDetailsDto { CultureId = y.CultureId, Description = y.Description, CultureCode = y.Culture.CultureId }).ToList()
                },
                SubmittedForm = x.SubmittedFormId.HasValue ? new SubmittedFormDto
                {
                    Id = x.SubmittedFormId.Value,
                    Subject = x.SubmittedForm.Subject,
                    CreatedBy = x.CreatedBy.FullName,
                    CreationDate = x.SubmittedForm.CreationDate,
                    SubmittedValue = x.SubmittedForm.SubmittedValue,
                    Attachments = x.SubmittedForm.Medias.Select(y => new SubmittedFormMediaVM { Id = y.MediaId, ContentType = y.Media.ContentType, PublicUrl = y.Media.PublicUrl,Filename=y.Media.Filename,
                        ReferenceLocation = y.Media.Folder.MediaFoldersDetails.Where(x => x.CultureId == cultureId).Select(i => i.Title).FirstOrDefault()
                    }).ToList()
                } : new SubmittedFormDto(),

            }).PaginateAsync(param.Pagination.Number, param.Pagination.Start);



            return result;

        }

        public async Task<PaginationResult<WorkFlowItemList>> GetAllWorkItemPerUser(SmartTableParam param, long workFlowId)
        {

            var siteId = _workContext.GetCurrentSiteId();

            var userId = await _workContext.GetCurrentUserId();

            var cultureId = _workContext.GetCurrentCultureId();

            var query = _workFlowItemRepository.Query()
                                               .Where(x => x.CurrentStage.WorkFlowId == workFlowId && x.CreatedById == userId)
                                               .Include(x => x.CurrentStage)
                                               .ThenInclude(x => x.StageDetails)
                                               .Include("CurrentStage.StageDetails.Culture")
                                               .Include(x => x.CurrentStage)
                                               .ThenInclude(x => x.WorkFlow)
                                               .ThenInclude(x => x.WorkFlowDetails)
                                               .Include("CurrentStage.WorkFlow.WorkFlowDetails.Culture")
                                               .Include(x => x.CreatedBy)
                                               .Include(x => x.SubmittedForm)
                                               .ThenInclude(x => x.Medias)
                                               .ThenInclude(x=>x.Media)
                                               .ThenInclude(x=>x.Folder)
                                               .ThenInclude(x=>x.MediaFoldersDetails)
                                               .Include(x => x.Media)
                                               .ThenInclude(x => x.Folder)
                                               .ThenInclude(x => x.MediaFoldersDetails)
                                               .AsNoTracking();


            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;

                if (search.WorkFlowId != null)
                {
                    long WorkFlowId = (long)search.WorkFlowId;
                    query = query.Where(x => x.CurrentStage.WorkFlowId == WorkFlowId);
                }

                if (search.StageId != null)
                {
                    long StageId = (long)search.StageId;
                    query = query.Where(x => x.CurrentStageId == StageId);
                }

                if (search.Status != null)
                {
                    var status = (EFlowStatus)search.Status;
                    query = query.Where(x => x.Status == status);
                }


                if (search.From != null)
                {
                    DateTimeOffset From = search.From;
                    query = query.Where(x => x.CreationDate >= From);
                }


                if (search.To != null)
                {
                    DateTimeOffset To = search.To;
                    query = query.Where(x => x.CreationDate <= To);
                }

                if (search.Code != null)
                {
                    string Code = search.Code;
                    query = query.Where(x => x.SubmittedForm.Code.Contains(Code));
                }

                if (search.CreatedBy != null)
                {
                    string CreatedBy = search.CreatedBy;
                    query = query.Where(x => x.CreatedBy.FullName.Contains(CreatedBy));
                }


                if (search.Search != null)
                {
                    string Search = search.Search;
                    query = query.Where(x => x.CreatedBy.FullName.ToUpper().Contains(Search.ToUpper()) ||( x.MediaId.HasValue ? x.Media.Filename.ToUpper().Contains(Search.ToUpper()) : true) );
                }


            }

            if (!query.Any())
            {
                return new PaginationResult<WorkFlowItemList>(new List<WorkFlowItemList>(), 0, 0);
            }



            var result = await query.OrderByDescending(x => x.CreationDate).Select(x => new WorkFlowItemList
            {
                Id = x.Id,
                Code = x.SubmittedFormId.HasValue ? x.SubmittedForm.Code : "",
                Type = x.CurrentStage.WorkFlow.Type,
                CreatedBy = x.CreatedBy.FullName,
                CreationDate = x.CreationDate,
                WorkFlowName = x.CurrentStage.WorkFlow.WorkFlowDetails.Where(x => x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                Media = x.MediaId.HasValue ? new AttachmentDto { Id = x.MediaId.Value, PublicUrl = x.Media.PublicUrl,
                    ContentType = x.Media.ContentType ,FileName=x.Media.Filename,
                    ReferenceLocation=x.Media.Folder.MediaFoldersDetails.Where(x => x.CultureId == cultureId).Select(i=>i.Title).FirstOrDefault()

                } : new AttachmentDto(),
                Status = x.Status,
                WorkFlowDetails = x.CurrentStage.WorkFlow.WorkFlowDetails.Select(y => new WorkFlowCutlureDetailsDto { CultureId = y.CultureId, CutlureCode = y.Culture.CultureId, Name = y.Name }).ToList(),
                CurrentStage = new BasicStageInfoDto
                {
                    Order = x.CurrentStage.Order,
                    Description = x.CurrentStage.StageDetails.Where(x => x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                    StageDetails = x.CurrentStage.StageDetails.Select(y => new StageCultureDetailsDto { CultureId = y.CultureId, Description = y.Description, CultureCode = y.Culture.CultureId }).ToList()
                },
                SubmittedForm = x.SubmittedFormId.HasValue ? new SubmittedFormDto
                {
                    Id = x.SubmittedFormId.Value,
                    Subject = x.SubmittedForm.Subject,
                    CreatedBy = x.CreatedBy.FullName,
                    CreationDate = x.SubmittedForm.CreationDate,
                    SubmittedValue = x.SubmittedForm.SubmittedValue,
                    Attachments = x.SubmittedForm.Medias.Select(y => new SubmittedFormMediaVM { Id = y.MediaId, ContentType = y.Media.ContentType, PublicUrl = y.Media.PublicUrl,
                        ReferenceLocation = x.Media.Folder.MediaFoldersDetails.Where(x => x.CultureId == cultureId).Select(i => i.Title).FirstOrDefault()
                    }).ToList()
                } : new SubmittedFormDto(),

            }).PaginateAsync(param.Pagination.Number, param.Pagination.Start);


            return result;

        }

        //
        public async Task<PaginationResult<WorkFlowItemDetailsDto>> GetAllWorkItemForWeb(SmartTableParam param)
        {
            var cultureId =  _workContext.GetCurrentCultureId();

            var siteId = _workContext.GetCurrentSiteId();

            var query = _workFlowItemRepository.Query()
                                                    .Where(x => x.CurrentStage.WorkFlow.SiteId == siteId)
                                                    .Include(x => x.CreatedBy)
                                                    .Include(x => x.CurrentStage)
                                                    .ThenInclude(x => x.StageDetails)
                                                    .Include("CurrentStage.StageDetails.Culture")
                                                    .Include(x => x.Media)
                                                    .Include(x => x.SubmittedForm)
                                                    .ThenInclude(x => x.Medias)
                                                    .ThenInclude(x => x.Media)
                                                    .Include(x => x.ItemHistories)
                                                    .Include("ItemHistories.CreatedBy")
                                                    .Include("ItemHistories.CurrentStage")
                                                    .Include("ItemHistories.CurrentStage.StageDetails")
                                                    .Include("ItemHistories.CurrentStage.StageDetails.Culture")
                                                    .AsNoTracking();


            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;



                if (search.Status != null)
                {
                    var status = (EFlowStatus)search.Status;
                    query = query.Where(x => x.Status == status);
                }

                if (search.From != null)
                {
                    DateTimeOffset From = search.From;
                    query = query.Where(x => x.CreationDate >= From);
                }


                if (search.To != null)
                {
                    DateTimeOffset To = search.To;
                    query = query.Where(x => x.CreationDate <= To);
                }

                if (search.Code != null)
                {
                    string Code = search.Code;
                    query = query.Where(x => x.SubmittedForm.Code.Contains(Code));
                }
                if (search.CreatedBy != null)
                {
                    string CreatedBy = search.CreatedBy;
                    query = query.Where(x => x.CreatedBy.FullName.Contains(CreatedBy));
                }


            }

            if (!query.Any())
            {
                return new PaginationResult<WorkFlowItemDetailsDto>(new List<WorkFlowItemDetailsDto>(), 0, 0);
            }

            //var result = await query.OrderByDescending(x => x.CreationDate).ProjectTo<WorkFlowItemDetailsDto>(_mapper.ConfigurationProvider)
            //    .PaginateAsync(param.Pagination.Number, param.Pagination.Start);
            
            var result = await query.OrderByDescending(x => x.CreationDate).Select(workflowItem => new WorkFlowItemDetailsDto
            {
                Id = workflowItem.Id,
                Attachment = workflowItem.MediaId.HasValue ? new AttachmentDto { Id = workflowItem.MediaId.Value, ContentType = workflowItem.Media.ContentType, PublicUrl = workflowItem.Media.PublicUrl, FileName = workflowItem.Media.Filename } : null,
                Code = workflowItem.SubmittedFormId.HasValue ? workflowItem.SubmittedForm.Code : null,
                CreatedBy = workflowItem.CreatedBy.FullName,
                CreationDate = workflowItem.CreationDate,
                Status = workflowItem.Status,
                CurrentStage = new BasicStageInfoDto
                {
                    Id = workflowItem.CurrentStageId,
                    Order = workflowItem.CurrentStage.Order,
                    Description = workflowItem.CurrentStage.StageDetails.Where(x => x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                    StageDetails = workflowItem.CurrentStage.StageDetails.Select(y => new StageCultureDetailsDto { Description = y.Description, CultureCode = y.Culture.CultureId, CultureId = y.CultureId }).ToList()
                },

                SubmittedForm = workflowItem.SubmittedFormId.HasValue ? new SubmittedFormDto
                {
                    SubmittedValue = workflowItem.SubmittedForm.SubmittedValue,
                    CreatedBy = workflowItem.CreatedBy.FullName,
                    CreationDate = workflowItem.SubmittedForm.CreationDate,
                    Id = workflowItem.SubmittedFormId.Value,
                    Subject = workflowItem.SubmittedForm.Subject,
                    Attachments = workflowItem.SubmittedForm.Medias.Select(y => new SubmittedFormMediaVM
                    {
                        Id = y.MediaId,
                        ContentType = y.Media.ContentType,
                        PublicUrl = y.Media.PublicUrl
                    }).ToList()
                } : new SubmittedFormDto(),
                ItemHistories = workflowItem.ItemHistories.OrderBy(x => x.CreationDate)
                                                          .Select(x => new ItemHistoryDto
                                                          {
                                                              ActionType = x.ActionType,
                                                              CreatedBy = x.CreatedBy.FullName,
                                                              CreationDate = x.CreationDate,
                                                              Note = x.Note,
                                                              Stage = x.CurrentStageId.HasValue ? new BasicStageInfoDto
                                                              {
                                                                  Id = x.CurrentStageId.Value,
                                                                  Description = x.CurrentStage.StageDetails.Where(x => x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                                                                  Order = x.CurrentStage.Order,
                                                                  StageDetails = x.CurrentStage.StageDetails.Select(y => new StageCultureDetailsDto { Description = y.Description, CultureCode = y.Culture.CultureId, CultureId = y.CultureId }).ToList()
                                                              } : new BasicStageInfoDto(),
                                                          }).ToList(),


            }).PaginateAsync(param.Pagination.Number, param.Pagination.Start);

            return result;

        }

        public async Task<WorkFlowItemDetailsDto> WorkFlowItemDetails(long workflowItemId)
        {

            var siteId = _workContext.GetCurrentSiteId();

            var cultureId = _workContext.GetCurrentCultureId();

            var workflowItem = await _workFlowItemRepository.Query()
                                                    .Where(x => x.Id == workflowItemId)
                                                    .Include(x => x.CreatedBy)
                                                    .Include(x => x.CurrentStage)
                                                    .ThenInclude(x => x.StageDetails)
                                                    .Include("CurrentStage.StageDetails.Culture")
                                                    .Include(x => x.Media)
                                                    .ThenInclude(x => x.Folder)
                                                    .ThenInclude(x => x.MediaFoldersDetails)
                                                    .Include(x => x.SubmittedForm)
                                                    .ThenInclude(x => x.Medias)
                                                    .ThenInclude(x => x.Media)
                                                    .ThenInclude(x => x.Folder)
                                                    .ThenInclude(x => x.MediaFoldersDetails)
                                                    .Include(x => x.ItemHistories)
                                                    .Include("ItemHistories.CreatedBy")
                                                    .Include("ItemHistories.CurrentStage")
                                                    .Include("ItemHistories.CurrentStage.StageDetails")
                                                    .Include("ItemHistories.CurrentStage.StageDetails.Culture")
                                                    .AsNoTracking()
                                                    .FirstOrDefaultAsync();

            if (workflowItem is null)
            {
                throw new Exception("WorkFlow item doesn't exist");
            }


            var result = new WorkFlowItemDetailsDto
            {
                Id = workflowItem.Id,
                Attachment = workflowItem.MediaId.HasValue ? new AttachmentDto { Id = workflowItem.MediaId.Value, ContentType = workflowItem.Media.ContentType, PublicUrl = workflowItem.Media.PublicUrl,FileName=workflowItem.Media.Filename,
                    ReferenceLocation = workflowItem.Media.Folder.MediaFoldersDetails.Where(x => x.CultureId == cultureId).Select(i => i.Title).FirstOrDefault()
                } : null,
                Code = workflowItem.SubmittedFormId.HasValue ? workflowItem.SubmittedForm.Code : null,
                CreatedBy = workflowItem.CreatedBy.FullName,
                CreationDate = workflowItem.CreationDate,
                Status = workflowItem.Status,
                CurrentStage = new BasicStageInfoDto
                {
                    Id= workflowItem.CurrentStageId,
                    Order = workflowItem.CurrentStage.Order,
                    Description = workflowItem.CurrentStage.StageDetails.Where(x => x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                    StageDetails = workflowItem.CurrentStage.StageDetails.Select(y => new StageCultureDetailsDto { Description = y.Description, CultureCode = y.Culture.CultureId, CultureId = y.CultureId }).ToList()
                },

                SubmittedForm = workflowItem.SubmittedFormId.HasValue ? new SubmittedFormDto
                {
                    SubmittedValue = workflowItem.SubmittedForm.SubmittedValue,
                    CreatedBy = workflowItem.CreatedBy.FullName,
                    CreationDate = workflowItem.SubmittedForm.CreationDate,
                    Id = workflowItem.SubmittedFormId.Value,
                    Subject = workflowItem.SubmittedForm.Subject,
                    Attachments = workflowItem.SubmittedForm.Medias.Select(y => new SubmittedFormMediaVM
                    {
                        Id = y.MediaId,
                        ContentType = y.Media.ContentType,
                        PublicUrl = y.Media.PublicUrl,
                       ReferenceLocation = y.Media.Folder.MediaFoldersDetails.Where(x => x.CultureId == cultureId).Select(i => i.Title).FirstOrDefault()

                    }).ToList()
                } : new SubmittedFormDto(),
                ItemHistories = workflowItem.ItemHistories.OrderBy(x => x.CreationDate)
                                                          .Select(x => new ItemHistoryDto
                                                          {
                                                              ActionType = x.ActionType,
                                                              CreatedBy = x.CreatedBy.FullName,
                                                              CreationDate = x.CreationDate,
                                                              Note = x.Note,
                                                              Stage = x.CurrentStageId.HasValue ?  new BasicStageInfoDto
                                                              {
                                                                  Id=x.CurrentStageId.Value,
                                                                  Description = x.CurrentStage.StageDetails.Where(x => x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                                                                  Order = x.CurrentStage.Order,
                                                                  StageDetails = x.CurrentStage.StageDetails.Select(y => new StageCultureDetailsDto { Description = y.Description, CultureCode = y.Culture.CultureId, CultureId = y.CultureId }).ToList()
                                                              } : new BasicStageInfoDto(),
                                                          }).ToList(),


            };



            return result;

            

        }

        #region Helpers

        public async Task<bool> SendAdminNotfication(List<string> adminEmails, WorkFlowItem workflowItem)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var _workContext = scope.ServiceProvider.GetRequiredService<IWorkContext>();
                var _siteSettings = scope.ServiceProvider.GetRequiredService<ISiteSettingsService>();
                var _testNotifier = scope.ServiceProvider.GetRequiredService<ITestNotifier>();

                long siteId = _workContext.GetCurrentSiteId();

                var settings = await _siteSettings.GetSiteSettingsByModule("WorkFlowTemplates", siteId);
                string WorkFlowItemTemplateId = null;
                if (settings.Count > 0)
                {

                    

                    WorkFlowItemTemplateId = settings.FirstOrDefault(x => x.Id == "AdminWorkFlowItemTemplateId")?.Value;



                }
                else
                {
                    return false;
                }



                var Parameters = new List<EmailParamsVM>();
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[WorkFlowItemId]",
                    Value = workflowItem.Id.ToString(),
                });

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[WorkFlowName]",
                    Value = workflowItem.CurrentStage.WorkFlow.WorkFlowDetails.Where(x => x.Culture.CultureId.Contains("en")).FirstOrDefault()?.Name ?? "",
                });

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[FullName]",
                    Value = workflowItem.CreatedBy.FullName,
                });



                NotificationObject notificationObject = new NotificationObject
                {
                    MessageBody = string.Empty,
                    MessageTitle = " New Pending Workflow Item",
                    SendVia = SendBy.Email,
                    SendValue = adminEmails,
                    NotificationType = NotificationType.Email,
                    EmailOptions = new EmailSenderVM
                    {
                        //Emails = adminEmails,
                        TemplateId = WorkFlowItemTemplateId,
                        Params = Parameters
                    },
                    EmailTemplateId = WorkFlowItemTemplateId,

                };

                if (!string.IsNullOrEmpty(WorkFlowItemTemplateId))
                {
                    await _testNotifier.SendMessageAsync(NotificationType.Email, null, notificationObject, siteId);

                }



            }

            return true;

        }

        public async Task<bool> WorkflowItemNotfication(long userId, WorkFlowItem workflowItem, EFlowStatus status)
        {

            using (var scope = _serviceScopeFactory.CreateScope())
            {

                var _workContext = scope.ServiceProvider.GetRequiredService<IWorkContext>();

                var siteId = _workContext.GetCurrentSiteId();

                var _siteSettings = scope.ServiceProvider.GetRequiredService<ISiteSettingsService>();
                var _localizationServices = scope.ServiceProvider.GetRequiredService<ILocalizationServices>();
                var _userSitesRepository = scope.ServiceProvider.GetRequiredService<IRepository<UsersSites>>();


                var user = _userSitesRepository.Query()
                                               .Where(x => x.UserId == userId && x.SiteId == siteId)
                                               .Include(x => x.User)
                                               .FirstOrDefault();


                if (!user.CultureId.HasValue)
                    return false;



                var settings = await _siteSettings.GetSiteSettingsByModule("WorkFlowTemplates", siteId);
                string WorkFlowItemTemplateId = string.Empty;
                if (settings.Count > 0)
                {

                    string userWorkFlowItemTemplate = $"UserWorkFlowItemTemplateId_{user.CultureId}";


                    WorkFlowItemTemplateId = settings.FirstOrDefault(x => x.Id.ToLower().Contains(userWorkFlowItemTemplate.ToLower()))?.Value ?? string.Empty;

                    if (string.IsNullOrEmpty(WorkFlowItemTemplateId))
                        return false;


                }
                else
                {
                    return false;
                }

                var allKeys = await _localizationServices.GetLocalization(string.Empty, siteId,user.CultureId.Value);

                var defaultStatus = allKeys.Where(x=>x.Key == "SharedResources").FirstOrDefault().Value.Where(x=>x.Key == status.ToString()).FirstOrDefault().Value;




                var Parameters = new List<EmailParamsVM>();
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[WorkFlowItemId]",
                    Value = workflowItem.Id.ToString(),
                });

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[WorkFlowName]",
                    Value = workflowItem.CurrentStage.WorkFlow.WorkFlowDetails.Where(X => X.CultureId == user.CultureId).FirstOrDefault()?.Name ?? "",
                });

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[Status]",
                    Value = defaultStatus ?? "",
                });



                NotificationObject notificationObject = new NotificationObject
                {
                    MessageBody = string.Empty,
                    MessageTitle = "Workflow Item",
                    SendVia = SendBy.Email,
                    SendValue = new List<string>() { user.User.Email },
                    NotificationType = NotificationType.Email,
                    SMSProvider = SMSProviders.SMSUniFonic,
                    EmailOptions = new EmailSenderVM
                    {
                        Emails = new List<string>() ,
                        TemplateId = WorkFlowItemTemplateId,
                        Params = Parameters
                    },
                    EmailTemplateId = WorkFlowItemTemplateId,

                };


                var _testNotifier = scope.ServiceProvider.GetRequiredService<ITestNotifier>();


                if (!string.IsNullOrEmpty(WorkFlowItemTemplateId))
                {

                    await _testNotifier.SendMessageAsync(NotificationType.Email, null, notificationObject, siteId);

                }




                return true;


            }



        }
        //public async Task<bool> WorkflowItemStatusNotfication(long userId, long workflowItemId, EFlowStatus status)
        //{
        //    long siteId = _workContext.GetCurrentSiteId();
        //    var user = _userSitesRepository.Query()
        //                                          .Where(x => x.UserId == userId)
        //                                          .FirstOrDefault();
        //    var settings = await _siteSettings.GetSiteSettingsByModule("WorkFlowTemplates", siteId);
        //    string WorkFlowItemTemplateId = null;
        //    if (settings.Count > 0)
        //    {


        //        WorkFlowItemTemplateId = settings.FirstOrDefault(x => x.Id == "AdminWorkFlowItemTemplateId")?.Value;


        //    }
        //    else
        //    {
        //        return false;
        //    }



        //var Parameters = new List<EmailParamsVM>();
        //Parameters.Add(new EmailParamsVM
        //{
        //    Param = "[WorkFlowId]",
        //    Value = workflowItemId.ToString(),
        //});



        //NotificationObject notificationObject = new NotificationObject
        //{
        //    MessageBody = string.Empty,
        //    MessageTitle = "new workflow item",
        //    SendVia = SendBy.Email,
        //    SendValue = new List<string>(),
        //    NotificationType = NotificationType.Email,
        //    SMSProvider = SMSProviders.SMSUniFonic,
        //    EmailOptions = new EmailSenderVM
        //    {
        //        Emails = adminEmails,
        //        TemplateId = WorkFlowItemTemplateId,
        //        Params = Parameters
        //    },
        //    EmailTemplateId = "",

        //};





        //await _testNotifier.SendMessageAsync(NotificationType.Email, null, notificationObject, siteId);


        //    return true;

        //}


        public async Task<WorkFlowItemDetailsDto> GetLastWorkFlowItem()
        {

            var siteId = _workContext.GetCurrentSiteId();

            var cultureId =  _workContext.GetCurrentCultureId();

            var userId = await _workContext.GetCurrentUserId();
            var workflowItem = _workFlowItemRepository.Query()
                                                    .Where(x => x.CreatedById == userId && x.CurrentStage.WorkFlow.SiteId == siteId && x.Status == EFlowStatus.InProgress)
                                                    .Include(x => x.CreatedBy)
                                                    .Include(x => x.CurrentStage)
                                                    .ThenInclude(x => x.StageDetails)
                                                    .Include("CurrentStage.StageDetails.Culture")
                                                    .Include(x => x.Media)
                                                    .Include(x => x.SubmittedForm)
                                                    .ThenInclude(x => x.Medias)
                                                    .ThenInclude(x => x.Media)
                                                    .Include(x => x.ItemHistories)
                                                    .Include("ItemHistories.CreatedBy")
                                                    .Include("ItemHistories.CurrentStage")
                                                    .Include("ItemHistories.CurrentStage.StageDetails")
                                                    .Include("ItemHistories.CurrentStage.StageDetails.Culture")
                                                    .AsNoTracking()
                                                    .OrderByDescending(x => x.CreationDate)
                                                    .FirstOrDefault();


            var result = new WorkFlowItemDetailsDto
            {
                Id = workflowItem.Id,
                Attachment = workflowItem.MediaId.HasValue ? new AttachmentDto { Id = workflowItem.MediaId.Value, ContentType = workflowItem.Media.ContentType, PublicUrl = workflowItem.Media.PublicUrl, FileName = workflowItem.Media.Filename } : null,
                Code = workflowItem.SubmittedFormId.HasValue ? workflowItem.SubmittedForm.Code : null,
                CreatedBy = workflowItem.CreatedBy.FullName,
                CreationDate = workflowItem.CreationDate,
                Status = workflowItem.Status,
                CurrentStage = new BasicStageInfoDto
                {
                    Id = workflowItem.CurrentStageId,
                    Order = workflowItem.CurrentStage.Order,
                    Description = workflowItem.CurrentStage.StageDetails.Where(x => x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                    StageDetails = workflowItem.CurrentStage.StageDetails.Select(y => new StageCultureDetailsDto { Description = y.Description, CultureCode = y.Culture.CultureId, CultureId = y.CultureId }).ToList()
                },

                SubmittedForm = workflowItem.SubmittedFormId.HasValue ? new SubmittedFormDto
                {
                    SubmittedValue = workflowItem.SubmittedForm.SubmittedValue,
                    CreatedBy = workflowItem.CreatedBy.FullName,
                    CreationDate = workflowItem.SubmittedForm.CreationDate,
                    Id = workflowItem.SubmittedFormId.Value,
                    Subject = workflowItem.SubmittedForm.Subject,
                    Attachments = workflowItem.SubmittedForm.Medias.Select(y => new SubmittedFormMediaVM
                    {
                        Id = y.MediaId,
                        ContentType = y.Media.ContentType,
                        PublicUrl = y.Media.PublicUrl
                    }).ToList()
                } : new SubmittedFormDto(),
                ItemHistories = workflowItem.ItemHistories.OrderBy(x => x.CreationDate)
                                                          .Select(x => new ItemHistoryDto
                                                          {
                                                              ActionType = x.ActionType,
                                                              CreatedBy = x.CreatedBy.FullName,
                                                              CreationDate = x.CreationDate,
                                                              Note = x.Note,
                                                              Stage = x.CurrentStageId.HasValue ? new BasicStageInfoDto
                                                              {
                                                                  Id = x.CurrentStageId.Value,
                                                                  Description = x.CurrentStage.StageDetails.Where(x => x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                                                                  Order = x.CurrentStage.Order,
                                                                  StageDetails = x.CurrentStage.StageDetails.Select(y => new StageCultureDetailsDto { Description = y.Description, CultureCode = y.Culture.CultureId, CultureId = y.CultureId }).ToList()
                                                              } : new BasicStageInfoDto(),
                                                          }).ToList(),


            };


            return result;

        }


        #endregion


    }

}
