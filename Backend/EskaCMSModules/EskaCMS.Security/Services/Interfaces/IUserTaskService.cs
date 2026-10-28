using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Security.Models.TaskModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Services.Interfaces
{
    public interface IUserTaskService
    {
        Task<bool> CreateTask(CreateTaskVM model);
        Task<bool> UpdateTask(UpdateTaskVM model);
        Task<bool> UpdateTask(UpdateTaskStatusVM model);
        Task<TasksDetailsDto> TaskDetails(long taskId);
        Task<List<TaskListDto>> GetAllTasks(SmartTableParam param, long userId);
        Task<bool> DeleteTask(long taskId);
        Task<List<TaskCategoryDto>> GetAllCategories();
        Task<bool> EditMultipleTasksOrder(List<TasksOrderDto> tasks);
        Task<bool> AddCategoryForTasks(CreateTaskCategoryVM model);
        Task<bool> UpdateCategory(TaskCategoryDto model);
        Task<bool> DeleteCategory(long Id);
        Task<PaginationResult<TasksResponseDto>> GetUserProfileTasks(SmartTableParam param, string Id);
    }
}
