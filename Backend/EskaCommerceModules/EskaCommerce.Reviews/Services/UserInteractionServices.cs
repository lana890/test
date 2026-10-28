using EskaCMS.Entities.Entity.Reviews;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Reviews.Models.UserInteractionModels;
using EskaCommerce.Reviews.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Reviews.Services
{
    public class UserInteractionServices : IUserInteractionServices
    {
        private readonly IRepository<UserInteractionConfiguration> userConfiguraationRepository;

        public UserInteractionServices(IRepository<UserInteractionConfiguration> UserConfiguraationRepository)
        {
            userConfiguraationRepository = UserConfiguraationRepository;
        }

        public async Task<List<UserInteractionViewModel>> Get(GeneralEnums.ERoleTypes? RoleId=null)
        {
            try
            {
                var query = await userConfiguraationRepository
                                                           .Query()
                                                           .Where(x => RoleId == null || RoleId == x.ERoleTypes)
                                                           .Select(x=>new UserInteractionViewModel
                                                           { 
                                                               AllowCommnetIfPayOrder=x.AllowCommnetIfPayOrder,
                                                               ERoleTypes=x.ERoleTypes,
                                                               MaxNumberOfComments=x.MaxNumberOfComments,
                                                               Id=x.Id,
                                                               MaxNumberOfReplays=x.MaxNumberOfReplays, 
                                                               MaxNumberOfReviews=x.MaxNumberOfReviews
                                                           
                                                           })
                                                           .ToListAsync();
                return query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> Insert(UserInteractionViewModel model)
        {
            try
            {
                var query = new UserInteractionConfiguration();
                query.AllowCommnetIfPayOrder = model.AllowCommnetIfPayOrder;
                query.MaxNumberOfComments = model.MaxNumberOfComments;
                query.MaxNumberOfReviews = model.MaxNumberOfReviews;
                query.MaxNumberOfReplays=model.MaxNumberOfReplays;
                query.ERoleTypes = model.ERoleTypes;

                userConfiguraationRepository.Add(query);

               await userConfiguraationRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> Remove(long Id)
        {
            try
            {
                var query = await userConfiguraationRepository.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();

                userConfiguraationRepository.Remove(query);
              await  userConfiguraationRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> Update(UserInteractionViewModel model)
        {
            try
            {
                var query = await userConfiguraationRepository.Query().Where(x => x.Id == model.Id).FirstOrDefaultAsync();
                query.AllowCommnetIfPayOrder = model.AllowCommnetIfPayOrder;
                query.MaxNumberOfComments = model.MaxNumberOfComments;
                query.MaxNumberOfReviews = model.MaxNumberOfReviews;
                query.MaxNumberOfReplays = model.MaxNumberOfReplays;
                query.ERoleTypes = model.ERoleTypes;
                await userConfiguraationRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
