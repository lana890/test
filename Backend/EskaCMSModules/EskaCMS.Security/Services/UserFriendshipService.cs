using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Security.Models.FriendshipModels;
using EskaCMS.Security.Services.Interface;
using EskaCMS.Security.Services.Interfaces;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.FriendshipEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Services
{
    public class UserFriendshipService : IUserFriendshipService
    {

        private readonly IWorkContext _workContext;
        private readonly IRepository<Friendships> _userFriendshipsRepository;
        private readonly IRepository<User> _userrepository;
        private readonly ISiteSettingsService _siteSettings;
        private readonly ITestNotifier _testNotifier;
        private readonly INotificationServices notificationServices;

        public UserFriendshipService(IWorkContext workContext,
                                    IRepository<Friendships> userFriendshipsRepository,
                                    IRepository<User> userrepository,
                                    ISiteSettingsService siteSettings,
                                    ITestNotifier testNotifier,
                                    INotificationServices notificationServices
                                    )
        {
            _workContext = workContext;
            _userFriendshipsRepository = userFriendshipsRepository;
            _userrepository = userrepository;
            _siteSettings = siteSettings;
            _testNotifier = testNotifier;
            this.notificationServices = notificationServices;
        }

        public async Task<bool> CreateFriendrequest(GetUserInfoVm model)
        {

            var currentUserId = await _workContext.GetCurrentUserId();

            var userId = DecryptedUserIdHelper(model.UserId);
            var siteId = _workContext.GetCurrentSiteId();

            if (_userFriendshipsRepository.Query().Any(x => ((x.ToId == currentUserId && x.FromId == userId) || (x.ToId == userId && x.FromId == currentUserId)) &&
                                                            ((x.Status == EFriendshipStatus.Accepted || x.Status == EFriendshipStatus.Requested)) &&
                                                            x.SiteId == siteId))
            {
                throw new Exception("There is pending request between you and this user");
            }

            var requestToBeAdded = new Friendships
            {
                FromId = currentUserId,
                Status = EFriendshipStatus.Requested,
                ToId = userId,
                CreationDate = DateTime.Now,
                SiteId = siteId
            };

            _userFriendshipsRepository.Add(requestToBeAdded);
            _userFriendshipsRepository.SaveChanges();

            var FromUser = await _userrepository.Query().AsNoTracking().Where(x => x.Id == currentUserId).FirstOrDefaultAsync();
            var ToUser = await _userrepository.Query().AsNoTracking().Where(x => x.Id == userId).FirstOrDefaultAsync();
         

            await notificationServices.SendMessage(new NotificationDto
            {

                Email = ToUser.Email,
                EmailTemplateId = model.EmailTemplateId,
                SiteId = siteId,
                FullName = FromUser.FullName,
                SenderFullName = FromUser.FullName,
                RecipientUserId = userId,
                ReciverFullName = ToUser.FullName,



            });

            return true;
        }

        public async Task<bool> ActionOnFriendRequest(string id, EFriendshipStatus action, string EmailTemplateId)
        {
            var currentUserId = await _workContext.GetCurrentUserId();

            var senderId = DecryptedUserIdHelper(id);
            var siteId = _workContext.GetCurrentSiteId();
            var request = _userFriendshipsRepository.Query()
                                                     .Where(x => (x.FromId == senderId && x.ToId == currentUserId) && x.Status == EFriendshipStatus.Requested && x.SiteId == siteId)
                                                     .Include(x => x.From)
                                                     .Include(x => x.To)
                                                     .FirstOrDefault();

            if (request is null)
            {
                return false;
            }

            request.StatusDate = DateTimeOffset.Now;

            if (action == EFriendshipStatus.Accepted)
            {
                request.AcceptedDate = DateTimeOffset.Now;
                await notificationServices.SendMessage(new NotificationDto
                {

                    Email = request.From.Email,
                    EmailTemplateId = EmailTemplateId,
                    SiteId = siteId,
                    FullName = request.To.FullName,
                    RecipientUserId = request.From.Id,
                    SenderFullName=request.To.FullName,
                    ReciverFullName=request.To.FullName,

                });
            }

            request.Status = action;

            _userFriendshipsRepository.SaveChanges();

            return true;
        }

        public async Task<bool> DeleteFriend(string id)
        {
            var currentUserId = await _workContext.GetCurrentUserId();

            var friendId = DecryptedUserIdHelper(id);

            var siteId = _workContext.GetCurrentSiteId();
            var request = _userFriendshipsRepository.Query()
                                                     .Where(x => ((x.ToId == currentUserId && x.FromId == friendId) || (x.ToId == friendId && x.FromId == currentUserId)) && x.Status == EFriendshipStatus.Accepted && x.SiteId == siteId)
                                                     .FirstOrDefault();

            if (request is null)
            {
                return false;
            }

            request.StatusDate = DateTimeOffset.Now;



            request.Status = EFriendshipStatus.Deleted;

            _userFriendshipsRepository.SaveChanges();

            return true;
        }

        public async Task<bool> CancelSendingRequest(string id)
        {
            var currentUserId = await _workContext.GetCurrentUserId();

            var reciverId = DecryptedUserIdHelper(id);
            var siteId = _workContext.GetCurrentSiteId();
            var request = _userFriendshipsRepository.Query()
                                                     .Where(x => (x.FromId == currentUserId && x.ToId == reciverId) && x.Status == EFriendshipStatus.Requested && x.SiteId == siteId)
                                                     .FirstOrDefault();

            if (request is null)
            {
                return false;
            }

            request.StatusDate = DateTimeOffset.Now;



            request.Status = EFriendshipStatus.Deleted;

            _userFriendshipsRepository.SaveChanges();

            return true;
        }

        public async Task<PaginationResult<UserRequestsDto>> GetAllFriendRequest(SmartTableParam model)
        {

            var currentUserId = await _workContext.GetCurrentUserId();

            var siteId = _workContext.GetCurrentSiteId();

            var allRequest = _userFriendshipsRepository.Query()
                                                       .Where(x => x.ToId == currentUserId && x.Status == EFriendshipStatus.Requested && x.SiteId == siteId)
                                                       .Include(x => x.From)
                                                       .AsNoTracking();


            if (model.Search.PredicateObject != null)
            {
                dynamic search = model.Search.PredicateObject;



                if (search.UserName != null)
                {
                    string UserName = search.UserName;
                    allRequest = allRequest.Where(x => x.From.NormalizedUserName.Contains(UserName.ToUpper()));
                }

                if (search.Email != null)
                {
                    string Email = search.Email;
                    allRequest = allRequest.Where(x => x.From.NormalizedEmail.Contains(Email.ToUpper()));
                }

                if (search.FullName != null)
                {
                    string FullName = search.FullName;
                    allRequest = allRequest.Where(x => x.From.FullName.ToUpper().Contains(FullName.ToUpper()));
                }

            }


            var result = await allRequest.Select(x => new UserRequestsDto
            {
                Id = x.Id,
                UserName = x.From.UserName,
                UserId = EncryptDecryptHelper.Encrypt(x.FromId.ToString()),
                Email = x.From.Email,
                FullName = x.From.FullName,
                Image = x.From.Image,
                CreationDate = x.CreationDate



            }).PaginateAsync(model.Pagination.Number, model.Pagination.Start);

            return result;
        }


        public async Task<PaginationResult<UserRequestsDto>> GetAllSentFriendRequest(SmartTableParam model)
        {

            var currentUserId = await _workContext.GetCurrentUserId();

            var siteId = _workContext.GetCurrentSiteId();

            var allRequest = _userFriendshipsRepository.Query()
                                                       .Where(x => x.FromId == currentUserId && x.Status == EFriendshipStatus.Requested && x.SiteId == siteId)
                                                       .Include(x => x.To)
                                                       .AsNoTracking();


            if (model.Search.PredicateObject != null)
            {
                dynamic search = model.Search.PredicateObject;



                if (search.UserName != null)
                {
                    string UserName = search.UserName;
                    allRequest = allRequest.Where(x => x.To.NormalizedUserName.Contains(UserName.ToUpper()));
                }

                if (search.Email != null)
                {
                    string Email = search.Email;
                    allRequest = allRequest.Where(x => x.To.NormalizedEmail.Contains(Email.ToUpper()));
                }

                if (search.FullName != null)
                {
                    string FullName = search.FullName;
                    allRequest = allRequest.Where(x => x.To.FullName.ToUpper().Contains(FullName.ToUpper()));
                }

            }



            var result = await allRequest.Select(x => new UserRequestsDto
            {
                Id = x.Id,
                UserName = x.To.UserName,
                UserId = EncryptDecryptHelper.Encrypt(x.ToId.ToString()),
                Email = x.To.Email,
                FullName = x.To.FullName,
                Image = x.To.Image,
                CreationDate = x.CreationDate



            }).PaginateAsync(model.Pagination.Number, model.Pagination.Start);

            return result;
        }

        public async Task<PaginationResult<UserFriendsDto>> GetAllFriends(SmartTableParam model)
        {

            var currentUserId = await _workContext.GetCurrentUserId();

            var siteId = _workContext.GetCurrentSiteId();

            var allRequest = _userFriendshipsRepository.Query()
                                                       .Where(x => (x.ToId == currentUserId || x.FromId == currentUserId) && x.Status == EFriendshipStatus.Accepted && x.SiteId == siteId)
                                                       .Include(x => x.From)
                                                       .Include(x => x.To)
                                                       .AsNoTracking();

            if (model.Search.PredicateObject != null)
            {
                dynamic search = model.Search.PredicateObject;



                if (search.UserName != null)
                {
                    string UserName = search.UserName;
                    allRequest = allRequest.Where(x => x.To.NormalizedUserName.Contains(UserName.ToUpper()) || x.From.NormalizedUserName.Contains(UserName.ToUpper()));
                }

                if (search.Email != null)
                {
                    string Email = search.Email;
                    allRequest = allRequest.Where(x => x.To.NormalizedEmail.Contains(Email.ToUpper()) || x.From.NormalizedEmail.Contains(Email.ToUpper()));
                }

                if (search.FullName != null)
                {
                    string FullName = search.FullName;
                    allRequest = allRequest.Where(x => x.To.FullName.ToUpper().Contains(FullName.ToUpper()) || x.From.FullName.ToUpper().Contains(FullName.ToUpper()));
                }

            }



            var result = await allRequest.Select(x => new UserFriendsDto
            {
                Id = x.FromId == currentUserId ? x.To.Id : x.From.Id,
                UserName = x.FromId == currentUserId ? x.To.UserName : x.From.UserName,
                UserId = x.FromId == currentUserId ? EncryptDecryptHelper.Encrypt(x.To.Id.ToString()) : EncryptDecryptHelper.Encrypt(x.From.Id.ToString()),
                Email = x.FromId == currentUserId ? x.To.Email : x.From.Email,
                FullName = x.FromId == currentUserId ? x.To.FullName : x.From.FullName,
                Image = x.FromId == currentUserId ? x.To.Image : x.From.Image,
                CreationDate = x.CreationDate,
                AcceptDate = x.AcceptedDate,
                StatusDate = x.StatusDate



            }).PaginateAsync(model.Pagination.Number, model.Pagination.Start);

            return result;
        }


        public async Task<UserFriendShipDto> GetUserProfile(string id)
        {
            var currentUserId = await _workContext.GetCurrentUserId();

            var userId = DecryptedUserIdHelper(id);
            var siteId = _workContext.GetCurrentSiteId();
            var userData = _userrepository.Query().AsNoTracking()
                                          .Where(x => x.Id == userId && x.UserSites.Any(x => x.SiteId == siteId && x.UserId == userId))
                                          .Include(x => x.UserOptions)
                                          .Include(x => x.UserFiles)
                                          .Include("UserFiles.Media")
                                          .FirstOrDefault();


            var allViewerData = await GetAllUserFriendShipData(currentUserId, siteId);

            var status = GetFriendshipStatus(allViewerData, currentUserId, userId);

            var userProfile = new UserFriendShipDto
            {
                UserId = EncryptDecryptHelper.Encrypt(userData.Id.ToString()),
                UserOptions = userData.UserOptions.Select(x => new UserOptionsVM
                {
                    Id = x.Id,
                    ControlType = x.ControlType,
                    PropertyName = x.PropertyName,
                    Value = x.Value,
                    IsEditable = x.IsEditable,
                    IsVisible = x.IsVisible
                }).ToList(),
                CoverImage = userData.UserFiles.OrderBy(x => x.Id).LastOrDefault()?.Media?.PublicUrl,
                Email = userData.Email,
                FullName = userData.FullName,
                ProfileImage = userData.Image,
                Status = status,
                UserName = userData.UserName,

            };


            return userProfile;
        }


        public async Task<PaginationResult<UserFriendShipDto>> GetAllUsers(SmartSearchTableParam<AllUsersVM> model)
        {

            var currentUserId = await _workContext.GetCurrentUserId();

            var siteId = _workContext.GetCurrentSiteId();

            var allViewerData = await GetAllUserFriendShipData(currentUserId, siteId);
           
            var allUsers = _userrepository.Query().Include(x => x.UserOptions)
                                          .Where(x => x.Id != currentUserId &&
                                                 x.UserSites.Any(x => x.SiteId == siteId) &&
                                                 x.Status == EStatus.Active)
                                          .Where(x=>x.Type!=EUsersTypes.GuestClient)
                                          .AsNoTracking();






            if (!string.IsNullOrEmpty(model.Search.UserName))
            {
                string UserName = model.Search.UserName;
                allUsers = allUsers.Where(x => x.NormalizedUserName.Contains(UserName.ToUpper()));
            }

            if (!string.IsNullOrEmpty(model.Search.Email))
            {
                string Email = model.Search.Email;
                allUsers = allUsers.Where(x => x.NormalizedEmail.Contains(Email.ToUpper()));
            }

            if (!string.IsNullOrEmpty(model.Search.FullName))
            {
                string FullName = model.Search.FullName;
                allUsers = allUsers.Where(x => x.FullName.ToUpper().Contains(FullName.ToUpper()));
            }

            if (model.Search.RoleIds != null && model.Search.RoleIds.Any())
            {

                allUsers = allUsers.Where(x => x.UserRoles.Any(y => model.Search.RoleIds.Contains(y.RoleId)));
            }





            var result = await allUsers.Select(x => new UserFriendShipDto
            {
                Id = x.Id,
                UserId = EncryptDecryptHelper.Encrypt(x.Id.ToString()),
                UserName = x.UserName,
                Email = x.Email,
                ProfileImage = x.Image,
                FullName = x.FullName,
                Status = GetFriendshipStatus(allViewerData, currentUserId, x.Id),
                AvailabilityStatus = x.UserOptions.Where(x => x.NormalizedPropertyName == "AVAILABILITYSTATUS").Count() > 0 ? Convert.ToInt32(x.UserOptions.Where(x => x.NormalizedPropertyName == "AVAILABILITYSTATUS").Select(x => x.Value).FirstOrDefault()) : null
            }).PaginateAsync(model.Pagination.Number, model.Pagination.Start);



            return result;
        }


        #region Helpers


        private long DecryptedUserIdHelper(string Id)
        {

            var decryptedUserId = EncryptDecryptHelper.Decrypt(Id);
            var userId = long.Parse(decryptedUserId);

            return userId;
        }

        private async Task<List<Friendships>> GetAllUserFriendShipData(long userId, long siteId)
        {
            var result = _userFriendshipsRepository.Query()
                                                   .Where(x => x.SiteId == siteId && (x.ToId == userId || x.FromId == userId) && x.Status != EFriendshipStatus.Deleted)
                                                   .AsNoTracking()
                                                   .ToList();


            return result;

        }


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


        private async Task<bool> SendNotification(User sender, User receipt, long siteId)
        {
            var settings = await _siteSettings.GetSiteSettingsByModule("FriendShipSettings", siteId);

            if (settings == null)
                return false;

            const string acceptFriendShipTemplateId = "AcceptFriendRequestTemplateId";

            var emailTemplateId = settings.Where(x => x.Id.ToLower() == acceptFriendShipTemplateId.ToLower()).FirstOrDefault()?.Value;

            var Parameters = new List<EmailParamsVM>();
            Parameters.Add(new EmailParamsVM
            {
                Param = "[Name]",
                Value = receipt.FullName

            });


            NotificationObject notificationObject = new NotificationObject
            {
                MessageBody = string.Empty,
                MessageTitle = "Friend Request Accepted",
                SendVia = SendBy.Email,
                SendValue = new List<string> { sender.Email },
                NotificationType = NotificationType.Email,
                EmailOptions = new EmailSenderVM
                {
                    Emails = new List<string>(),
                    TemplateId = emailTemplateId,
                    Params = Parameters
                },

            };

            await _testNotifier.SendMessageAsync(NotificationType.Email, null, notificationObject, siteId);

            return true;


        }


        public async Task<List<UserFriendDto>> GetFriendsByUserId(long UserId, long SiteId)
        {
            try
            {

                var allRequest = await _userFriendshipsRepository.Query()
                                                           .Where(x => (x.ToId == UserId || x.FromId == UserId) && x.Status == EFriendshipStatus.Accepted && x.SiteId == SiteId)
                                                           .AsNoTracking()
                                                           .Select(x => new UserFriendDto
                                                           {

                                                               UserId = x.FromId == UserId ? x.To.Id : x.From.Id,
                                                               UserName = x.FromId == UserId ? x.To.UserName : x.From.UserName


                                                           }).ToListAsync();

                return allRequest;
            }
            catch (Exception ex)
            {

                throw ex;
            }


        }

        public async Task<bool> RequestAndConfirmFriendship(GetUserInfoVm model)
        {
            try
            {
                var currentUserId = await _workContext.GetCurrentUserId();

                var userId = DecryptedUserIdHelper(model.UserId);
                var siteId = _workContext.GetCurrentSiteId();

                if (_userFriendshipsRepository.Query().Any(x => ((x.ToId == currentUserId && x.FromId == userId) || (x.ToId == userId && x.FromId == currentUserId)) &&
                                                                ((x.Status == EFriendshipStatus.Accepted || x.Status == EFriendshipStatus.Requested)) &&
                                                                x.SiteId == siteId))
                {
                    return false;
                }

                var requestToBeAdded = new Friendships
                {
                    FromId = currentUserId,
                    Status = EFriendshipStatus.Accepted,
                    ToId = userId,
                    CreationDate = DateTime.Now,
                    SiteId = siteId
                };

                _userFriendshipsRepository.Add(requestToBeAdded);
                _userFriendshipsRepository.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #endregion
    }
}
