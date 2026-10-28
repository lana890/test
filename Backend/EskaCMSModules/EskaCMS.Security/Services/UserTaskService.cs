using EskaCMS.Core.Extensions;
using EskaCMS.Core.Interfaces;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Security.Models.TaskModels;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.FriendshipEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Entities.Enum.UserTaskEnums;

namespace EskaCMS.Security.Services
{
    public class UserTaskService : IUserTaskService
    {

        private readonly IRepository<UserTasks> _userTasksRepository;
        private readonly IRepository<UserTasksHistory> _userTasksHistoryRepository;
        private readonly IRepository<CategoryDetails> _categoryDetailsRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Friendships> _userFriendshipsRepository;
        private readonly ISiteCultureService _siteCultureService;


        public UserTaskService(
            IWorkContext workContext,
            IRepository<UserTasks> userTasksRepository,
             IRepository<UserTasksHistory> userTasksHistoryRepository,
              IRepository<CategoryDetails> categoryDetailsRepository,
              IRepository<Category> categoryRepository,
              ISiteCultureService siteCultureService,
             IRepository<Friendships> userFriendshipsRepository


       )
        {
            _workContext = workContext;
            _userTasksRepository = userTasksRepository;
            _userTasksHistoryRepository = userTasksHistoryRepository;
            _categoryDetailsRepository = categoryDetailsRepository;
            _categoryRepository = categoryRepository;
            _siteCultureService = siteCultureService;
            _userFriendshipsRepository = userFriendshipsRepository;
        }



        public async Task<bool> CreateTask(CreateTaskVM model)
        {
            var userId = await _workContext.GetCurrentUserId();

            var siteId = _workContext.GetCurrentSiteId();

            var lastUserTask = await _userTasksRepository.Query()
                                                         .Where(x => x.CreatedById == userId && x.SiteId == siteId && x.Status != ETaskStatus.Deleted)
                                                         .OrderBy(x => x.Order)
                                                         .LastOrDefaultAsync();
            
            var toDoItemOrder = lastUserTask is null ? 0 : lastUserTask.Order+1;



            var taskToBeAdded = new UserTasks
            {
                Title = model.Title,
                Description = model.Description,
                CreatedById = userId,
                ExpectedStartDate = model.ExpectedStartDate,
                ExpectedEndDate = model.ExpectedEndDate,
                SiteId = siteId,
                CreatedDate = DateTimeOffset.Now,
                Status = model.Status.Value,
                ParentId = model.ParentId,
                Priority = model.Priority.Value,
                CategoryId = model.CategoryId,
                Order = toDoItemOrder,
                Privacy = model.Privacy.Value
            };

            _userTasksRepository.Add(taskToBeAdded);
            _userTasksRepository.SaveChanges();

            await InsertTaskHistory(taskToBeAdded);

            return true;
        }


        public async Task<bool> UpdateTask(UpdateTaskVM model)
        {
            var userId = await _workContext.GetCurrentUserId();

            var siteId = _workContext.GetCurrentSiteId();

            var taskToBeUpdate = _userTasksRepository.Query()
                                 .Where(x => x.Id == model.Id && x.SiteId == siteId && x.CreatedById == userId)
                                 .FirstOrDefault();

            if (taskToBeUpdate is null)
            {
                throw new Exception("The selected task doesn't found");
            }


            taskToBeUpdate.Title = model.Title;
            taskToBeUpdate.Description = model.Description;
            taskToBeUpdate.ExpectedStartDate = model.ExpectedStartDate;
            taskToBeUpdate.ExpectedEndDate = model.ExpectedEndDate;
            taskToBeUpdate.ModificationDate = DateTimeOffset.Now;
            taskToBeUpdate.Priority = model.Priority;
            taskToBeUpdate.Status = model.Status;
            taskToBeUpdate.CategoryId = model.CategoryId;
            taskToBeUpdate.Privacy = model.Privacy;

            if (taskToBeUpdate.Status != model.Status)
            {
                taskToBeUpdate.StatusDate = DateTimeOffset.Now;
            }

            await InsertTaskHistory(taskToBeUpdate);
            _userTasksRepository.SaveChanges();

            return true;
        }


        public async Task<bool> UpdateTask(UpdateTaskStatusVM model)
        {
            var userId = await _workContext.GetCurrentUserId();

            var siteId = _workContext.GetCurrentSiteId();

            var taskToBeUpdate = _userTasksRepository.Query()
                                 .Where(x => x.Id == model.Id && x.SiteId == siteId && x.CreatedById == userId)
                                 .FirstOrDefault();

            var lastUserTask = await _userTasksRepository.Query()
                                                         .Where(x =>  x.Id != taskToBeUpdate.Id && x.CreatedById == userId && x.SiteId == siteId && x.Status != ETaskStatus.Deleted)
                                                         .OrderBy(x => x.Order)
                                                         .LastOrDefaultAsync();


            taskToBeUpdate.Order = lastUserTask is null ? taskToBeUpdate.Order : lastUserTask.Order + 1;
            taskToBeUpdate.Status = model.Status;   

            await InsertTaskHistory(taskToBeUpdate);
            _userTasksRepository.SaveChanges();

            return true;
        }

        public async Task<bool> DeleteTask(long taskId)
        {
            var userId = await _workContext.GetCurrentUserId();

            var siteId = _workContext.GetCurrentSiteId();

            var taskToBeUpdate = _userTasksRepository.Query()
                                 .Where(x => x.Id == taskId && x.SiteId == siteId && x.CreatedById == userId)
                                 .FirstOrDefault();

            if (taskToBeUpdate is null)
            {
                throw new Exception("The selected task doesn't found");
            }



            taskToBeUpdate.Status = ETaskStatus.Deleted;

            taskToBeUpdate.StatusDate = DateTimeOffset.Now;


            await InsertTaskHistory(taskToBeUpdate);
            _userTasksRepository.SaveChanges();

            return true;
        }

        public async Task<bool> InsertTaskHistory(UserTasks model)
        {
            var taskHistory = new UserTasksHistory
            {
                Priority = model.Priority,
                Status = model.Status,
                CreationDate = DateTimeOffset.Now,
                Description = model.Description,
                Title = model.Title,
                ExpectedEndDate = model.ExpectedStartDate,
                ExpectedStartDate = model.ExpectedStartDate,
                UserTaskId = model.Id,
                Privacy = model.Privacy
            };



            _userTasksHistoryRepository.Add(taskHistory);
            _userTasksHistoryRepository.SaveChanges();


            return true;
        }

        public async Task<TasksDetailsDto> TaskDetails(long taskId)
        {
            var userId = await _workContext.GetCurrentUserId();

            var siteId = _workContext.GetCurrentSiteId();

            var taskInDb = _userTasksRepository.Query()
                                 .Where(x => x.Id == taskId && x.SiteId == siteId && x.CreatedById == userId && x.Status != ETaskStatus.Deleted)
                                 .Include(x => x.ChildrenTasks)
                                 .Include(x => x.Category)
                                 .ThenInclude(x => x.CategoryDetails)
                                 .FirstOrDefault();

            if (taskInDb is null)
            {
                throw new Exception("The selected task doesn't found");
            }



            var result = new TasksDetailsDto
            {
                Id = taskInDb.Id,
                Status = taskInDb.Status,
                Description = taskInDb.Description,
                CreationDate = DateTimeOffset.Now,
                ExpectedEndDate = taskInDb.ExpectedEndDate,
                ExpectedStartDate = taskInDb.ExpectedStartDate,
                Title = taskInDb.Title,
                Order = taskInDb.Order,
                Privacy = taskInDb.Privacy,
                ChildrenTasks = taskInDb.ChildrenTasks.Any() ? taskInDb.ChildrenTasks.Where(x => x.Status != ETaskStatus.Deleted).Select(x => new TasksResponseDto
                {
                    Id = x.Id,
                    Order = x.Order,
                    Priority = x.Priority,
                    ExpectedStartDate = x.ExpectedStartDate,
                    ExpectedEndDate = x.ExpectedEndDate,
                    Status = x.Status,
                    Description = x.Description,
                    Title = x.Title,
                    Privacy = x.Privacy
                }).OrderBy(x => x.Order).ToList() : new List<TasksResponseDto>(),
            };




            return result;
        }

        public async Task<List<TaskListDto>> GetAllTasks(SmartTableParam param, long userId)
        {


            var siteId = _workContext.GetCurrentSiteId();

            var cultureId = _workContext.GetCurrentCultureId();

            var allUserTasks = _userTasksRepository.Query()
                                 .Where(x => x.SiteId == siteId && x.CreatedById == userId && x.Status != ETaskStatus.Deleted && !x.ParentId.HasValue)
                                 .Include(x => x.Category)
                                 .ThenInclude(x => x.CategoryDetails)
                                 .AsNoTracking();



            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;



                if (search.Title != null)
                {
                    string Title = search.Title;
                    allUserTasks = allUserTasks.Where(x => x.Title.ToLower().Contains(Title.ToLower()));
                }


                if (search.Status != null)
                {
                    var status = (ETaskStatus)search.Status;
                    allUserTasks = allUserTasks.Where(x => x.Status == status);
                }
                if (search.Priority != null)
                {
                    var Priority = (ETaskPriority)search.Priority;
                    allUserTasks = allUserTasks.Where(x => x.Priority == Priority);
                }


                if (search.CreationDate != null)
                {
                    var CreationDate = (DateTimeOffset)search.CreationDate;
                    allUserTasks = allUserTasks.Where(x => x.CreatedDate.Date == CreationDate.Date);
                }



            }



            var taskList = allUserTasks.ToList();
            var result = taskList.GroupBy(x => x.CategoryId)
                                     .Select(group => new { CategoryId = group.Key, GroupItems = group.ToList() })
                                     .Select(x =>
                                     new TaskListDto
                                     {
                                         CategoryId = x.CategoryId,
                                         CategoryName = x.CategoryId.HasValue ? x.GroupItems.FirstOrDefault()?.Category.CategoryDetails.FirstOrDefault()?.Name : string.Empty,
                                         ToDoLists = x.GroupItems.Select(x => new TasksResponseDto
                                         {
                                             Id = x.Id,
                                             Title = x.Title,
                                             Description = x.Description,
                                             ExpectedEndDate = x.ExpectedEndDate,
                                             ExpectedStartDate = x.ExpectedStartDate,
                                             Priority = x.Priority,
                                             Order = x.Order,
                                             Status = x.Status,
                                             CategoryId = x.CategoryId,
                                             CreatedDate= x.CreatedDate,    
                                             Privacy=x.Privacy,
                                         }).OrderBy(x=>x.Order).ToList(),

                                     }
                                      )
                                      .ToList();






            return result;

        }

        public async Task<List<TaskCategoryDto>> GetAllCategories()
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                var cultureId = (await _siteCultureService.GetSiteCultures(SiteId)).FirstOrDefault()?.Id;

                var userId = await _workContext.GetCurrentUserId();

                var query = await _categoryDetailsRepository.Query()
                                                .Where(x => x.Category.Status != EStatus.Deleted &&
                                                            x.Category.CreatedById == userId &&
                                                            x.Category.SiteId == SiteId &&
                                                            x.Category.CategoryTypeId == (int)ECategoryTypes.ToDoList &&
                                                            x.CultureId == cultureId)
                                                .AsNoTracking()
                                                .Select(x => new TaskCategoryDto
                                                {
                                                    Id = x.CategoryId,
                                                    Name = x.Name,
                                                }).ToListAsync();

                return query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> AddCategoryForTasks(CreateTaskCategoryVM model)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();

                var allCulture = await _siteCultureService.GetSiteCultures(siteId);

                var userId = await _workContext.GetCurrentUserId();

                var categoryTobBeAdded = new Category
                {
                    CreationDate = DateTimeOffset.Now,
                    CreatedById = userId,
                    CategoryTypeId = (int)ECategoryTypes.ToDoList,
                    SiteId = siteId,
                    CategoryDetails = allCulture.Select(x => new CategoryDetails { Name = model.Name, CultureId = x.Id }).ToList()
                };

                _categoryRepository.Add(categoryTobBeAdded);
                _categoryRepository.SaveChanges();




                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<bool> UpdateCategory(TaskCategoryDto model)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();


                var userId = await _workContext.GetCurrentUserId();

                var categoryDetails = await _categoryDetailsRepository.Query()
                                                 .Where(x => x.Category.Status != EStatus.Deleted &&
                                                             x.Category.CreatedById == userId &&
                                                             x.Category.SiteId == siteId &&
                                                             x.Category.CategoryTypeId == (int)ECategoryTypes.ToDoList &&
                                                             x.CategoryId == model.Id)
                                                 .ToListAsync();


                categoryDetails.ForEach(x => x.Name = model.Name);


                _categoryRepository.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> DeleteCategory(long categoryyId)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();



                var category = _categoryRepository.Query()
                                                 .Where(x => x.Id == categoryyId && x.SiteId == siteId && x.Status != EStatus.Deleted)
                                                 .FirstOrDefault();


                category.Status = EStatus.Deleted;
                category.StatusDate = DateTimeOffset.Now;



                _categoryRepository.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> EditMultipleTasksOrder(List<TasksOrderDto> tasks)
        {

            var userId = await _workContext.GetCurrentUserId();
            var siteId = _workContext.GetCurrentSiteId();



            var taskIds = tasks.Select(x => x.Id).ToList();

            var targetTasks = _userTasksRepository.Query()
                                            .Where(x => taskIds.Contains(x.Id) && x.CreatedById == userId && x.SiteId == siteId)
                                            .ToList();


            foreach (var stageInfo in tasks)
            {

                var stage = targetTasks.Where(x => x.Id == stageInfo.Id).FirstOrDefault();

                if (stage != null)
                {
                    stage.Order = stageInfo.Order;
                }

            }


            await _userTasksRepository.SaveChangesAsync();

            return true;
        }


        public async Task<PaginationResult<TasksResponseDto>> GetUserProfileTasks(SmartTableParam param, string Id)
        {


            var siteId = _workContext.GetCurrentSiteId();

            var currentUserId = await _workContext.GetCurrentUserId();


            if (!string.IsNullOrEmpty(Id))
            {
                var decryptedUserId = EncryptDecryptHelper.Decrypt(Id);
                var userId = long.Parse(decryptedUserId);



                var userFriendShips = _userFriendshipsRepository.Query()
                                                 .Where(x => x.SiteId == siteId && (x.ToId == currentUserId || x.FromId == currentUserId) && x.Status != EFriendshipStatus.Deleted)
                                                 .AsNoTracking()
                                                 .ToList();

                var friendStatus = GetFriendshipStatus(userFriendShips, currentUserId, userId);

                var allUserTasks = _userTasksRepository.Query()
                           .Where(x =>  x.SiteId == siteId && x.CreatedById == userId && x.Status != ETaskStatus.Deleted  && !x.ParentId.HasValue &&(friendStatus == EUserProfileStatus.Friend ? (x.Privacy == ETaskPrivacy.Public || x.Privacy == ETaskPrivacy.FirnedsOnly) : x.Privacy == ETaskPrivacy.Public))
                           .Include(x => x.Category)
                           .ThenInclude(x => x.CategoryDetails)
                           .AsNoTracking();

                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;



                    if (search.Title != null)
                    {
                        string Title = search.Title;
                        allUserTasks = allUserTasks.Where(x => x.Title.ToLower().Contains(Title.ToLower()));
                    }


                    if (search.Status != null)
                    {
                        var status = (ETaskStatus)search.Status;
                        allUserTasks = allUserTasks.Where(x => x.Status == status);
                    }
                    if (search.Priority != null)
                    {
                        var Priority = (ETaskPriority)search.Priority;
                        allUserTasks = allUserTasks.Where(x => x.Priority == Priority);
                    }

                }




                var result = await allUserTasks.OrderByDescending(x => x.CreatedDate).Select(x => new TasksResponseDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    ExpectedEndDate = x.ExpectedEndDate,
                    ExpectedStartDate = x.ExpectedStartDate,
                    Priority = x.Priority,
                    Order = x.Order,
                    Status = x.Status,
                    CategoryId = x.CategoryId,
                    Privacy = x.Privacy,
                    CreatedDate = x.CreatedDate


                }).PaginateAsync(param.Pagination.Number, param.Pagination.Start);



                return result;

            }
            else
            {


                var allUserTasks = _userTasksRepository.Query()
                               .Where(x => x.SiteId == siteId && x.CreatedById == currentUserId && x.Status != ETaskStatus.Deleted && !x.ParentId.HasValue)
                               .Include(x => x.Category)
                               .ThenInclude(x => x.CategoryDetails)
                               .AsNoTracking();



                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;



                    if (search.Title != null)
                    {
                        string Title = search.Title;
                        allUserTasks = allUserTasks.Where(x => x.Title.ToLower().Contains(Title.ToLower()));
                    }


                    if (search.Status != null)
                    {
                        var status = (ETaskStatus)search.Status;
                        allUserTasks = allUserTasks.Where(x => x.Status == status);
                    }
                    if (search.Priority != null)
                    {
                        var Priority = (ETaskPriority)search.Priority;
                        allUserTasks = allUserTasks.Where(x => x.Priority == Priority);
                    }

                }




                var result = await allUserTasks.OrderByDescending(x => x.CreatedDate).Select(x => new TasksResponseDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    ExpectedEndDate = x.ExpectedEndDate,
                    ExpectedStartDate = x.ExpectedStartDate,
                    Priority = x.Priority,
                    Order = x.Order,
                    Status = x.Status,
                    CategoryId = x.CategoryId,
                    Privacy = x.Privacy,
                    CreatedDate=x.CreatedDate

                }).PaginateAsync(param.Pagination.Number, param.Pagination.Start);

                return result;

            }

        }



        #region Helper

        private static EUserProfileStatus GetFriendshipStatus(List<Friendships> allCurrentUserFriendships, long currentUserId, long targetUserProfileId)
        {

            var filterdData = allCurrentUserFriendships.Where(x => x.ToId == targetUserProfileId || x.FromId == targetUserProfileId).LastOrDefault();

            if (filterdData is null)
            {
                return EUserProfileStatus.NotFriend;
            }

            switch (filterdData.Status)
            {
                case EFriendshipStatus.Accepted:
                    return EUserProfileStatus.Friend;
                case EFriendshipStatus.Requested when filterdData.FromId == currentUserId:
                    return EUserProfileStatus.SentRequest;
                case EFriendshipStatus.Requested when filterdData.ToId == currentUserId:
                    return EUserProfileStatus.ReceivedRequest;
                default:
                    return EUserProfileStatus.NotFriend;
            }
        }
        #endregion
    }
}
