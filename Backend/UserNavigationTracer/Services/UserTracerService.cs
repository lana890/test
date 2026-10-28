
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Module.Core.Data;
using EskaCMS.UserNavigationTracer.Entites;
using EskaCMS.UserNavigationTracer.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EskaCMS.Core.Enums.GeneralEnums;

namespace EskaCMS.UserNavigationTracer.Services
{
    public class UserTracerService : IUserTracerService
    {
        public IRepository<UserVisit> _UserVisitRepo { get; set; }
        public IRepository<UserNavigation> _UserNavigationRepo { get; set; }
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private IWorkContext _workContext;

        private static readonly double  MAX_ESTIMATION_TIME_PER_VISITE = 15; // in minutes 

        public UserTracerService(IRepository<UserVisit> UserVisitRepo,
            IServiceScopeFactory serviceScopeFactory,
            IRepository<UserNavigation> UserNavigationRepo,
            IWorkContext workContext)
        {
            _UserVisitRepo = UserVisitRepo;
            _serviceScopeFactory = serviceScopeFactory;
            _UserNavigationRepo = UserNavigationRepo;
            _workContext = workContext;
        }


        public async Task<bool> ExecuteTakeNavigationSnapShot(string Slug , string SlugName,SlugsTypes SlugType,string IP,long PageId)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId =  _workContext.GetCurrentSiteId();
                var IPAdress =  _workContext.GetIPAddress();
                await TakeNavigationSnapShot(Slug, SlugName, IPAdress, SlugType, PageId, UserId, SiteId);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }



        #region Helper Functions For BGS
        private async Task<bool> TakeNavigationSnapShot(string Slug, string SlugName,string UserIP, SlugsTypes SlugType, long PageId, long UserId,long SiteId)
        {
            try
            {


                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var _UserVisitRepo = scope.ServiceProvider.GetRequiredService<IRepository<UserVisit>>();
                    var _UserNavigationRepo = scope.ServiceProvider.GetRequiredService<IRepository<UserNavigation>>();

                     long LastUserVisitId =0;

                    var LastSnapShot =await _UserNavigationRepo.Query()
                        .Where(u => u.SiteId == SiteId && u.UserId == UserId)
                        .OrderByDescending(u => u.Id)
                        .FirstOrDefaultAsync();

                    if(LastSnapShot == null)
                    {
                        UserVisit UserVisitObj = await AddUserVisit(UserId, SiteId, _UserVisitRepo);
                        LastUserVisitId = UserVisitObj.Id;
                    }
                    else
                    {
                        TimeSpan EstimationTime = DateTime.Now - LastSnapShot.CreationDate;

                        if (EstimationTime.TotalMinutes > MAX_ESTIMATION_TIME_PER_VISITE)
                        {
                            LastSnapShot.Period = TimeSpan.FromMinutes(15);

                            UserVisit UserVisitObj = await AddUserVisit(UserId, SiteId, _UserVisitRepo);
                            LastUserVisitId = UserVisitObj.Id;


                        }
                        else
                        {
                            LastSnapShot.Period = EstimationTime;

                            LastUserVisitId = LastSnapShot.VisitId;


                        }

                    }
                   


                    var UserNavigation = new UserNavigation()
                    {
                        SiteId = SiteId,
                        UserId = UserId,
                        Slug = Slug,
                        CreationDate = DateTime.Now,
                        VisitId = LastUserVisitId,
                        UserIp = UserIP,
                        SlugsType = SlugType,

                    };

                    _UserNavigationRepo.Add(UserNavigation);
                    await _UserNavigationRepo.SaveChangesAsync();


                    return true;
                }
            }
            catch (Exception e)
            {
                return false;
            }

        }

        private async Task<UserVisit> AddUserVisit(long UserId , long SiteId, IRepository<UserVisit> _UserVisitRepo)
        {
            try
            {
                var UserVisit = new UserVisit()
                {
                   CreationDate = DateTime.Now,
                    SiteId = SiteId,
                    UserId = UserId,


                };

                _UserVisitRepo.Add(UserVisit);

                await _UserVisitRepo.SaveChangesAsync();
                return UserVisit;
            }
            catch (Exception E)
            {
                throw E;
            }
        }
        #endregion

    }
}
