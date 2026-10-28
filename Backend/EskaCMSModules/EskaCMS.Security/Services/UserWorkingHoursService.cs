using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Events;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Manage;
using EskaCMS.Core.Services;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using EskaCMS.Sites.Services;
using EskaCMS.Sites.Services.Interfaces;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.DataAccess.Data;

namespace EskaCMS.Security.Services
{
    public class UserWorkingHoursService : IUserWorkingHoursService
    {


        private readonly IRepository<UserWorkingHours> _UserWorkingHoursRepo;
        private readonly IRepository<User> _UsersRepo;
        private readonly EskaDCMSDbContext _context;
        private readonly IRepository<WorkingHoursDay> _workingHoursDay;
        private readonly IWorkContext _workContext;
        public UserWorkingHoursService(
            IWorkContext workContext,
            IRepository<UserWorkingHours> UserWorkingHoursRepo,
            IRepository<User> UsersRepo,
               EskaDCMSDbContext context,
            IRepository<WorkingHoursDay> WorkingHoursDay

       )
        {
            _workContext = workContext;
            _UserWorkingHoursRepo = UserWorkingHoursRepo;
            _UsersRepo = UsersRepo;
            _context = context;
            _workingHoursDay = WorkingHoursDay;
        }



        public async Task<PaginationResult<WorkingHoursVm>> GetAllWorkingHourse(PaginationFiltersVM Model)
        {
            try
            {
              return   await _UserWorkingHoursRepo.Query()
                     .Include(x => x.CustomerGroup)
                     .ThenInclude(x=>x.customerGroupDetails)
                     .Include(x=>x.Days)
                     .ThenInclude(x=>x.Day)
                     .Where(x => Model.customerGroupId == null || Model.customerGroupId.Value == 0 || x.CustomerGroupId == Model.customerGroupId.Value)
                     .Where(x => string.IsNullOrEmpty(Model.SearchText) ||
                                 x.CustomerGroup.customerGroupDetails.Any(h=>h.Name.ToLower().Contains(Model.SearchText.ToLower())) || 
                                 x.Id.ToString()== Model.SearchText

                     )
                     .Select(x => new WorkingHoursVm
                     {
                         Id = x.Id,
                         Day = x.Days.Select(x => new DayOfWeekViewModel { Id = x.Day.Id-1, Name = x.Day.Name }).ToList(),
                         CustomerGroupId = x.CustomerGroup.Id,
                         CustomerGroupName = x.CustomerGroup.customerGroupDetails.Select(x=>x.Name).FirstOrDefault(),
                         FromTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,x.FromTime.Hours, x.FromTime.Minutes,0), // DateTime.Now.Date + x.FromTime.to,
                         ToTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, x.ToTime.Hours, x.ToTime.Minutes, 0),
                     })
                     .PaginateAsync(Model.PageSize, Model.PageNumber);


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<WorkingHoursVm> GetById(long Id)
        {
            try
            {
                var cultureId = _workContext.GetCurrentCultureId();

                var Query = await _UserWorkingHoursRepo.Query()
                    .Where(x => x.Id == Id)
                    .Include(x=>x.CustomerGroup)
                    .ThenInclude(x=>x.customerGroupDetails)
                    .Include(x => x.Days)
                     .ThenInclude(x => x.Day)
                     .Select(x => new WorkingHoursVm
                     {
                         Id = x.Id,
                         Day = x.Days.Select(x =>new DayOfWeekViewModel { Id = x.Day.Id-1, Name = x.Day.Name }).ToList(),
                         CustomerGroupId = x.CustomerGroup.Id,
                        CustomerGroupName = x.CustomerGroup.customerGroupDetails.Where(x=> cultureId==0||x.CultureId== cultureId).Select(x => x.Name).FirstOrDefault(),
                         FromTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, x.FromTime.Hours, x.FromTime.Minutes, 0), // DateTime.Now.Date + x.FromTime.to,
                        ToTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, x.ToTime.Hours, x.ToTime.Minutes, 0),
                    })
                    .FirstOrDefaultAsync();

                return Query;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> IsInWorkingHours(long UserId, long SiteId)
        {
            var Groups = await _UsersRepo.Query()
                     .Where(u => u.Id == UserId)
                     .Include(u => u.CustomerGroups)
                     .Select(u => u.CustomerGroups.Select(z => z.CustomerGroupId))
                     .FirstOrDefaultAsync();
            var currentdayOfWeek = (int)DateTimeOffset.Now.DayOfWeek;
            var targetday = currentdayOfWeek-1;
            var hours = await _UserWorkingHoursRepo
                      .Query().AsNoTracking()
                      .Include(x=>x.Days)
                      .Include(x=>x.CustomerGroup)
                      .Where(u =>
                      Groups.Any(x => x == u.CustomerGroupId) &&
                      (u.CustomerGroup.SiteId == null || u.CustomerGroup.SiteId.Value == SiteId)
                        &&
                      (u.Days.Any(x=>(int)x.DayId== targetday))
                      ).ToListAsync();

           // var time = DateTimeOffset.Now.TimeOfDay;
            var ValidHours = hours
                .Where(u =>
                            u.FromTime.CompareTo(DateTime.Now.TimeOfDay) <= 0 &&
                            u.ToTime.CompareTo(DateTime.Now.TimeOfDay) > 0)
                .ToList();

            if (hours.Count == 0 || ValidHours.Count > 0)
            {
                return true;
            }



            return Groups == null || Groups.Count() == 0;
        }

        public async Task<bool> Remove(long Id)
        {
            try
            {
                var Query = await _UserWorkingHoursRepo.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();

                var WorkingHours = await _workingHoursDay.Query().Where(x => x.WorkingHoursId == Id).ToListAsync();

                _workingHoursDay.RemoveRange(WorkingHours);

                await _workingHoursDay.SaveChangesAsync();


                _UserWorkingHoursRepo.Remove(Query);

                await _UserWorkingHoursRepo.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {

                throw ex;

            }
        }

        public async Task<bool> upsert(UpsertWorkingHoursVm workingHours)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var DBTrans = await _context.Database.BeginTransactionAsync();
                try
                {


                    // NOTE!! any api with files must be formdata
                    long UserId = await _workContext.GetCurrentUserId();


                    bool IsEditMood = workingHours.Id != 0;

                    var UserWorkingHour = new UserWorkingHours();

                    if (IsEditMood)
                    {
                        UserWorkingHour = await _UserWorkingHoursRepo
                                      .Query()
                                      .Where(m => m.Id == workingHours.Id)
                                      .FirstOrDefaultAsync();
                        if (UserWorkingHour == null)
                        {
                            throw new Exception("Not Found UserWorkingHour");
                        }


                        UserWorkingHour.ModifiedById = UserId;
                        UserWorkingHour.ModificationDate = DateTimeOffset.Now;
                    }
                    else
                    {
                        UserWorkingHour.CreatedById = UserId;
                        UserWorkingHour.CreationDate = DateTimeOffset.Now;
                    }




                    UserWorkingHour.CustomerGroupId = workingHours.CustomerGroupId;
                    UserWorkingHour.ToTime = workingHours.ToTime.TimeOfDay;
                    UserWorkingHour.FromTime = workingHours.FromTime.TimeOfDay;


                    if (!IsEditMood)
                    {
                        _UserWorkingHoursRepo.Add(UserWorkingHour);


                    }


                    await _UserWorkingHoursRepo.SaveChangesAsync();

                    List < WorkingHoursDay > DaysOfwork= new();
                    if (IsEditMood)
                    {
                        var WorkingHourseOld = await _workingHoursDay.Query().Where(x => x.WorkingHoursId == UserWorkingHour.Id).ToListAsync();

                        _workingHoursDay.RemoveRange(WorkingHourseOld);

                    }

                    foreach (var Id in workingHours.Day)
                    {

                        DaysOfwork.Add(new WorkingHoursDay() { WorkingHoursId = UserWorkingHour.Id, DayId = Id+1 });


                    }

                    _workingHoursDay.AddRange(DaysOfwork);
                    await _workingHoursDay.SaveChangesAsync();

                    await DBTrans.CommitAsync();

                    return true;


                }

                catch (Exception ex)
                {
                    await DBTrans.RollbackAsync();

                    throw ex;
                }
            });

        }
    }
}