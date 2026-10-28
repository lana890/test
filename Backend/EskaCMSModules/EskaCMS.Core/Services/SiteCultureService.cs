using AutoMapper;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Interfaces;
using EskaCMS.Core.Models.SitesCultures;
using EskaCMS.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Helpers;

namespace EskaCMS.Core.Services
{
    public class SiteCultureService : ISiteCultureService
    {
        private readonly IRepository<SitesCultures> _SitesCulturesRepository;
        private readonly IWorkContext _workContext;
        private readonly IMapper _mapper;


        public SiteCultureService(
            IRepository<SitesCultures> SitesCulturesRepository,
            IWorkContext workContext,
            IMapper mapper)
        {
            _workContext = workContext;
            _SitesCulturesRepository = SitesCulturesRepository;
            _mapper = mapper;
        }

        public async Task<bool> Add(SitesCulturesViewModel culture)
        {

            long UserId = await _workContext.GetCurrentUserId();
            List<SitesCultures> mappedCultures = new List<SitesCultures>();
            SitesCultures sitesCulture = new SitesCultures
            {
                CreationDate = DateTimeOffset.Now,
                CultureId = culture.CultureCodeId,
                SiteId = culture.SiteId,
                CreatedById = UserId,
                Status = EStatus.Active
            };
            _SitesCulturesRepository.Add(sitesCulture);
            await _SitesCulturesRepository.SaveChangesAsync();

            return true;

        }
        public async Task<bool> AddSiteCultures(List<SitesCulturesViewModel> Cultures)
        {
            long UserId = await _workContext.GetCurrentUserId();
            List<SitesCultures> mappedCultures = new List<SitesCultures>();// _mapper.Map<List<SitesCultures>>(Cultures);
            foreach (var culture in Cultures)
            {
                SitesCultures sitesCulture = new SitesCultures
                {
                    CreationDate = DateTimeOffset.Now,
                    CultureId = culture.CultureCodeId,
                    SiteId = culture.SiteId,
                    CreatedById = UserId,
                    Status=EStatus.Active
                };
                _SitesCulturesRepository.Add(sitesCulture);
                await _SitesCulturesRepository.SaveChangesAsync();

            }

            return true;
        }
        public async Task<bool> Edit(SitesCulturesViewModel SiteCulture)
        {
            try
            {
                long UserId = await _workContext.GetCurrentUserId();

                SitesCultures siteCulture = _SitesCulturesRepository.Query().FirstOrDefault(x => x.Id == SiteCulture.Id);
                siteCulture.CultureId = SiteCulture.CultureCodeId;
                siteCulture.ModificationDate = DateTime.Now;
                siteCulture.ModifiedById = UserId;

                _SitesCulturesRepository.Update(siteCulture);
                _SitesCulturesRepository.SaveChanges();

                return true;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<bool> DeleteSiteCulture(long id)
        {
            try
            {
                long UserId = await _workContext.GetCurrentUserId();
                SitesCultures siteCulture = _SitesCulturesRepository.Query().FirstOrDefault(x => x.Id == id);
                siteCulture.Status = EStatus.Deleted;
                siteCulture.ModifiedById = UserId;
                siteCulture.ModificationDate = DateTimeOffset.Now;
                await _SitesCulturesRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        public async Task<List<SitesCulturesGetAllVM>> GetSiteCultures(long? SiteId)
        {
            try
            {
                if(SiteId == null)
                    SiteId = await _workContext.GetCurrentSiteIdAsync();

                 var Sites =await _SitesCulturesRepository.Query().Where(x => x.SiteId == SiteId && x.Status!=EStatus.Deleted).Select(s => new SitesCulturesGetAllVM
                {
                    Id=s.Id,
                    CreationDate = s.CreationDate,
                    SiteId = s.SiteId,
                    CultureCodeId = s.CultureId,
                    CreatedById = s.CreatedById,
                    Culture = s.CultureId
                })
                    .ToListAsync();

                return  Sites;


            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public SitesCulturesGetAllVM GetCultureById(long Id)
        {
            try
            {
                
                var Culture =  _SitesCulturesRepository.Query().Where(x => x.Id == Id && x.Status != EStatus.Deleted).Select(s => new SitesCulturesGetAllVM
                {
                    Id = s.Id,
                    CreationDate = s.CreationDate,
                    SiteId = s.SiteId,
                    CultureCodeId = s.CultureId,
                    CreatedById = s.CreatedById,
                    Culture = s.CultureId
                })
                   .FirstOrDefault();

                return Culture;


            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        public async Task<bool> AddCssStyleToSiteCultures(long CultureId, string CssStyle, long SiteId)
        {
            try
            {
                    var SiteCultureItem = await _SitesCulturesRepository.Query().Where(x => x.Id == CultureId && x.SiteId == SiteId).FirstOrDefaultAsync();
                    SiteCultureItem.CssStyle = CssStyle;
                    _SitesCulturesRepository.Update(SiteCultureItem);
                string Folder = "wwwroot/Resources/styles/siteId_" + SiteId + "_cultureId_" + CultureId;

                var minifiedCss = FileHelper.MinifyCssFile(cssContent: CssStyle);
                FileHelper.UpsertFile(Content: minifiedCss, Name: "cultureStyle", Path: Folder, type: "css");
                await _SitesCulturesRepository.SaveChangesAsync();
                    return true;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


       

        public async Task<string> GetCssStyle(long CultureId)
        {
            try
            {
                var SiteCultureItem = await _SitesCulturesRepository.Query().Where(x => x.Id == CultureId).FirstOrDefaultAsync();
                
                 return SiteCultureItem.CssStyle;

               
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<long> GetDefaultSiteCulture (long siteId)
        {
            try
            {
                const string CultureId = "en-US";
              var siteCulture = await _SitesCulturesRepository.Query()
                                                              .Where(x=>x.SiteId == siteId && x.Status ==EStatus.Published && x.CultureId.Contains(CultureId))
                                                              .FirstOrDefaultAsync();
               if(siteCulture is not null)
                    return siteCulture.Id;

                return 0;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }





    }
}
