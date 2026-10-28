using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Core.ViewModels.ComponentKeys;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Entitys;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EskaCMS.Components.Services
{
    public class ComponentKeysServices : IComponentKeysServices
    {
        private readonly IRepository<ComponentCommunicationKeys> Componentcommunicationrepository;
        private readonly IWorkContext _WorkContext;

        public ComponentKeysServices(IRepository<ComponentCommunicationKeys> componentcommunicationrepository, IWorkContext workContext)
        {
            Componentcommunicationrepository = componentcommunicationrepository;
            _WorkContext = workContext;
        }

        public async Task<List<ComponentKeysVM>> GetComponentKeys(bool? status)
        {
            try
            {


                long SiteId = await _WorkContext.GetCurrentSiteIdAsync();


                var query = await Componentcommunicationrepository
                      .Query()
                      .Where(x => x.SiteId == SiteId)
                      .Where(x=>status==null||status.Value==x.IsConFirmed)
                      .Select(x => new ComponentKeysVM
                      {
                          Id = x.Id,
                          PageId = x.PageId,
                          TemplatePageId = x.PageTemplateId,
                          Key = x.Key,
                          SiteId = x.SiteId,
                          IsConFirmed=x.IsConFirmed
                          

                      }).AsNoTracking().ToListAsync();


                return query;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> ChangeStatusByPageTemplateId(long PageTemplateId)
        {
            try
            {
                var SiteId = await _WorkContext.GetCurrentSiteIdAsync();

                var query = await Componentcommunicationrepository.Query().Where(x => x.PageTemplateId == PageTemplateId && x.SiteId == SiteId).ToListAsync();

                foreach (var value in query)
                {
                    value.IsConFirmed = true;
                }

                await Componentcommunicationrepository.SaveChangesAsync();

                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<bool> ChangeStatus(long PageId)
        {
            try
            {
                    var SiteId= await _WorkContext.GetCurrentSiteIdAsync();
               
                    var query = await Componentcommunicationrepository.Query().Where(x =>x.PageId ==PageId && x.SiteId == SiteId).ToListAsync();

                    foreach (var value in query)
                    {
                        value.IsConFirmed = true;
                    }

                    await Componentcommunicationrepository.SaveChangesAsync();

                    return true;
               

            }
            catch (Exception ex)
            {

                throw ex;
            }
        
        }
        public async Task<ComponentKeysVM> GetComponentKeysById(long id)
        {
            try
            {

                var query = await Componentcommunicationrepository
                      .Query()
                      .Where(x => x.Id == id)
                      .Select(x => new ComponentKeysVM
                      {
                          Id = x.Id,
                          PageId = x.PageId,
                          TemplatePageId = x.PageTemplateId,
                          Key = x.Key,
                          SiteId = x.SiteId,
                          IsConFirmed=x.IsConFirmed

                      }).AsNoTracking().FirstOrDefaultAsync();


                return query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<bool> UpsertComponentKeys(List<UpsertComponentKeysVM> model)
        {
            try
            {
                foreach (var Model in model)
                {
                    if ((Model.PageId == 0 && Model.PageTemplateId == 0) || (Model.PageId == null && Model.PageTemplateId == null))
                    {

                        throw new Exception("Please Add At Least one Value In PageId Or PageTemplateId");

                    }

                    if (Model.Id == 0)
                    {
                        await AddComponentKeys(Model);
                    }
                    else
                    { await UpdateComponentKeys(Model); }
                }

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> DeleteComponentKey(long id)
        {
            try
            {
                var query = await Componentcommunicationrepository.Query()
                                                                .Where(x => x.Id == id)
                                                                .FirstOrDefaultAsync();

                Componentcommunicationrepository.Remove(query);

               await Componentcommunicationrepository.SaveChangesAsync();

                return true;
                          
            }
            catch (Exception ex)
            {

                throw ex;
            }        }

        #region Helpers


        private async Task<bool> UpdateComponentKeys(UpsertComponentKeysVM model)
        {
            try
            {
                long SiteId = await _WorkContext.GetCurrentSiteIdAsync();
                long UserId = await _WorkContext.GetCurrentUserId();

             //   await CheckIsExist(model.Key, SiteId);

                var query = await Componentcommunicationrepository.Query().Where(x => x.Id == model.Id).FirstOrDefaultAsync();

                query.PageId = model.PageId == 0 ? null : model.PageId;
                query.PageTemplateId = model.PageTemplateId == 0 ? null : model.PageTemplateId;
                query.Key = model.Key;
                query.ModifiedById = UserId;
                query.ModificationDate = DateTime.Now;
                query.IsConFirmed = model.IsConFirmed;


                await Componentcommunicationrepository.SaveChangesAsync();

                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<bool> AddComponentKeys(UpsertComponentKeysVM model)
        {
            try
            {
                long SiteId = await _WorkContext.GetCurrentSiteIdAsync();

                long UserId = await _WorkContext.GetCurrentUserId();

                await CheckIsExist(model.Key, SiteId);

                ComponentCommunicationKeys Model = new ComponentCommunicationKeys();


                Model.PageId = model.PageId==0?null: model.PageId;
                Model.PageTemplateId = model.PageTemplateId == 0 ? null: model.PageTemplateId;
                Model.Key = model.Key;
                Model.CreatedById = UserId;
                Model.SiteId = SiteId;
                Model.CreationDate = DateTime.Now;

                Model.IsConFirmed = model.IsConFirmed;




                    

                

                Componentcommunicationrepository.Add(Model);
                await Componentcommunicationrepository.SaveChangesAsync();

                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task CheckIsExist(string key, long siteId)
        {
            try
            {
                var Result =await Componentcommunicationrepository.Query().AsNoTracking().Where(x => key==x.Key && siteId == x.SiteId).CountAsync();
                
                if (Result > 0)
                {
                    throw new Exception("Key Is Already Exist with Same Site Id");
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

   
        #endregion

    }
}
