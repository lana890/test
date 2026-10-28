using EskaCMS.Components.Models;
using EskaCMS.Components.Services.Interfaces;
using EskaCMS.Entities.Entity.Component;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Components.Services
{
    public class PredefinedTemplatesServices : IPredefinedTemplatesServices
    {
        private readonly IRepository<PredefinedTemplates> PredefinedTemplatesRepository;

        public PredefinedTemplatesServices(IRepository<PredefinedTemplates> PredefinedTemplatesRepository)
        {
            this.PredefinedTemplatesRepository = PredefinedTemplatesRepository;
        }

        public async Task<bool> CreateSetting(PredefinedTemplatesViewModel setting)
        {
            try
            {
                var query = new PredefinedTemplates
                {
                    Description = setting.Description,
                    Settings = setting.Settings,
                    SettingsType = setting.SettingsType.ToUpper(),
                    Name = setting.Name,
                    ThumbnailUrl = setting.ThumbnailUrl
                };
                PredefinedTemplatesRepository.Add(query);
                await PredefinedTemplatesRepository.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task DeleteSetting(int id)
        {
            try
            {
                var ObjPredefinedTemplates = await PredefinedTemplatesRepository.Query()
                                                                          .Where(x => x.Id == id)
                                                                          .FirstOrDefaultAsync();

                PredefinedTemplatesRepository.Remove(ObjPredefinedTemplates);
                await PredefinedTemplatesRepository.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<PaginationResult<PredefinedTemplatesViewModel>> GetAllSettings(PredefinedTemplatesSearchTableParam<SearchPredefinedTemplatesViewModel> model)
        {
            try
            {
                var query = await PredefinedTemplatesRepository.Query()
                                                           .AsNoTracking()
                                                           .FilterPredefinedTemplates(model.Search)
                                                           .Select(x => new PredefinedTemplatesViewModel
                                                                  {
                                                                      Description = x.Description,
                                                                      Name = x.Name,
                                                                      Id = x.Id,
                                                                      SettingsType = x.SettingsType,
                                                                      Settings = x.Settings,
                                                                      ThumbnailUrl = x.ThumbnailUrl
                                                                  })
                                                                   .OrderByDescending(x=>x.Id)
                                                                   .PaginateAsync(model.Pagination.Number, model.Pagination.Start);
                return query;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<PredefinedTemplatesViewModel> GetSettingById(int id)
        {
            try
            {
                var query = await PredefinedTemplatesRepository.Query()
                                                                .AsNoTracking()
                                                                .Where(x => x.Id == id
                                                               ).Select(x => new PredefinedTemplatesViewModel
                                                               {
                                                                   Description = x.Description,
                                                                   Name = x.Name,
                                                                   Id = x.Id,
                                                                   SettingsType = x.SettingsType,
                                                                   Settings = x.Settings,
                                                                   ThumbnailUrl = x.ThumbnailUrl
                                                               }).FirstOrDefaultAsync();

                return query;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task UpdateSetting(long id, PredefinedTemplatesViewModel setting)
        {
            try
            {
                var ObjPredefinedTemplates = await PredefinedTemplatesRepository.Query()
                                                                          .Where(x => x.Id == id)
                                                                          .FirstOrDefaultAsync();
                ObjPredefinedTemplates.Description = setting.Description;
                ObjPredefinedTemplates.Settings = setting.Settings;
                ObjPredefinedTemplates.SettingsType = setting.SettingsType.ToUpper();
                ObjPredefinedTemplates.Name = setting.Name;
                ObjPredefinedTemplates.ThumbnailUrl = setting.ThumbnailUrl;

                await PredefinedTemplatesRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
