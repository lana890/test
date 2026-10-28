using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Core.Services
{
    public class CultureService : ICultureService
    {

        private readonly IRepositoryWithTypedId<Culture, string> _CultureRepository;
        private readonly IRepository<SitesCultures> _SitesCulturesRepository;

        public CultureService(IRepositoryWithTypedId<Culture, string> CultureRepository, IRepository<SitesCultures> SitesCulturesRepository)
        {
            _CultureRepository = CultureRepository;
            _SitesCulturesRepository = SitesCulturesRepository;
    }

        public async Task<bool> AddCulture(CultureViewModel cultureViewModel)
        {
            try
            {
                var Query = new Culture(cultureViewModel.Id);

                Query.Name = cultureViewModel.Name;
                
                _CultureRepository.Add(Query);

              await   _CultureRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<CultureViewModel> GetById(string Id)
        {
            try
            {
                var Query =await  _CultureRepository
                                         .Query()
                                         .Where(x => x.Id.ToLower() == Id.ToLower())
                                         .Select(x=>new CultureViewModel
                                         {
                                             Id=x.Id,
                                             Name=x.Name
                                         })
                                         .FirstOrDefaultAsync();

                return Query;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<List<CultureViewModel>> GetCultures()
        {
            try
            {
                var Query = await _CultureRepository
                                       .Query()
                                       .Select(x => new CultureViewModel
                                       {
                                           Id = x.Id,
                                           Name = x.Name
                                       })
                                       .ToListAsync();

                return Query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> RemoveCulture(string Id)
        {
            try
            {
                var Query = await _CultureRepository
                                     .Query()
                                     .Where(x => x.Id.Trim() == Id.Trim())
                                     .FirstOrDefaultAsync();

                var QuerySiteCulture = await _SitesCulturesRepository.Query().Where(x => x.CultureId.ToLower().Trim() == Id.ToLower().Trim()).ToListAsync();
                if (QuerySiteCulture.Count() > 0)
                {

                    throw new Exception("You Can Not Delete This Culture Because it Belongs to Site");
                }
              await   _SitesCulturesRepository.SaveChangesAsync();

                _CultureRepository.Remove(Query);

               await _CultureRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> UpdateCulture(CultureViewModel cultureViewModel)
        {
            try
            {
                var Query =await _CultureRepository.Query().Where(x => cultureViewModel.Id == x.Id).FirstOrDefaultAsync();

                if (Query == null)
                {
                    throw new Exception("Can Not Find This Culture");
                }
                Query.Name = cultureViewModel.Name;
                await _CultureRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
