using EskaCMS.Core.Entities;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.SitesAndCultures;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Sites.Models;
using EskaCMS.Sites.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Sites.Services
{
    public class SitePreferanceService : ISitePreferance
    {
        private readonly IRepository<SitePrefrances> _sitePreferanceRepository;
        private readonly IWorkContext _workContext;

        public SitePreferanceService(IRepository<SitePrefrances> sitePreferanceRepository, IWorkContext workContext)
        {
            _sitePreferanceRepository = sitePreferanceRepository;
            _workContext = workContext;
        }

        public async Task<bool> AddNewSitePreferences(CreateSitePreferanceViewModel sitePreferanceVM)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = _workContext.GetCurrentSiteId();
                await RemoveIfExist(SiteId);
                SitePrefrances SitePrefrances = new SitePrefrances();
                SitePrefrances.DateFormat = sitePreferanceVM.DateFormate;
                SitePrefrances.TimeFormat = sitePreferanceVM.TimeFormate;
                SitePrefrances.FontSize = sitePreferanceVM.FontSize;
                SitePrefrances.ThemeId = sitePreferanceVM.ThemeId;
                SitePrefrances.SiteId = SiteId;
                SitePrefrances.CreatedById = UserId;
                SitePrefrances.CreationDate = DateTimeOffset.Now;

                _sitePreferanceRepository.Add(SitePrefrances);
                await _sitePreferanceRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<bool> RemoveIfExist( long SiteId)
        {
            var SitePrefrances = await _sitePreferanceRepository.Query().Where(x => x.SiteId == SiteId).ToListAsync();

            if (SitePrefrances.Count > 0)
            {
                _sitePreferanceRepository.RemoveRange(SitePrefrances);

                await _sitePreferanceRepository.SaveChangesAsync();

            }
            return true;
        }

        public async Task<bool> UpdateSitePreferences(UpdateSitePreferanceVM sitePreferanceVM )
        {
            try
            {
              
                var SiteId = _workContext.GetCurrentSiteId();
                var userId =await _workContext.GetCurrentUserId();

                var SitePrefrances = await _sitePreferanceRepository.Query().Where(x => x.SiteId == SiteId).FirstOrDefaultAsync();
                if (SitePrefrances != null)
                {
                    SitePrefrances.DateFormat = sitePreferanceVM.DateFormate;
                    SitePrefrances.TimeFormat = sitePreferanceVM.TimeFormate;
                    SitePrefrances.FontSize = sitePreferanceVM.FontSize;
                    SitePrefrances.ThemeId = sitePreferanceVM.ThemeId;
                    SitePrefrances.ModifiedById = userId;
                    SitePrefrances.ModificationDate = DateTimeOffset.Now;
                    SitePrefrances.ThemeId = sitePreferanceVM.ThemeId;

                    _sitePreferanceRepository.Update(SitePrefrances);
                    await _sitePreferanceRepository.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<GetSitePreferanceVM> GetSitePreference()
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                string GUID = Guid.NewGuid().ToString("N");
                var PreferanceItem = await _sitePreferanceRepository.Query()
                    .Where(x =>  x.SiteId == SiteId)
                    .Include(x => x.Theme)
                    .ThenInclude(x => x.ImportedFiles)
                    .Include("Theme.ImportedFiles.Media")
                    .Include("Theme.ImportedFiles.Media.MediaDetails")
                    .Select(x => new GetSitePreferanceVM
                    {
                        Id = x.Id,
                        DateFormate = x.DateFormat,
                        TimeFormate = x.TimeFormat,
                        ThemeId = x.ThemeId,
                        FontSize = x.FontSize,
                        ThemeDetails =x.ThemeId.HasValue ? new ThemeVM()
                        {
                            Name = x.Theme.Name,
                            Colors = x.Theme.Colors,
                            Classes = x.Theme.Classes,
                            Fonts = x.Theme.Fonts,
                            Scripts = x.Theme.Scripts,
                            StyleUrl = "Resources/styles/themes/siteId_" + SiteId + "_themeId_" + x.ThemeId.ToString() + "/themeStyle.css?" + GUID + "=" + GUID,
                            ImportFiles = x.Theme.ImportedFiles.Select(y => new ImportedFilesVM
                            {
                                contentType = y.Media.ContentType,
                                FileUrl = y.Media.PublicUrl,
                                FileName = y.Media.MediaDetails
                                            .Select(md => md.Title)
                                            .FirstOrDefault()
                            }).ToList()
                        } : null

                    }).FirstOrDefaultAsync();

                return PreferanceItem;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
