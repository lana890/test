using EskaCMS.Core.Models;
using EskaCMS.Events.Models;
using EskaCMS.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Events.Repositories.Interfaces
{
    public interface IEventsRepository : IBaseRepository<EventVM,long>
    {
        Task<List<EventVM>> GetAll(string Name, string Description, DateTimeOffset? FromDate, DateTimeOffset? ToDate, DateTimeOffset? FromTime, DateTimeOffset? ToTime, long? LocationId, long? CultureId, long? CategoryId, EStatus? Status, long? ParentId);

        Task<List<EventVM>> GetByParentId(long id);

        Task<bool> ToggleStatus(long EventId, EStatus eventStatus);

        Task<bool> UpdateByParentId(List<EventVM> EventVM, long EventId);



        #region Category

        Task<long> AddCategory(CategoryViewModel category);

        Task<bool> UpdateCategory(long catId, CategoryViewModel catVM);

        Task<List<CategoryViewModel>> GetCategoriesByTypeId();

        Task<List<EventVM>> GetEventsByCategoryId(long catId);

        Task DeleteCategory(long id);

        #endregion
    }
}
