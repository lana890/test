using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Media.Models;
using EskaCMS.Media.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.DataAccess.Data;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Media.Services
{
    public class UserMediaService : IUserMediaService
    {
        private readonly EskaDCMSDbContext _context;
        private readonly IWorkContext _workContext;
        private readonly IRepository<UsersSites> _userSitesRepository;
        private readonly IRepository<UserFiles> _userFilesRepository;
        private readonly IMediaService _mediaService;
        private readonly IRepository<User> _userrepository;



        public UserMediaService(EskaDCMSDbContext context, IWorkContext workContext, IRepository<UsersSites> userSitesRepository,
             IRepository<UserFiles> userFilesRepository, IMediaService mediaService, IRepository<User> userrepository
)
        {
            _context = context;
            _workContext = workContext;
            _userSitesRepository = userSitesRepository;
            _userFilesRepository = userFilesRepository;
            _mediaService = mediaService;
            _userrepository = userrepository;   
        }

        public async Task<bool> SaveUserMediaAsync(UserFilesVM userFilesVM)
        {
            if (userFilesVM.MediaIds != null)
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var siteId = _workContext.GetCurrentSiteId();
                        var userId = await _workContext.GetCurrentUserId();


                        foreach (long mediaId in userFilesVM.MediaIds)
                        {
                            UserFiles userFilesEntity = new UserFiles();
                            userFilesEntity.UserId = userId;
                            userFilesEntity.CreatedById = userId;
                            userFilesEntity.MediaId = mediaId;
                            _userFilesRepository.Add(userFilesEntity);
                            await _userFilesRepository.SaveChangesAsync();
                        }

                        await transaction.CommitAsync();

                    }
                    catch (Exception exc)
                    {
                        await transaction.RollbackAsync();
                        throw exc;
                    }
                });
                return true;
            }
            return true;
        }

        public async Task<List<UserFileVM>> GetUserMedia(string userName)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var currentUser = await _workContext.GetCurrentUser();
            userName = userName.ToLower();
            long targerUserId;
            if (currentUser.Type == EUsersTypes.Admin)
            {
                var targetUser = await _userrepository.Query().AsNoTracking()
                    .Include(x => x.UserSites)
                    .Where(x => x.UserName.ToLower() == userName
                    && x.UserSites.Any(s => s.SiteId == siteId))
                    .FirstOrDefaultAsync();
                if (targetUser == null)
                {
                    throw new ArgumentException($"User doesn't exists '{userName}'");
                }
                targerUserId = targetUser.Id;
            }
            else
            {
                if (currentUser.UserName.ToLower() != userName.ToLower())
                {
                    throw new UnauthorizedAccessException("You aren't authoreized to access files");
                }
                targerUserId = currentUser.Id;
            }
            List<UserFileVM> UserFiles = await _userFilesRepository.Query().Include(x => x.Media).Include(x => x.User).ThenInclude(x => x.UserSites).Where(x =>
                    x.User.UserSites.Any(y => y.SiteId == siteId && y.UserId == targerUserId)
                 ).Select(x => new UserFileVM
                 {
                     Id = x.Id,
                     FileName = x.Media.Filename,
                     PublicUrl = x.Media.PublicUrl,
                     ContentType = x.Media.ContentType
                 }).ToListAsync();
            return UserFiles;
        }

        public async Task<bool> DeleteUserFile(long Id)
        {
            var userId = await _workContext.GetCurrentUserId();
            var UserFile = await _userFilesRepository.Query().Where(x => x.Id == Id && x.UserId == userId).FirstOrDefaultAsync();
            if (UserFile != null)
            {
                _userFilesRepository.Remove(UserFile);
                await _userFilesRepository.SaveChangesAsync();
                await _mediaService.DeleteMedia(UserFile.MediaId);
            }
            return true;
        }

    }
}
