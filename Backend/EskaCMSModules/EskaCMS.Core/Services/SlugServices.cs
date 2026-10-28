using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.DataAccess.Data;

namespace EskaCMS.Core.Services
{
   

    public class SlugServices : ISlugServices
    {
        private readonly IWorkContext _workContext;
        private readonly EskaDCMSDbContext _context;
        private readonly IRepository<SlugInfo> _SlugRepository;

        public SlugServices(IWorkContext workContext, EskaDCMSDbContext context, IRepository<SlugInfo> SlugRepository)
        {
            _workContext = workContext;

            _context = context;
            _SlugRepository = SlugRepository;
        }


        public async Task<long?> Upsert(SlugManagementVm slugManagementVm)
        {
                try
                {

                    if (string.IsNullOrEmpty(slugManagementVm.Slug))
                    {
                        await DeleteSlugInfo(slugManagementVm.Id);

                        return null; // to add null FK
                    }


                    SlugInfo Slug = await _SlugRepository.Query()
                        .Where(x => x.Id == slugManagementVm.Id)
                        .FirstOrDefaultAsync();

                    if(Slug == null)
                    {
                       return await AddIfNotExsist(slugManagementVm);
                    }

                    // Guard: AddIfNotExsist already refuses to create a second row with the same
                    // Slug + SiteId + CultureId, but this update path used to overwrite blindly.
                    // That is how duplicate Core_SlugInfo rows were produced: a later
                    // AddIfNotExsist then picks one of the duplicates via FirstOrDefaultAsync and
                    // can hand it to a DIFFERENT post, which violates the unique index
                    // IX_Posts_CultureId_SiteId_SlugInfoId on save. Reject the rename instead.
                    // Same lookup shape and message as AddIfNotExsist so both paths agree;
                    // the row being edited is excluded so re-saving an unchanged slug still works.
                    var conflictingSlug = await _SlugRepository.Query()
                                                              .AsNoTracking()
                                                              .Where(x => x.Id != slugManagementVm.Id
                                                                      && x.Slug.ToLower() == slugManagementVm.Slug.ToLower()
                                                                      && x.SiteId == slugManagementVm.SiteId
                                                                      && x.CultureId == slugManagementVm.CultureId)
                                                              .FirstOrDefaultAsync();

                    if (conflictingSlug != null)
                    {
                        throw new Exception("Slug Is Already Exists In " + Enum.GetName(typeof(EEntity), Convert.ToInt32(conflictingSlug.Entity)));
                    }

                    long UserId = await _workContext.GetCurrentUserId();



                    Slug.Slug = slugManagementVm.Slug;
                    Slug.ModificationDate = DateTime.Now;
                    Slug.ModifiedById = UserId;
                    Slug.CultureId = slugManagementVm.CultureId;

                    await _SlugRepository.SaveChangesAsync();
                    return Slug.Id;

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            
        }

        public async Task<string> GetSlugById(long? id)
        {
            try
            {
                var Slug = await _SlugRepository.Query()
                                                .Where(x => x.Id == id)
                                                .Select(x => x.Slug)
                                                .AsNoTracking()
                                                .FirstOrDefaultAsync();

                return string.IsNullOrEmpty(Slug)?String.Empty:Slug;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<bool> DeleteSlugInfo(long id)
        {
            try
            {
                var Slug = await _SlugRepository.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
                if (Slug != null)
                {

                    _SlugRepository.Remove(Slug);
                    _SlugRepository.SaveChanges();
                }

                return true;

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<long?> AddIfNotExsist(SlugManagementVm slugManagementVm)
        {
            try
            {
                // if slug is null or empty return null to add null in the FK
                if (string.IsNullOrEmpty(slugManagementVm.Slug))
                {
                    return null;
                }


                long UserId = await _workContext.GetCurrentUserId();

                SlugInfo SlugItem = await _SlugRepository.Query()
                                                         .Where(x => x.Slug.ToLower() == slugManagementVm.Slug.ToLower()
                                                                 && x.SiteId == slugManagementVm.SiteId
                                                                 && x.CultureId == slugManagementVm.CultureId)
                                                         .FirstOrDefaultAsync();


                // if slug exsists return id if will use in same entity
                // don't worry if another object use same id the entity will throw slug already used exception
                if (SlugItem != null)
                {
                    if (SlugItem.Entity != slugManagementVm.Entity)
                    {

                        throw new Exception("Slug Is Already Exists In "+ Enum.GetName(typeof(EEntity),Convert.ToInt32( SlugItem.Entity)));
                    }
                    else
                    {
                        return SlugItem.Id;
                    }


                }

                SlugInfo Slug = new SlugInfo();

                Slug.SiteId = (slugManagementVm.SiteId != 0 && slugManagementVm.SiteId!=null) ? slugManagementVm.SiteId : null;
                Slug.CultureId = (slugManagementVm.CultureId!=0 && slugManagementVm.CultureId != null) ? slugManagementVm.CultureId :null;
                Slug.Slug = slugManagementVm.Slug;
                Slug.Entity = slugManagementVm.Entity;
                Slug.CreatedById = UserId;
                Slug.CreationDate = DateTimeOffset.Now;
                _SlugRepository.Add(Slug);
                await _SlugRepository.SaveChangesAsync();

                return Slug.Id;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> IsDuplicatedSlug(SlugManagementVm SlugInfo)
        {

            SlugInfo Slug = await _SlugRepository.Query().AsNoTracking()
                                               .Where(s => s.SiteId == SlugInfo.SiteId
                                                        && SlugInfo.CultureId == s.CultureId
                                                        && s.Slug == SlugInfo.Slug)
                                               .FirstOrDefaultAsync();
            
            if( Slug != null 
                && Slug.Entity != SlugInfo.Entity 
                ||(Slug.Id != SlugInfo.Id) && SlugInfo.Id !=0)
            {
                throw new Exception("Slug Is Already Exists In " + Enum.GetName(typeof(EEntity), Convert.ToInt32(Slug.Entity)));

            }

            return true;


        }

        public async Task<long> UpsertAndGetId(SlugManagementVm slugManagementVm)
        {
            try
            {
                long SlugId = (long) await Upsert(slugManagementVm);

                return SlugId;
            }
            catch (Exception ex)
            {

                throw ex;
            }
           
        }

        public async Task<SlugInfo> GetSlugDataBySlug(string Slug)
        {
            try
            {
                var CultureId = _workContext.GetCurrentCultureId();
                var SlugInfo = await  _SlugRepository.Query().Where(x => x.Slug.ToLower() == Slug &&  CultureId==x.CultureId).FirstOrDefaultAsync();
        
                return SlugInfo;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

    }
}
