using EskaCMS.Core.Entities;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Services
{
    public class UserPreferanceService : IUserPreferancese
    {
        private readonly IRepository<UserPreferance> _userPreferanceRepository;
        private readonly IWorkContext _workContext;

        public UserPreferanceService(IRepository<UserPreferance> userPreferanceRepository, IWorkContext workContext)
        {
            _userPreferanceRepository = userPreferanceRepository;
            _workContext = workContext;
        }

        public async Task<bool> AddNewUserPreferences(CreateUserPreferanceVM userPreferanceVM)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = _workContext.GetCurrentSiteId();
                await RemoveIfExist(UserId, SiteId);
                UserPreferance userPreferance = new UserPreferance();
                userPreferance.UserId = userPreferanceVM.UserId.HasValue ? userPreferanceVM.UserId.Value : UserId;
                userPreferance.DateFormat = userPreferanceVM.DateFormate;
                userPreferance.TimeFormat = userPreferanceVM.TimeFormate;
                userPreferance.FontSize = userPreferanceVM.FontSize;
                userPreferance.ThemeId = userPreferanceVM.ThemeId;
                userPreferance.SiteId = SiteId;

                _userPreferanceRepository.Add(userPreferance);
                await _userPreferanceRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<bool> RemoveIfExist(long userId, long SiteId)
        {
            var userPreferance = await _userPreferanceRepository.Query().Where(x => x.UserId == userId && x.SiteId == SiteId).ToListAsync();

            if (userPreferance.Count > 0)
            {
                _userPreferanceRepository.RemoveRange(userPreferance);

                await _userPreferanceRepository.SaveChangesAsync();

            }
            return true;
        }

        public async Task<bool> UpdateUserPreferences(UpdateUserPreferanceVM userPreferanceVM , long userId)
        {
            try
            {
              
                var SiteId = _workContext.GetCurrentSiteId();

                var userPreferance = await _userPreferanceRepository.Query().Where(x => x.UserId == userId && x.SiteId == SiteId).FirstOrDefaultAsync();
                if (userPreferance != null)
                {
                    userPreferance.DateFormat = userPreferanceVM.DateFormate;
                    userPreferance.TimeFormat = userPreferanceVM.TimeFormate;
                    userPreferance.FontSize = userPreferanceVM.FontSize;
                    userPreferance.ThemeId = userPreferanceVM.ThemeId;

                    _userPreferanceRepository.Update(userPreferance);
                    await _userPreferanceRepository.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<GetUserPreferanceVM> GetUserPreferencesByUserId(long userId)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                string GUID = Guid.NewGuid().ToString("N");
                var PreferanceItem = await _userPreferanceRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.UserId == userId && x.SiteId == SiteId)
                    .Include(x => x.Theme)
                    .ThenInclude(x => x.ImportedFiles)
                    .Include("Theme.ImportedFiles.Media")
                    .Include("Theme.ImportedFiles.Media.MediaDetails")
                    .Select(x => new GetUserPreferanceVM
                    {
                        Id = x.Id,
                        UserId = x.UserId,
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
