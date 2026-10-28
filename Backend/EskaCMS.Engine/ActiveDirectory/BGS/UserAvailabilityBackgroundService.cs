using EskaCMS.Engine.Models;
using EskaCMS.Engine.Services.Interfaces;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.DbFactory;
using EskaCMS.Infrastructure.Enums;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Engine.BGS
{
    public class UserAvailabilityBackgroundService : BaseBackgroundService
    {
        private readonly ILogger<UserAvailabilityBackgroundService> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration configuration;
        public long UserOptionServiceExecutionTime = 5;
        private string DefaultConnection = "";
        private EProvider Provider ;

        public UserAvailabilityBackgroundService(ILogger<UserAvailabilityBackgroundService> logger,
                                                      System.IServiceProvider serviceProvider,
                                                      IConfiguration Configuration)
            : base(nameof(UserAvailabilityBackgroundService), logger, serviceProvider, Configuration)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            configuration = Configuration;
            DefaultConnection = configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
            UserOptionServiceExecutionTime = Convert.ToInt32(configuration.GetSection("UserAvailabilityBackgroundService:ExecutionTimeInMinutes").Value);
            Provider = DbProviderHelper.ParseProvider(configuration.GetValue<string>("ConnectionStrings:DatabaseProvider"));

        }



        public override async Task<bool> Execute()
        {
            try
            {

                TimeSpan period = TimeSpan.FromMinutes(UserOptionServiceExecutionTime);
                using PeriodicTimer timer = new PeriodicTimer(period);


                var allUserOptionsToBeUpdated = await GetAllUsersOptionsToBeUpdated();

                using (var scope = serviceProvider.CreateScope())
                {
                    var activeDirectoryServices = scope.ServiceProvider.GetRequiredService<IActiveDirectoryServices>();

                    if (allUserOptionsToBeUpdated.Any())
                    {
                        foreach (var useroption in allUserOptionsToBeUpdated)
                        {
                            await activeDirectoryServices.AddUpdateUserOptionsSP(useroption.UserId, useroption.OptionName, useroption.OptionValue);


                        }

                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                logger.LogInformation($" User Availability Background Service Faild :  {ex.Message}.");
            }
        }



        private async Task<List<UserOptionsToBeAddedVM>> GetAllUsersOptionsToBeUpdated()
        {
            try
            {
                var userIds = new List<long>();

                using (var connection = dbFactory.CreateConnection(DefaultConnection, Provider))

                {
                    using (var command =dbFactory.CreateCommand("GetEventDetails", connection,Provider))
                    {
                        var dateParam = dbFactory.CreateParameter(Provider, "@currentDateTime", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(dateParam);
                        await connection.OpenAsync();
                        using var reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            DbDataReader result = reader;

                            var userId = Convert.ToInt64(result["UserId"].ToString());
                            userIds.Add(userId);
                        }
                    }
                }

                var alluserOptionToBeUpdated = userIds.DistinctBy(x => x)
                                                       .Select(x => new UserOptionsToBeAddedVM
                                                       {
                                                           UserId = x,
                                                           OptionName = "AvailabilityStatus",
                                                           OptionValue = ((int)EUserAvailabilityStatus.Busy).ToString()
                                                       })
                                                       .ToList();

                if (!alluserOptionToBeUpdated.Any())
                    return new List<UserOptionsToBeAddedVM>();

                return alluserOptionToBeUpdated;


            }
            catch (Exception ex)
            {
                logger.LogInformation("UpdateParentSP : Error   : " + ex.Message);
                return null;

            }
        }
    }
}
