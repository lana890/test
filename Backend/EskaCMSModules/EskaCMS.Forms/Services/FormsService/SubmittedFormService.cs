using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.DataAccess.Data;
using EskaCMS.Entities.Entity.Forms;
using EskaCMS.Entities.Entity.WorkFlow;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.DbFactory;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Media.Models;
using EskaCMS.Media.Services.Interfaces;
using EskaCMS.WorkFlow.Models.Request;
using EskaCMS.WorkFlow.Models.Response;
using EskaCMS.WorkFlow.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Forms.Enums.SubmittedFormStatus;
using EskaCMS.Forms.Services.FormsService.Interfaces;
using EskaCMS.Forms.Model.FormsViewModel;

namespace EskaCMS.Forms.Services
{
    public class SubmittedFormService : ISubmittedFormService
    {
        private readonly IRepository<SubmittedForm> _submittedFormRepository;
        private readonly IRepository<WorkFlowSetup> _workFlowSetupRepository;
        private readonly IRepository<SubmittedFormMedia> _submittedFormMediaRepository;
        private readonly IWorkContext _workContext;
        private readonly IWorkFlowItemService _workFlowItemService;
        private readonly IRepository<WorkFlowItem> _workFlowItemRepository;
        private readonly IMediaService _mediaService;
        private readonly EskaDCMSDbContext context;
        private readonly IRepository<EskaCMS.Entities.Entity.Core.UserOptions> _userOptionsRepository;

        public SubmittedFormService(IRepository<SubmittedForm> submittedFormRepository,
                                    IWorkContext workContext,
                                    IWorkFlowItemService workFlowItemService,
                                    IRepository<WorkFlowItem> workFlowItemRepository,
                                   IRepository<WorkFlowSetup> workFlowSetupRepository,
                                   IMediaService mediaService,
                                              EskaDCMSDbContext context,
                                               IRepository<EskaCMS.Entities.Entity.Core.UserOptions> userOptionsRepository,
                                   IRepository<SubmittedFormMedia> submittedFormMediaRepository
                                   )
        {
            _submittedFormRepository = submittedFormRepository;
            _workContext = workContext;
            _workFlowItemService = workFlowItemService;
            _workFlowItemRepository=workFlowItemRepository;
            _workFlowSetupRepository = workFlowSetupRepository;
            _mediaService = mediaService;
            this.context = context;
            _submittedFormMediaRepository = submittedFormMediaRepository;
            _userOptionsRepository = userOptionsRepository;

        }


        public async Task<SubmittedFormResponseVM> SubmitForm(SubmittedFormVM model)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();
            bool IsEditMood = model.Id != 0;

            var submittedForm = IsEditMood
                ? await _submittedFormRepository
                    .Query()
                    .Where(x => x.Id == model.Id && x.SiteId == siteId)
                    .Include(x => x.WorkFlowItem)
                    .Include(x => x.Medias)
                    .FirstOrDefaultAsync()
                : new SubmittedForm();


            submittedForm.SubmittedValue = model.SumbittedValue;
            submittedForm.Subject = model.Subject;

            if (!IsEditMood)
            {

                submittedForm.Code = model.Code;
                submittedForm.CreatedById = userId;
                submittedForm.CreationDate = DateTimeOffset.Now;
                submittedForm.SiteId = siteId;
                submittedForm.FormId = (model.FormId==null ||model.FormId==0)?null: model.FormId;
               
                _submittedFormRepository.Add(submittedForm);
            }
             
            else if (IsEditMood)
            {
                await HandleEditMoodAsync(submittedForm, userId, model);

            }
           

            await _submittedFormRepository.SaveChangesAsync();

            if (!IsEditMood && model.WorkFlowId.HasValue)
            {

                await CreateWorkflowItemAsync(submittedForm.Id, userId, model.WorkFlowId.Value);
            }
            

            if (model.Attachments!=null)
            {
              
               await HandleFormAttachments(model.Attachments, submittedForm.Id, userId, siteId , model.FolderId);
            
            }


            var workflowItemId = await  GetWorkFlowItemId(submittedForm.Id, siteId);

            var response = new SubmittedFormResponseVM
            {
                ItemId = workflowItemId,
                Message =  "Your form has been submitted",
            };

            return response;
        }

        public async Task< Dictionary<string,List< ExpandoObject>>> GetStatistics(StatisticsVM model)
        {
            try
            {
                var Result = new Dictionary<string,List< ExpandoObject>>();

                await ValidationInputs(model);

                foreach (var keyvalue in model.KeyValue)
                {
                    var connection = context.Database.GetDbConnection();
                    var shouldCloseConnection = connection.State != ConnectionState.Open;

                    using (var command = connection.CreateCommand())
                    {
                        DbProviderHelper.ConfigureCommand(command);
                        var ConditionParamKey = command.CreateParameter();
                        ConditionParamKey.Value = $"$." + keyvalue.KeySearch;
                        ConditionParamKey.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@ConditonsearchKey");
                        ConditionParamKey.DbType = DbType.String;
                        ConditionParamKey.Direction = ParameterDirection.Input;

                        var ConditionparamValue = command.CreateParameter();
                        ConditionparamValue.Value =keyvalue.ValueSearch;
                        ConditionparamValue.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@ConditonsearchValue");
                        ConditionparamValue.DbType = DbType.String;
                        ConditionparamValue.Direction = ParameterDirection.Input;

                        var paramKey = command.CreateParameter();
                        paramKey.Value =$"$." + keyvalue.Key;
                        paramKey.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@searchKey");
                        paramKey.DbType = DbType.String;
                        paramKey.Direction = ParameterDirection.Input;




                        var paramValue = command.CreateParameter();
                        paramValue.Value = keyvalue.Value;
                        paramValue.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@searchValue");
                        paramValue.DbType = DbType.String;
                        paramValue.Direction = ParameterDirection.Input;

                       var GroupBy = command.CreateParameter();
                        GroupBy.Value =(long) model.GroupBy;
                        GroupBy.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@GroupCondition");
                        GroupBy.DbType = DbType.Int64;
                        GroupBy.Direction = ParameterDirection.Input;


                        var paramComponentSettingsId = command.CreateParameter();
                        paramComponentSettingsId.Value = model.ComponentsettingsId;
                        paramComponentSettingsId.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@ComponentsettingsId");
                        paramComponentSettingsId.DbType = DbType.String;
                        paramComponentSettingsId.Direction = ParameterDirection.Input;

                        var UserId = command.CreateParameter();
                        UserId.Value = model.UserId;
                        UserId.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@UserId");
                        UserId.DbType = DbType.UInt64;
                        UserId.Direction = ParameterDirection.Input;

                        var PageId = command.CreateParameter();
                        PageId.Value = model.PageId;
                        PageId.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@PageId");
                        PageId.DbType = DbType.Int64;
                        PageId.Direction = ParameterDirection.Input;



                        var paramCompersionOpertaors = command.CreateParameter();
                        paramCompersionOpertaors.Value = (long)keyvalue.ComparisonOperators;
                        paramCompersionOpertaors.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@comparison_operators");
                        paramCompersionOpertaors.DbType = DbType.Int64;
                        paramCompersionOpertaors.Direction = ParameterDirection.Input;

                        var ConditionsCompersionOpertaors = command.CreateParameter();
                        ConditionsCompersionOpertaors.Value = (long)keyvalue.ComparisonOperators;
                        ConditionsCompersionOpertaors.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@Conditions_comparison_operators");
                        ConditionsCompersionOpertaors.DbType = DbType.Int64;
                        ConditionsCompersionOpertaors.Direction = ParameterDirection.Input;


                        var paramTimeIntervals = command.CreateParameter();
                        paramTimeIntervals.Value = (long)model.Timeinterval;
                        paramTimeIntervals.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@Time_intervals");
                        paramTimeIntervals.DbType = DbType.Int64;
                        paramTimeIntervals.Direction = ParameterDirection.Input;


                        var paramTypeMathematics = command.CreateParameter();
                        paramTypeMathematics.Value = (long)model.TypeMathematics;
                        paramTypeMathematics.ParameterName = DbProviderHelper.NormalizeParameterName(command, "@Type_mathematics");
                        paramTypeMathematics.DbType = DbType.Int64;
                        paramTypeMathematics.Direction = ParameterDirection.Input;



                        command.Parameters.Add(ConditionparamValue);
                        command.Parameters.Add(ConditionParamKey);
                        command.Parameters.Add(paramKey);
                        command.Parameters.Add(paramValue);
                        command.Parameters.Add(paramCompersionOpertaors);
                        command.Parameters.Add(ConditionsCompersionOpertaors);

                        command.Parameters.Add(paramTimeIntervals);

                        command.Parameters.Add(paramTypeMathematics);
                        command.Parameters.Add(paramComponentSettingsId);
                        command.Parameters.Add(PageId);
                        command.Parameters.Add(GroupBy);
                        command.Parameters.Add(UserId);
                        command.CommandText = "GetDataByKeyValue";
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            if (shouldCloseConnection)
                            {
                                await connection.OpenAsync();
                            }

                            using (var result = await command.ExecuteReaderAsync())
                            {
                                if (result.HasRows)
                                {
                                    var dataForCurrentKey = new List<ExpandoObject>();


                                    while (await result.ReadAsync())
                                    {
                                        var expandoObject = new ExpandoObject();
                                        var expandoDict = (IDictionary<string, object>)expandoObject;
                                        for (int i = 0; i < result.FieldCount; i++)
                                        {
                                            expandoDict[result.GetName(i)] = result[i];


                                        }
                                        dataForCurrentKey.Add(expandoObject);

                                    }
                                    Result.Add(keyvalue.Key, dataForCurrentKey);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);

                        }
                        finally
                        {
                            if (shouldCloseConnection)
                            {
                                await context.Database.CloseConnectionAsync();
                            }
                        }

                    }
                }

                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        //its needed for integration (billing)
        public async Task<PaginationResult<SubmittedFormDetailsVM>> AllSubmittedByStatusOrCode(SubmittedFormByCodeVM model)
        {
            var userId = model.UserOptions != null ? await GetUserIdBasedOnOptions(model.UserOptions) : await _workContext.GetCurrentUserId();



            var siteId = _workContext.GetCurrentSiteId();

            var query = _submittedFormRepository.Query()
                                                    .Where(x =>  x.SiteId == siteId && x.CreatedById == userId)
                                                    .Include(x => x.WorkFlowItem)
                                                    .Include(x => x.WorkFlowItem)
                                                    .ThenInclude(x => x.ItemHistories)
                                                    .Include(x => x.WorkFlowItem)
                                                    .ThenInclude(x => x.CreatedBy)
                                                    .Include(x => x.Medias)
                                                    .ThenInclude(x => x.Media)
                                                    .AsNoTracking();




            if (!string.IsNullOrEmpty(model.Code))
            {
                query = query.Where(x => x.Code.ToLower() == model.Code.ToLower());
            }

            if (model.Status != null)
            {
                
                query = query.Where(x =>  (model.Status != ESubmittedFormStatus.Draft ? x.WorkFlowItem != null : true));
            }


        

            if (!query.Any())
            {
                return new PaginationResult<SubmittedFormDetailsVM>(new List<SubmittedFormDetailsVM>(), 0, 0);
            }


            var result = await query.OrderByDescending(x => x.CreationDate)
                                    .Select(x => new SubmittedFormDetailsVM
                                    {
                                        Id = x.Id,
                                        CreationDate = x.CreationDate,
                                        Subject = x.Subject,
                                        Code = x.Code,
                                        Attachments = !x.Medias.Any() ? new List<FormMedia>() : x.Medias.Select(x => new FormMedia { Id = x.Media.Id, ContentType = x.Media.ContentType, Filename = x.Media.Filename, PublicUrl = x.Media.PublicUrl, Size = x.Media.Size }).ToList(),
                                        SubmittedValue = x.SubmittedValue,
                                        Status = x.WorkFlowItem != null ? (ESubmittedFormStatus)x.WorkFlowItem.Status : ESubmittedFormStatus.Draft,
                                        WorkFlowItemDetails = x.WorkFlowItem != null ? new BasicItemDetails
                                        {
                                            CreatedBy = x.WorkFlowItem.CreatedBy.FullName,
                                            CreationDate = x.WorkFlowItem.CreationDate,
                                            Note = x.WorkFlowItem.ItemHistories.Any() ? x.WorkFlowItem.ItemHistories.OrderBy(x => x.CreationDate).LastOrDefault().Note : null,
                                        } : null
                                    })
                                    .PaginateAsync(model.PageSize.Value,model.PageIndex.Value);

            return result;


        }


        public async Task<SubmittedFormDetailsVM> GetSubmittedFormByCode(SubmittedFormByCodeVM model)
        {
            var userId = model.UserOptions !=null ? await GetUserIdBasedOnOptions(model.UserOptions) : await _workContext.GetCurrentUserId();

           

            var siteId = _workContext.GetCurrentSiteId();

            var query = _submittedFormRepository.Query().AsNoTracking()
                                                    .Where(x => x.Code.ToLower() == model.Code.ToLower() && x.SiteId == siteId && x.CreatedById == userId)
                                                    .Include(x => x.WorkFlowItem)
                                                    .Include(x => x.WorkFlowItem)
                                                    .ThenInclude(x => x.ItemHistories)
                                                    .Include(x => x.WorkFlowItem)
                                                    .ThenInclude(x => x.CreatedBy)
                                                    .Include(x=>x.Medias)
                                                    .ThenInclude(x=>x.Media)
                                                    .FirstOrDefault();
                                              




            

            if (query == null)
            {
                return new SubmittedFormDetailsVM();
            }

            var result =  new SubmittedFormDetailsVM
                                    {
                                        Id = query.Id,
                                        CreationDate = query.CreationDate,
                                        Subject = query.Subject,
                                        Code = query.Code,
                                        Attachments = !query.Medias.Any() ? new List<FormMedia>() : query.Medias.Select(x => new FormMedia { Id = x.Media.Id, ContentType = x.Media.ContentType, Filename = x.Media.Filename, PublicUrl = x.Media.PublicUrl, Size = x.Media.Size }).ToList(),
                                        SubmittedValue = query.SubmittedValue,
                                        Status = query.WorkFlowItem != null ? (ESubmittedFormStatus)query.WorkFlowItem.Status : ESubmittedFormStatus.Draft,
                                        WorkFlowItemDetails = query.WorkFlowItem != null ? new BasicItemDetails
                                        {
                                            CreatedBy = query.WorkFlowItem.CreatedBy.FullName,
                                            CreationDate = query.WorkFlowItem.CreationDate,
                                            Note = query.WorkFlowItem.ItemHistories.Any() ? query.WorkFlowItem.ItemHistories.OrderBy(x => x.CreationDate).LastOrDefault().Note : null,
                                        } : null
                                    };
                                    

            return result;


        }

        public async Task<PaginationResult<SubmittedFormDetailsVM>> GetAllSubmittedForm(SmartTableParam param)
        {
            var userId = await _workContext.GetCurrentUserId();

            var FromWeb = _workContext.GetCurrentPlatform() == EPlatformSource.Web ? true : false;

            var siteId = _workContext.GetCurrentSiteId();

            var query = _submittedFormRepository.Query()
                                                    .Where(x => x.SiteId == siteId)
                                                    .Include(x => x.WorkFlowItem)
                                                    .Include(x => x.WorkFlowItem)
                                                    .ThenInclude(x => x.ItemHistories)
                                                    .Include(x => x.WorkFlowItem)
                                                    .ThenInclude(x => x.CreatedBy)
                                                    .Include(x => x.Medias)
                                                    .ThenInclude(x => x.Media)
                                                    .AsNoTracking();




            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;


                if (search.From != null)
                {
                    DateTimeOffset From = search.From;
                    query = query.Where(x => x.CreationDate >= From);
                }

                if (search.UserId != null)
                {
                    long UserId = Convert.ToUInt64(search.UserId);
                    query = query.Where(x => x.CreatedById == UserId);

                }
                else
                {
                    query = query.Where(x => (FromWeb ? x.CreatedById == userId : true));
                }

                if (search.To != null)
                {
                    DateTimeOffset To = search.To;
                    query = query.Where(x => x.CreationDate <= To);
                }
                if (search.Code != null)
                {
                    string Code = search.Code;
                    query = query.Where(x => x.Code.Contains(Code));
                }

                if (search.InWorkFlow != null)
                {
                    bool InWorkFlow = search.InWorkFlow;
                    query = query.Where(x => (InWorkFlow ? x.WorkFlowItem!=null : x.WorkFlowItem == null));
                }

                if (search.Status != null)
                {
                    var status = (EFlowStatus)search.Status;
                    query = query.Where(x => x.WorkFlowItem != null ?  x.WorkFlowItem.Status == status : true);
                } 


            }

            if (!query.Any())
            {
                return new PaginationResult<SubmittedFormDetailsVM>(new List<SubmittedFormDetailsVM>(), 0, 0);
            }



            var result = await query.OrderByDescending(x => x.CreationDate)
                                    .Select(x => new SubmittedFormDetailsVM
                                    {
                                        Id = x.Id,
                                        CreationDate = x.CreationDate,
                                        Subject = x.Subject,
                                        Code = x.Code,
                                        Attachments = !x.Medias.Any() ? new List<FormMedia>() : x.Medias.Select(x => new FormMedia { Id = x.Media.Id , ContentType=x.Media.ContentType , Filename = x.Media.Filename , PublicUrl=x.Media.PublicUrl,Size=x.Media.Size}).ToList(),
                                        SubmittedValue = x.SubmittedValue,
                                        Status = x.WorkFlowItem != null ? (ESubmittedFormStatus)x.WorkFlowItem.Status : ESubmittedFormStatus.Draft,
                                        WorkFlowItemDetails = x.WorkFlowItem != null ? new BasicItemDetails
                                        {
                                            CreatedBy = x.WorkFlowItem.CreatedBy.FullName,
                                            CreationDate = x.WorkFlowItem.CreationDate,
                                            Note = x.WorkFlowItem.ItemHistories.Any() ? x.WorkFlowItem.ItemHistories.OrderBy(x => x.CreationDate).LastOrDefault().Note : null,
                                        } : null
                                    })
                                    .PaginateAsync(param.Pagination.Number, param.Pagination.Start);

            return result;


        }

        #region Helpers
        private async Task HandleEditMoodAsync(SubmittedForm submittedForm, long userId, SubmittedFormVM model)
        {
            if (model.AttachmentIds != null)
            {
                await HandleUpdateFormAttachments(submittedForm, model.AttachmentIds);
            }
            if (submittedForm.WorkFlowItem != null)
            {
                await HandleWorkflowItemEditAsync(submittedForm, userId, model);
            }
            else if (model.WorkFlowId.HasValue)
            {
                await CreateWorkflowItemAsync(submittedForm.Id, userId, model.WorkFlowId.Value);
            }
        }

        private async Task<long> GetWorkFlowItemId(long submittedFormId,long siteId)
        {
            var submittedForm = await _submittedFormRepository
                  .Query().AsNoTracking()
                  .Where(x => x.Id == submittedFormId && x.SiteId == siteId)
                  .Include(x => x.WorkFlowItem)
                  .Include(x => x.Medias)
                  .FirstOrDefaultAsync();

            var workflowItemId = submittedForm.WorkFlowItem != null ? submittedForm.WorkFlowItem.Id : 0;

            return workflowItemId;

        }

        private async Task HandleWorkflowItemEditAsync(SubmittedForm submittedForm, long userId, SubmittedFormVM model)
        {
            var workflowStatus = submittedForm.WorkFlowItem.Status;

            if (workflowStatus == EFlowStatus.InProgress)
            {
                throw new Exception("The form cannot be updated at the moment. The workflow item associated with this form is in progress");
            }
            else if (workflowStatus == EFlowStatus.Approved)
            {
                throw new Exception("The form cannot be updated at the moment. The workflow item associated with this form is already approved");
            }
            else if (workflowStatus == EFlowStatus.Rejected && model.WorkFlowId.HasValue)
            {
                await _workFlowItemService.UpdateItemStatus(submittedForm.WorkFlowItem.Id, userId);
            }
        }

        private async Task CreateWorkflowItemAsync(long submittedFormId, long userId, long workFlowId)
        {
            var newWorkflowItem = new CreateWorkFlowItemDto
            {
                SubmittedFormId = submittedFormId,
                WorkFlowId = workFlowId,
                UserId = userId,
            };

           await _workFlowItemService.CreateWorkFlowItem(newWorkflowItem);
        }

        public async Task<bool> ValidateWorkflow(long workflowId, long siteId)
        {
            var workflowSetup = _workFlowSetupRepository.Query().AsNoTracking()
                                                        .Where(x => x.SiteId == siteId && x.Id == workflowId)
                                                        .FirstOrDefault();

            if (workflowSetup.Type != EWorkFlowType.Forms)
                return false;


            return true;
        }

        private async Task HandleFormAttachments(IFormFileCollection Attachments,long submitedFormId , long userId ,long siteId , long? folderId)
        {
            var mediaUpload = new MediaUploadModel();

        
            var modifiedFiles = new List<IFormFile>();

            foreach (var file in Attachments)
            {
                // Modify the filename by appending userId
                var modifiedFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

       
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    // Create a new FormFile with the modified filename
                    var modifiedFile = new FormFile(new MemoryStream(fileBytes), 0, fileBytes.Length, null, modifiedFileName)
                    {
                        Headers = file.Headers,
                        ContentType = file.ContentType
                    };

                    // Add the modified file to the list
                    modifiedFiles.Add(modifiedFile);
                }
            }



            mediaUpload.Uploads = modifiedFiles;

       
            mediaUpload.SiteId = siteId;
            mediaUpload.CreatedBy = userId;
            mediaUpload.Force = false;
            mediaUpload.ParentId = folderId.HasValue ? folderId : null;

            var media = await _mediaService.SaveMedia(mediaUpload);

            var mediaIds = media.Select(x => x.Id).ToList();

            var medias = new List<SubmittedFormMedia>();
            foreach (var mediaId in mediaIds)
            {
                var submittedFormMedia = new SubmittedFormMedia
                {

                    MediaId = mediaId,
                    SubmittedFormId = submitedFormId,

                };

                medias.Add(submittedFormMedia);
            }

            _submittedFormMediaRepository.AddRange(medias);
            _submittedFormMediaRepository.SaveChanges();

        }

        private async Task<bool> HandleUpdateFormAttachments(SubmittedForm submittedForm,List<long> attachmentIds)
        {

            if(attachmentIds.Any())
            {
                 var allSubmittedMedias = submittedForm.Medias.Select(x=>x.MediaId).ToList();


                var mediaIdsToBeRemoved = allSubmittedMedias.Where(x => !attachmentIds.Contains(x)).Select(x => x).ToList();


                if (mediaIdsToBeRemoved.Any())
                {
                    var  mediaToBeRemoved = _submittedFormMediaRepository.Query()
                                    .Where(x => mediaIdsToBeRemoved.Contains(x.MediaId))
                                     .ToList();
                    _submittedFormMediaRepository.RemoveRange(mediaToBeRemoved);
                    _submittedFormMediaRepository.SaveChanges();

                }

            }

             
                
            

            return true;


        }

        private async Task ValidationInputs(StatisticsVM model)
        {
            try
            {
                foreach (var item in model.KeyValue)
                {
                   var IsStringValue= item.Value.Any(char.IsLetter);
                    
                    if ((ComparisonOperators.GreaterThan == item.ComparisonOperators ||
                        ComparisonOperators.LessThan == item.ComparisonOperators ||
                        ComparisonOperators.GreaterThanOrEqual == item.ComparisonOperators ||
                        ComparisonOperators.LessThanOrEqual == item.ComparisonOperators)&& IsStringValue)
                    {

                        throw new Exception($"Exception: The value '{item.Value}' is not an integer Because you Select Compersion For Number Values.");


                    }

                }
                

            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task<long> GetUserIdBasedOnOptions(Dictionary<string, string> userOptions)
        {
            
            if (userOptions is not null && userOptions.Any())
            {
                var uppercasedOptions = userOptions.Select(option => new
                                                {
                                                    Key = option.Key.ToUpper(),
                                                    Value = option.Value.ToUpper()
                                                })
                                                .ToList();

                var query = _userOptionsRepository.Query().AsQueryable();

                foreach (var useroption in uppercasedOptions)
                {
                    query = query.Where(u => u.NormalizedPropertyName ==useroption.Key &&
                                 u.NormalizedValue == useroption.Value);
                }

                var userIdBasedOnOptions = await query.AsNoTracking()
                                                     .GroupBy(x => x.UserId)
                                                     .Select(x => x.Key)
                                                     .FirstOrDefaultAsync();

               

                
                return userIdBasedOnOptions;
                
               

            }
            return 0;
        }

        #endregion
    }
}
