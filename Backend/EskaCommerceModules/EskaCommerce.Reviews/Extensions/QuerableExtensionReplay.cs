using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Reviews;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Reviews.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<Reply> FilterReplay(this IQueryable<Reply> query, SmartTableParam param)
        {

            if (param.Search?.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.Id != null)
                {
                    long id = search.Id;
                    query = query.Where(x => x.Id == id);
                }

                if (search.ReviewId != null)
                {
                    long ReviewId = search.ReviewId;
                    query = query.Where(x => x.ReviewId == ReviewId);
                }

                if (search.Status != null)
                {
                    var status = (ReviewStatus)search.Status;
                    query = query.Where(x => x.Status == status);
                }
                if (search.Name != null)
                {
                    string Name = search.Name.ToString();
                    query = query.Where(x => x.User.FullName.Contains(Name));
                }

                if (search.EntityType != null)
                {
                    EEntity Type = search.EntityType;
                    query = query.Where(x => Type == EEntity.Product ? x.Review.ReviewProducts.Any() : x.Review.ReviewPosts.Any());
                }

                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        query = query.Where(x => x.CreatedOn <= before);
                    }

                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        query = query.Where(x => x.CreatedOn >= after);
                    }
                }
            }

            return query;

        }
    }

}
