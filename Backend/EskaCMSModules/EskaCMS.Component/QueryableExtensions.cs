using EskaCMS.Components.Models;
using EskaCMS.Entities.Entity.Component;
using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Components
{
    public static class QueryableExtensions
    {
        public static IQueryable<PredefinedTemplates> FilterPredefinedTemplates(this IQueryable<PredefinedTemplates> query, SearchPredefinedTemplatesViewModel search)
        {
            if (search == null)
                return query;

            if (!string.IsNullOrEmpty(search.Description))
            {
                var Description = search.Description.ToUpper();

                query = query.Where(x => x.Description.ToUpper().Contains(Description));
            }
            if (!string.IsNullOrEmpty(search.Name))
            {
                var Name = search.Name.ToUpper();

                query = query.Where(x => x.Name.ToUpper().Contains(Name));
            }
            if (!string.IsNullOrEmpty(search.SettingsType))
            {
                var SettingsType = search.SettingsType.ToUpper();

                query = query.Where(x => x.SettingsType.Equals(SettingsType));
            }
            if (!string.IsNullOrEmpty(search.FullSearch))
            {
                var FullSearch = search.FullSearch.ToUpper();
                query = query.Where(x => x.Description.ToUpper().Contains(FullSearch)
                                        || x.Name.ToUpper().Contains(FullSearch)
                                        || x.SettingsType.Equals(FullSearch)
                                   );
                            
            }

            return query;


        }
    }
}
