using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Events.Models;
using EskaCMS.Events.Repositories.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Events;

namespace EskaCMS.Events.Services.Repositories
{
    public class EventsRepository : IEventsRepository
    {
        private readonly IRepository<Event> _eventRepository;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Category> _categoriesRepository;
        private readonly IRepository<CategoryDetails> _categoryDetailsRepository;
        private readonly ISlugServices _SlugServices;

        public EventsRepository(
            IRepository<Event> eventRepository,
            IWorkContext workContext,
            IRepository<Category> categoriesRepository,
            IRepository<CategoryDetails> CategoryDetailsRepository,
            ISlugServices SlugServices
            )
        {
            _eventRepository = eventRepository;
            _workContext = workContext;
            _categoriesRepository = categoriesRepository;
_categoryDetailsRepository = CategoryDetailsRepository;
            _SlugServices = SlugServices;   
        }

        public async Task<EventVM> GetById(long id)
        {
            return await _eventRepository.Query().Include(x => x.CreatedBy)
                .Where(x => x.Id == id ).Select(x => new EventVM
            {


                ThumbnailId = x.ThumbnailId,
                AdditionalMetaTags = x.AddtionalMetaTags,
                Duration = x.Duration,
                FromDate = x.FromDate,
                ToDate = x.ToDate,
                EventStatus = x.EventStatus,
                Content = new ContentVM
                {
                    Name = x.Name,
                    CultureId = x.CultureId,
                    Description = x.Description
                },
                FromTime = x.FromTime,
                ToTime = x.ToTime,
                Importance = x.Importance,
                Keywords = x.Keywords,
                LocationId = x.LocationId,
                MaxEnrollment = x.MaxEnrollment,
                MetaDescription = x.MetaDescription,
                RepeatType = x.RepeatType,
                SendReminder = x.SendReminder,
                Settings = x.Settings,
                CategoryId = x.CategoryId,
                PeriodInterval = x.PeriodInterval,
                PeriodType = x.PeriodType,
                SiteId = x.SiteId,
                CreatedById = x.CreatedById,
                CoreUsername = x.CreatedBy.UserName

            }).FirstOrDefaultAsync();
        }

        public async Task<List<EventVM>> GetByParentId(long id)
        {
            return await _eventRepository.Query().Include(x => x.CreatedBy)
                .Where(x => (x.ParentId == id )).Select(x => new EventVM
            {
                ThumbnailId = x.ThumbnailId,
                AdditionalMetaTags = x.AddtionalMetaTags,
                Duration = x.Duration,
                FromDate = x.FromDate,
                ToDate = x.ToDate,
                EventStatus = x.EventStatus,
                Content = new ContentVM
                {
                    Name = x.Name,
                    CultureId = x.CultureId,
                    Description = x.Description
                },
                FromTime = x.FromTime,
                ToTime = x.ToTime,
                Importance = x.Importance,
                Keywords = x.Keywords,
                LocationId = x.LocationId,
                MaxEnrollment = x.MaxEnrollment,
                MetaDescription = x.MetaDescription,
                RepeatType = x.RepeatType,
                SendReminder = x.SendReminder,
                Settings = x.Settings,
                CategoryId = x.CategoryId,
                PeriodInterval = x.PeriodInterval,
                PeriodType = x.PeriodType,
                SiteId = x.SiteId,
                CreatedById = x.CreatedById,
                CoreUsername = x.CreatedBy.UserName,
                ParentId = x.ParentId

            }).ToListAsync();
        }

        public async Task<long> Create(EventVM eventVM)
        {
            var siteId = await _workContext.GetCurrentSiteIdAsync();
            var userId = await _workContext.GetCurrentUserId();

            var EntityEvent = new Event();
            EntityEvent.AddtionalMetaTags = eventVM.AdditionalMetaTags;
            EntityEvent.CreatedById = userId;
            EntityEvent.CreationDate = DateTimeOffset.Now;
            EntityEvent.Duration = eventVM.Duration;
            EntityEvent.EventStatus = eventVM.EventStatus;
            EntityEvent.FromDate = eventVM.FromDate;
            EntityEvent.FromTime = eventVM.FromTime;
            EntityEvent.ToDate = eventVM.ToDate;
            EntityEvent.ToTime = eventVM.ToTime;
            EntityEvent.Importance = eventVM.Importance;
            EntityEvent.Keywords = eventVM.Keywords;
            EntityEvent.LocationId = eventVM.LocationId != 0? eventVM.LocationId : null ;
            EntityEvent.MaxEnrollment = eventVM.MaxEnrollment;
            EntityEvent.MetaDescription = eventVM.MetaDescription;
            EntityEvent.RepeatType = eventVM.RepeatType;
            EntityEvent.SendReminder = eventVM.SendReminder;
            EntityEvent.Settings = eventVM.Settings;
            EntityEvent.SiteId = siteId;
            EntityEvent.ThumbnailId = eventVM.ThumbnailId;
            EntityEvent.CategoryId = eventVM.CategoryId;
            EntityEvent.PeriodType = eventVM.PeriodType;
            EntityEvent.PeriodInterval = eventVM.PeriodInterval;
            EntityEvent.NumberOfDays = eventVM.NumberOfDays;
            EntityEvent.EventPlace = eventVM.EventPlace;
            EntityEvent.ParentId = eventVM.ParentId;
            if (eventVM.Content != null)
            {
                EntityEvent.CultureId = eventVM.Content.CultureId;
                EntityEvent.Description = eventVM.Content.Description;
                EntityEvent.Name = eventVM.Content.Name;
            }

            _eventRepository.Add(EntityEvent);
            await _eventRepository.SaveChangesAsync();
            return EntityEvent.Id;
        }

        public async Task Delete(long Id)
        {
            await _eventRepository.Query().FirstOrDefaultAsync(x => x.Id == Id);
            return;
        }

        public async Task<bool> Update(EventVM EventVM, long EventId)
        {
            EventVM.ModifiedById = await _workContext.GetCurrentUserId();
            EventVM.SiteId = await _workContext.GetCurrentSiteIdAsync();
            var EntityEvent = await _eventRepository.Query().Where(x => x.Id == EventId).FirstOrDefaultAsync();
            EntityEvent.AddtionalMetaTags = EventVM.AdditionalMetaTags;
            EntityEvent.ModifiedById = EventVM.ModifiedById;
            EntityEvent.ModificationDate = DateTimeOffset.Now;
            EntityEvent.CultureId = EventVM.Content.CultureId;
            EntityEvent.Description = EventVM.Content.Description;
            EntityEvent.Duration = EventVM.Duration;
            EntityEvent.EventStatus = EventVM.EventStatus;
            EntityEvent.FromDate = EventVM.FromDate;
            EntityEvent.FromTime = EventVM.FromTime;
            EntityEvent.Importance = EventVM.Importance;
            EntityEvent.Keywords = EventVM.Keywords;
            EntityEvent.LocationId = EventVM.LocationId != 0 ? EventVM.LocationId : null;
            EntityEvent.MaxEnrollment = EventVM.MaxEnrollment;
            EntityEvent.MetaDescription = EventVM.MetaDescription;
            EntityEvent.Name = EventVM.Content.Name;
            EntityEvent.RepeatType = EventVM.RepeatType;
            EntityEvent.SendReminder = EventVM.SendReminder;
            EntityEvent.Settings = EventVM.Settings;
            EntityEvent.SiteId = EventVM.SiteId;
            EntityEvent.ThumbnailId = EventVM.ThumbnailId;
            EntityEvent.CategoryId = EventVM.CategoryId;
            EntityEvent.PeriodType = EventVM.PeriodType;
            EntityEvent.PeriodInterval = EventVM.PeriodInterval;
            EntityEvent.NumberOfDays = EventVM.NumberOfDays;
            EntityEvent.EventPlace = EventVM.EventPlace;
            EntityEvent.ParentId = EventVM.ParentId;
            _eventRepository.Update(EntityEvent);
            await _eventRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateByParentId(List<EventVM> EventVMList, long ParentId)
        {
            var ModifiedById = await _workContext.GetCurrentUserId();
            var SiteId = await _workContext.GetCurrentSiteIdAsync();
            var EntityEventList = await _eventRepository.Query().Where(x => (x.ParentId == ParentId)).ToListAsync();

            foreach (EventVM eventVM in EventVMList)
            {
                var EntityEvent = EntityEventList.Where(x => x.CultureId == eventVM.Content.CultureId).FirstOrDefault();
                if (EntityEvent != null)
                {
                    EntityEvent.AddtionalMetaTags = eventVM.AdditionalMetaTags;
                    EntityEvent.ModifiedById = ModifiedById;
                    EntityEvent.ModificationDate = DateTimeOffset.Now;
                    EntityEvent.CultureId = eventVM.Content.CultureId;
                    EntityEvent.Description = eventVM.Content.Description;
                    EntityEvent.Duration = eventVM.Duration;
                    EntityEvent.EventStatus = eventVM.EventStatus;
                    EntityEvent.FromDate = eventVM.FromDate;
                    EntityEvent.FromTime = eventVM.FromTime;
                    EntityEvent.Importance = eventVM.Importance;
                    EntityEvent.Keywords = eventVM.Keywords;
                    EntityEvent.LocationId = eventVM.LocationId;
                    EntityEvent.MaxEnrollment = eventVM.MaxEnrollment;
                    EntityEvent.MetaDescription = eventVM.MetaDescription;
                    EntityEvent.Name = eventVM.Content.Name;
                    EntityEvent.RepeatType = eventVM.RepeatType;
                    EntityEvent.SendReminder = eventVM.SendReminder;
                    EntityEvent.Settings = eventVM.Settings;
                    EntityEvent.SiteId = SiteId;
                    EntityEvent.ThumbnailId = eventVM.ThumbnailId;
                    EntityEvent.CategoryId = eventVM.CategoryId;
                    EntityEvent.PeriodType = eventVM.PeriodType;
                    EntityEvent.PeriodInterval = eventVM.PeriodInterval;
                    EntityEvent.NumberOfDays = eventVM.NumberOfDays;
                    EntityEvent.EventPlace = eventVM.EventPlace;
                    _eventRepository.Update(EntityEvent);
                }
                else
                {
                    eventVM.ParentId = ParentId;
                    await Create(eventVM);
                }
                await _eventRepository.SaveChangesAsync();
            }
            return true;
        }


        public async Task<List<EventVM>> GetAll(string Name, string Description, DateTimeOffset? FromDate, 
            DateTimeOffset? ToDate, DateTimeOffset? FromTime, DateTimeOffset? ToTime, long? LocationId, 
            long? CultureId, long? CategoryId, EStatus? Status,long? ParentId)
        {
            var SiteId = await _workContext.GetCurrentSiteIdAsync();
            return await _eventRepository.Query().Include(x => x.Thumbnail).Include(x => x.CreatedBy).Where(x =>
            x.EventStatus != EStatus.Deleted &&
             ((x.SiteId == SiteId)) && ((x.EventStatus == Status) || Status == null) &&
             ((x.CultureId == CultureId) || CultureId == null) && 
             ((x.CategoryId == CategoryId) || CategoryId == null) &&
             ((x.Name.ToLower().Contains(Name.ToLower())) || string.IsNullOrEmpty(Name)) &&
             ((x.Description.Contains(Description)) || string.IsNullOrEmpty(Description)) &&
             ((x.FromDate > FromDate) || FromDate == null) &&
             ((x.ToDate < ToDate) || ToDate == null) &&
             ((x.FromTime > FromTime) || FromTime == null) &&
             ((x.ToTime < ToTime) || ToTime == null) && 
             ((x.LocationId == LocationId) || LocationId == null)&&
             ((x.ParentId == ParentId) || ParentId == null))
            .Select(x => new EventVM
            {
                Content = new ContentVM
                {
                    CultureId = x.CultureId,
                    Name = x.Name,
                    Description = x.Description
                },
                ThumbnailId = x.ThumbnailId,
                AdditionalMetaTags = x.AddtionalMetaTags,
                CreationDate = x.CreationDate,
                Duration = x.Duration,
                FromDate = x.FromDate,
                ToDate = x.ToDate,
                EventStatus = x.EventStatus,
                FromTime = x.FromTime,
                ToTime = x.ToTime,
                Importance = x.Importance,
                Keywords = x.Keywords,
                LocationId = x.LocationId,
                MaxEnrollment = x.MaxEnrollment,
                MetaDescription = x.MetaDescription,
                RepeatType = x.RepeatType,
                SendReminder = x.SendReminder,
                Settings = x.Settings,
                SiteId = x.SiteId,
                CategoryId = x.CategoryId,
                PeriodInterval = x.PeriodInterval,
                PeriodType = x.PeriodType,
                RequestDate = x.CreationDate,
                Id = x.Id,
                ThumbnailPath = x.Thumbnail.PublicUrl,
                NumberOfDays = x.NumberOfDays,
                EventPlace = x.EventPlace,
                CreatedById = x.CreatedById,
                CoreUsername = x.CreatedBy.UserName,
                ParentId = x.ParentId
            }).ToListAsync();
        }

        public async Task<bool> ToggleStatus(long EventId, EStatus eventStatus)
        {
            var userId = await _workContext.GetCurrentUserId();
            var EntityEventList = await _eventRepository.Query().Where(x => x.ParentId == EventId).ToListAsync();
            foreach (Event EntityEvent in EntityEventList)
            {
                EntityEvent.ModifiedById = userId;
                EntityEvent.ModificationDate = DateTimeOffset.Now;
                EntityEvent.EventStatus = eventStatus;
                _eventRepository.Update(EntityEvent);
            }
            await _eventRepository.SaveChangesAsync();
            return true;
        }



        #region Category
        public async Task<long> AddCategory(CategoryViewModel model)
        {
            
                try
                {
                    long UserId = await _workContext.GetCurrentUserId();
                    long SiteId = _workContext.GetCurrentSiteId();

                    var Category = new Category();

                    Category.SiteId = SiteId;
                    Category.CategoryTypeId = Convert.ToInt64(model.CategoryTypeId);
                    Category.CreatedById = UserId;
                    Category.IncludeInMenu = model.IncludeInMenu;
                    Category.StatusDate = DateTime.Now;
                    Category.CreationDate = DateTime.Now;
                    Category.ParentId = model.ParentId == 0 ? null : model.ParentId;
                    Category.DisplayOrder = model.DisplayOrder;
                    Category.Status = model.Status;
                    _categoriesRepository.Add(Category);
                    await _categoriesRepository.SaveChangesAsync();

                    await InsertCategoryDetails(model, Category.Id);

                    return Category.Id;

                }
                catch (Exception ex)
                {

                    throw new Exception(ex.Message.Replace("Category: ", ""));
                }
            
        }

        private async Task<bool> InsertCategoryDetails(CategoryViewModel category, long id)
        {
            try
            {
                long SiteId = _workContext.GetCurrentSiteId();

                var CategoriesDetails = new CategoryDetails();



                var slugInfo = new SlugManagementVm
                {

                    CultureId = category.CultureId,
                    Entity = EEntity.Category,
                    SiteId = SiteId,
                    Slug = category.Slug


                };




                CategoriesDetails.CategoryId = id;
                CategoriesDetails.Name = category.Name;
                CategoriesDetails.Description = category.Description;
                CategoriesDetails.MetaTitle = category.MetaTitle;
                CategoriesDetails.MetaDescription = category.MetaDescription;
                CategoriesDetails.Description = category.Description;
                CategoriesDetails.CategoryTemplateId = category.CategoryTemplateId == 0 || category.CategoryTemplateId == null ? null : category.CategoryTemplateId;
                CategoriesDetails.CultureId = category.CultureId;
                CategoriesDetails.SlugInfoId = await _SlugServices.UpsertAndGetId(slugInfo);
                CategoriesDetails.Name = category.Name;
                _categoryDetailsRepository.Add(CategoriesDetails);

                await _categoryDetailsRepository.SaveChangesAsync();








                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }


        }

        public async Task<bool> UpdateCategory(long catId, CategoryViewModel catVM)
        {
            var userId = await _workContext.GetCurrentUserId();
            var cat = await _categoriesRepository.Query().Include(x=>x.CategoryDetails).ThenInclude(x=>x.SlugInfo).Where(x => x.Id == catId).FirstOrDefaultAsync();
            cat.ModifiedById = userId;
            cat.ModificationDate = DateTimeOffset.Now;


            cat.Status = catVM.Status;
            cat.StatusDate = DateTimeOffset.Now;
            cat.IncludeInMenu = catVM.IncludeInMenu;
            cat.ParentId = catVM.ParentId;
            cat.ThumbnailImageId = catVM.ThumnailImageId;
            cat.SiteId = catVM.SiteId;
            cat.DisplayOrder = catVM.DisplayOrder;
            cat.ParentId = catVM.ParentId != 0 ? catVM.ParentId : null;
            _categoriesRepository.Update(cat);
            await _categoriesRepository.SaveChangesAsync();
            await UpdateCategoryDetails(catVM,catId);
        
            return true;
        }

        private async Task<bool> UpdateCategoryDetails(CategoryViewModel catVM,long Id)
        {
            try
            {
                var cat =await _categoryDetailsRepository.Query().Where(x => x.CategoryId == Id).FirstOrDefaultAsync();
                cat.Name = catVM.Name;
                cat.Description = catVM.Description;
                SlugManagementVm slugManagementVm = new()
                {
                    Id = cat.SlugInfoId ?? 0,
                    CultureId = cat.CultureId,
                    Entity = EEntity.Category,


                };
                if (cat.SlugInfoId == null)
                {
                    slugManagementVm.Slug = catVM.Slug;
                }
                else
                {
                    slugManagementVm.Slug = catVM.Name + "-slug";
                    slugManagementVm.Id = cat.SlugInfoId.HasValue ? cat.SlugInfoId.Value : 0;

                }
                cat.SlugInfoId = await _SlugServices.Upsert(slugManagementVm);
                cat.MetaDescription = catVM.MetaDescription;
                cat.MetaKeywords = catVM.MetaKeywords;
                cat.MetaTitle = catVM.MetaTitle;
               await _categoryDetailsRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<CategoryViewModel>> GetCategoriesByTypeId()
        {
            try
            {
                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                var query = _categoriesRepository.Query().Include(x=>x.CategoryDetails).Where(x => x.CategoryTypeId == (int)ECategoryTypes.Events
                && ((x.SiteId == SiteId) || x.SiteId == null)
                && x.Status != EStatus.Deleted);
                return await query.Select(p => new CategoryViewModel
                {
                    Id = p.Id,
                    Name = p.CategoryDetails.Select(x=>x.Name).FirstOrDefault(),
                    CategoryTypeId = (ECategoryTypes)p.CategoryTypeId,
                    ParentId = p.ParentId,
                    SiteId = p.SiteId,
                    Status = p.Status,
                    CreatedById = p.CreatedById,
                    TotalEvents = _eventRepository.Query().Where(x => x.CategoryId == p.Id).Count()
                }).ToListAsync();
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<EventVM>> GetEventsByCategoryId(long catId)
        {
            try
            {
                return await _eventRepository.Query().Include(x => x.CreatedBy).Where(x => x.CategoryId == catId).Select(x => new EventVM
                {

                    ThumbnailId = x.ThumbnailId,
                    AdditionalMetaTags = x.AddtionalMetaTags,
                    Duration = x.Duration,
                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    EventStatus = x.EventStatus,
                    Content = new ContentVM
                    {
                        Name = x.Name,
                        Description = x.Description,
                    },
                    FromTime = x.FromTime,
                    ToTime = x.ToTime,
                    Importance = x.Importance,
                    Keywords = x.Keywords,
                    LocationId = x.LocationId,
                    MaxEnrollment = x.MaxEnrollment,
                    MetaDescription = x.MetaDescription,
                    RepeatType = x.RepeatType,
                    SendReminder = x.SendReminder,
                    Settings = x.Settings,
                    CategoryId = x.CategoryId,
                    PeriodInterval = x.PeriodInterval,
                    PeriodType = x.PeriodType,
                    SiteId = x.SiteId,
                    CoreUsername = x.CreatedBy.UserName,
                    CreatedById = x.CreatedById
                }).ToListAsync();
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task DeleteCategory(long id)
        {
            var category = await _categoriesRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            category.Status = EStatus.Deleted /*Delete*/;
            _categoriesRepository.Update(category);
            _categoriesRepository.SaveChanges();
        }

        #endregion


    }
}
