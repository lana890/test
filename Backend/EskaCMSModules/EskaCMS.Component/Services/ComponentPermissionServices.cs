using EskaCMS.Components.Models.ComponentPermission;
using EskaCMS.Components.Services.Interfaces;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Component;
using EskaCMS.Entities.Entity.Elements;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Components.Services
{
    public class ComponentPermissionServices : IComponentPermissionServices
    {
        private readonly IRepository<Page> pageRepository;
        private readonly IRepository<PageTemplates> pageTemplaterepository;
        private readonly IWorkContext workContext;
        private readonly IRepository<ShareableElement> shareableElementRepository;
        private readonly IRepository<ComponentPermissions> componentPermissionRepository;

        public ComponentPermissionServices(IRepository<Page> pageRepository,
                                           IRepository<PageTemplates> pageTemplaterepository,
                                           IWorkContext workContext,
                                           IRepository<ShareableElement> shareableElementRepository,
                                           IRepository<ComponentPermissions> componentPermissionRepository)
        {
            this.pageRepository=pageRepository;
            this.pageTemplaterepository=pageTemplaterepository;
            this.workContext=workContext;
            this.shareableElementRepository=shareableElementRepository;
            this.componentPermissionRepository=componentPermissionRepository;
        }

        public async Task AddComponentPermission(RequestComponentPermissionViewModel model)
        {
            try
            {
                var userId = await workContext.GetCurrentUserId();
                var permissionToRemove = await componentPermissionRepository.Query()
                                                                          .Where(x => x.ComponentSettingsId==model.ComponentSettingsId).ToListAsync();
                if (permissionToRemove.Any())
                {
                    componentPermissionRepository.RemoveRange(permissionToRemove);
                    await componentPermissionRepository.SaveChangesAsync();
                }

                var added = model.PermissionIds.ConvertAll(x => new ComponentPermissions
                {
                    ComponentSettingsId=model.ComponentSettingsId,
                    PermissionId=x,
                    CreationDate=DateTimeOffset.Now,
                    CreatedById=userId,
                    RoleId=model.RoleId
                });

                componentPermissionRepository.AddRange(added);
                await componentPermissionRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public async Task<List<GetComponentPermissionViewModel>> GetComponentPermission(FilterationComponentPermissionViewModel model)
        {
            try
            {
                var componentSettings = await getRelatedComponenetSettingsIds(model);



                return componentSettings;
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        private async Task<List<GetComponentPermissionViewModel>> getRelatedComponenetSettingsIds(FilterationComponentPermissionViewModel model)
        {
            string data = null;

            if (model.PageId.HasValue)
            {
                data = await pageRepository.Query()
                    .Where(x => x.Id == model.PageId.Value)
                    .Include(x => x.CurrentVersion)
                    .Select(x => x.CurrentVersion.SettingsBody)
                    .FirstOrDefaultAsync();
            }
            else if (model.TemplateId.HasValue)
            {
                data = await pageTemplaterepository.Query()
                    .Where(x => x.Id == model.TemplateId.Value)
                    .Include(x => x.CurrentVersion)
                    .Select(x => x.CurrentVersion.SettingsBody)
                    .FirstOrDefaultAsync();
            }
            else if (model.SharedId.HasValue)
            {
                return await shareableElementRepository.Query()
                     .Where(x => x.Id == model.SharedId.Value)
                     .Select(x => new GetComponentPermissionViewModel
                     {
                         ComponentSettingsId=x.DataKey,
                         ComponentName=x.Name
                     })
                     .ToListAsync();
            }
            var permissions = componentPermissionRepository.Query().AsQueryable();

            return ExtractComponents(data, permissions);
        }
        private List<GetComponentPermissionViewModel> ExtractComponents(string json, IQueryable<ComponentPermissions> permissionsrepository)
        {
            var components = new List<GetComponentPermissionViewModel>();

            if (!string.IsNullOrWhiteSpace(json))
            {
                var root = JObject.Parse(json);
                var comps = root.SelectTokens("$..component");

                foreach (var comp in comps)
                {
                    var settingsId = comp["componentSettingsId"]?.ToString();

                    components.Add(new GetComponentPermissionViewModel
                    {
                        ComponentSettingsId = settingsId,
                        ComponentName = comp["componentName"]?.ToString(),
                        PermissionIds = permissionsrepository
                                .Where(p => p.ComponentSettingsId == settingsId)
                                .Select(p => new GetPermissionViewModel
                                {
                                    Code=p.Permission.Code,
                                    Name=p.Permission.Name,
                                    PermissionId=p.PermissionId

                                })
                                .ToList()
                    });
                }
            }

            return components;
        }


    }
}
