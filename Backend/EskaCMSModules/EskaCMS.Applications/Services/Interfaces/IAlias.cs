

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Sites;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace EskaCMS.Sites.Services.Interfaces
{
    public interface IAlias
    {
 
        Task<IEnumerable<Aliases>> GetAllAsync(long SiteId );

     
        Task<Aliases> GetByIdAsync(long Id);

       
        Task<Aliases> GetByAliasUrlAsync(string url, long SiteId);

    
        Task<IEnumerable<Aliases>> GetByRedirectUrlAsync(string url, long SiteId);

        /// <summary>
        /// Adds or updates the given model in the database
        /// depending on its state.
        /// </summary>
        /// <param name="model">The model</param>
        Task SaveAsync(Aliases model);

      
        Task DeleteAsync(long Id);

        Task DeleteAsync(Aliases model);
    }
}
