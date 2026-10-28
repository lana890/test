using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.UserDashboard;
using EskaCMS.Infrastructure.Data;
using EskaCMS.UserDashboard.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Engine.UserManagment.BGS
{
    public class CurrentUsersBackgroundServices : BaseBackgroundService
    {
        public readonly ILogger<CurrentUsersBackgroundServices> _logger;
        private readonly System.IServiceProvider _serviceProvider;
        private IConfiguration _Configuration;


        public CurrentUsersBackgroundServices(ILogger<CurrentUsersBackgroundServices> logger,
            System.IServiceProvider serviceProvider,
            IConfiguration Configuration
            )
           : base(nameof(CurrentUsersBackgroundServices), logger, serviceProvider, Configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _Configuration = Configuration;
        }


        public override async Task<bool> Execute()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var currentLoggedInUsers = scope.ServiceProvider.GetRequiredService<IRepositoryWithTypedId<CurrentLoggedInUsers, string>>();
                var userNavigations = scope.ServiceProvider.GetRequiredService<IRepository<UserNavigations>>();

                var ConditionTime = DateTimeOffset.Now.AddHours(-24);
                var OldSessions = await currentLoggedInUsers.Query()
                                                                .Where(x => x.LoginDate < ConditionTime && x.TypeLogin == ETypeLogin.Web)
                                                                .ToListAsync();



                var SessionsToBeRemoved = new List<CurrentLoggedInUsers>();

                foreach (var session in OldSessions)
                {
                    if (await IsInactiveSession(session, userNavigations))
                    {
                        SessionsToBeRemoved.Add(session);

                    }
                }


                if (SessionsToBeRemoved.Any())
                    currentLoggedInUsers.RemoveRange(SessionsToBeRemoved);
                await currentLoggedInUsers.SaveChangesAsync();

                return true;

            }
            catch (Exception)
            {
                _logger.LogInformation("Failed CurrentUsersBackgroundServices");
                return false; 

            }


        }


        #region Helpser Functions 
        private async Task<bool> IsInactiveSession(CurrentLoggedInUsers Session, IRepository<UserNavigations> userNavigations)
        {
            var ConditionTime = DateTimeOffset.Now.AddHours(-24);

            var LastSession = await userNavigations.Query().AsNoTracking()
                                                             .Where(x => x.UserId == Session.UserId && x.UserIp == Session.ClientIP)
                                                             .OrderByDescending(x => x.CreationDate)
                                                             .FirstOrDefaultAsync();

            return LastSession != null && LastSession.CreationDate < ConditionTime;
        }
        #endregion


    }
}
