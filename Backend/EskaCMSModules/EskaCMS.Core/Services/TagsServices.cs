using EskaCMS.Core.Areas.Core.ViewModels.Tags;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Shared;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Services
{
    public class TagsServices : ITagsServices
    {
        private readonly IRepository<Tags> tagsRepository;
        private readonly IRepository<EntityTags> entityTagsRepository;
        private readonly IWorkContext workContext;

        public TagsServices(

            IRepository<Tags> TagsRepository,
            IRepository<EntityTags> EntityTagsRepository,
            IWorkContext workContext)
        {
            tagsRepository = TagsRepository;
            entityTagsRepository = EntityTagsRepository;
            this.workContext = workContext;
        }


        public async Task<bool> CreateTag(TagsEntityViewModel model)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();
                var UserId = await workContext.GetCurrentUserId();
                List<long> tagsId = new List<long>();
                foreach (var tag in model.tags)
                {
                    var ExistTagName = await tagsRepository.Query().Where(x => x.SiteId == SiteId && x.Title == tag.Title).FirstOrDefaultAsync();

                    if (ExistTagName == null)
                    {
                        var Tag = new Tags
                        {
                            Title = tag.Title,
                            SiteId = SiteId,
                            CreatedById = UserId,
                            CreationDate = DateTimeOffset.Now,
                        };
                        tagsRepository.Add(Tag);
                        await tagsRepository.SaveChangesAsync();
                        tagsId.Add(Tag.Id);

                    }
                    else
                    {
                        tagsId.Add(ExistTagName.Id);
                    }
                }



                await AddTagEntitys(model, tagsId);
                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<bool> UpdateTag(TagsEntityViewModel model)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();
                var UserId = await workContext.GetCurrentUserId();
                List<long> tagsId = new List<long>();

                var RemoveEntityTags = await entityTagsRepository.Query().Where(x => x.EntityId == model.EntityId && x.EntityType == model.EntityType).ToListAsync();

                entityTagsRepository.RemoveRange(RemoveEntityTags);
                await entityTagsRepository.SaveChangesAsync();

                foreach (var tag in model.tags)
                {
                    var ExistTagName = await tagsRepository.Query().Where(x => x.SiteId == SiteId && x.Title == tag.Title).FirstOrDefaultAsync();

                    if (ExistTagName == null)
                    {
                        var Tag = new Tags
                        {
                            Title = tag.Title,
                            SiteId = SiteId,
                            CreatedById = UserId,
                            CreationDate = DateTimeOffset.Now,
                        };
                        tagsRepository.Add(Tag);
                        await tagsRepository.SaveChangesAsync();
                        tagsId.Add(Tag.Id);

                    }
                    else
                    {
                        tagsId.Add(ExistTagName.Id);
                    }

                }
                await AddTagEntitys(model, tagsId);

                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<GetAllTagsDto>> GetTags(SearchViewModel model)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();
                var Tags = await tagsRepository.Query()
                                              .Include(x => x.EntityTags)
                                              .Where(x => x.SiteId == SiteId)
                                              .Where(x => string.IsNullOrEmpty(model.SearchValue) || x.Title.Contains(model.SearchValue))
                                              .Where(x => model.EntityType == null || x.EntityTags.Any(y => y.EntityType == model.EntityType))
                                              .Select(x => new GetAllTagsDto
                                              {

                                                  Id = x.Id,
                                                  Title = x.Title,
                                                  TagsEntities = x.EntityTags.Where(x => model.EntityType == null || x.EntityType == model.EntityType).Select(x => new TagsEntityViewModel
                                                  {
                                                      EntityId = x.Id,
                                                      EntityType = x.EntityType,

                                                  }).ToList()

                                              }).ToListAsync();

                return Tags;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<List<GetTagsViewModel>> GetTagsByEntityType(SearchEntitesViewModel model)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();

                var Query = await entityTagsRepository.Query()
                                                             .Include(x => x.Tags)
                                                             .Where(x => x.Tags.SiteId == SiteId)
                                                             .Where(x => x.EntityType == model.EntityType)
                                                             .Where(x => model.EntityIds == null || model.EntityIds.Contains(x.EntityId))
                                                             .Select(x => new GetTagsViewModel
                                                             {
                                                                 EntityId = x.EntityId,
                                                                 EntityType = x.EntityType,
                                                                 Title = x.Tags.Title,
                                                                 TagsId = x.TagsId

                                                             }).ToListAsync();

                var DistinctTags = Query.DistinctBy(x => x.TagsId).ToList();

                return DistinctTags;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<long>> FetchTagsByEntityAsync(List<long> EntityIds, EEntity entityType, long[]? TagsId = null)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();

                var Query = await entityTagsRepository.Query()
                                                             .Include(x => x.Tags)
                                                             .Where(x => TagsId.Any(u => u == x.TagsId))
                                                             .Where(x => x.EntityType == entityType)
                                                             .Where(x => EntityIds.Any(u => u == x.EntityId))
                                                             .Select(x => x.EntityId)
                                                             .ToListAsync();

                return Query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<Dictionary<long, GetTagsViewModel>> GetFirstTagByEntityIds(SearchEntitesViewModel model)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();

                var idsToFetch = (model.EntityIds ?? new List<long>())
                    .Append(1L)  
                    .Distinct()
                    .ToList();

                var query = await entityTagsRepository.Query()
                    .Include(x => x.Tags)
                    .Where(x => x.Tags.SiteId == SiteId)
                    .Where(x => x.EntityType == model.EntityType)  
                    .Where(x => idsToFetch.Contains(x.EntityId))
                    .Select(x => new GetTagsViewModel
                    {
                        EntityId = x.EntityId,
                        EntityType = x.EntityType,
                        Title = x.Tags.Title,
                        TagsId = x.TagsId
                    })
                    .ToListAsync();

                var tagsByEntityId = query
                    .GroupBy(x => x.EntityId)
                    .ToDictionary(g => g.Key, g => g.First());

                tagsByEntityId.TryGetValue(1L, out var defaultTag);

                var result = new Dictionary<long, GetTagsViewModel>();

                foreach (var productId in model.EntityIds ?? new List<long>())
                {
                    if (tagsByEntityId.TryGetValue(productId, out var ownTag))
                        result[productId] = ownTag;
                    else if (defaultTag != null)
                        result[productId] = defaultTag;
                }

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #region Helpers

        private async Task AddTagEntitys(TagsEntityViewModel model, List<long> TagsIds)
        {
            try
            {
                var EntityTag = TagsIds.ConvertAll(x => new EntityTags
                {
                    EntityId = model.EntityId,
                    TagsId = x,
                    EntityType = model.EntityType
                });

                entityTagsRepository.AddRange(EntityTag);
                await entityTagsRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }




        #endregion
    }
}
