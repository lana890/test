using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Comments;
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
    public static class QuerableExtensionReview
    {
        public static IQueryable<Review> FilterReview(this IQueryable<Review> query, SmartTableParam param)
        {

            if (param.Search?.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.Id != null)
                {
                    long id = search.Id;
                    query = query.Where(x => x.Id == id);
                }

                if (search.Status != null)
                {
                    var status = (ReviewStatus)search.Status;
                    query = query.Where(x => x.Status == status);
                }

                if (search.ProductId != null)
                {
                    long ProductId = Convert.ToInt64(search.ProductId);
                    query = query.Where(x => x.ReviewProducts.Any(x => x.ProductId == ProductId));
                }
                if (search.PostId != null)
                {
                    long PostId = Convert.ToInt64(search.PostId);
                    query = query.Where(x => x.ReviewPosts.Any(x => x.PostId == PostId));
                }
                if (search.UserId != null)
                {
                    long UserId = Convert.ToInt64(search.UserId);
                    query = query.Where(x => x.UserId == UserId);
                }

                if (search.Name != null)
                {
                    string Name = search.Name.ToString();
                    query = query.Where(x => x.User.FullName.Contains(Name));
                }

                if (search.EntityType != null)
                {
                    EEntity Type = search.EntityType;
                    query = query.Where(x => Type == EEntity.Product ? x.ReviewProducts.Any() : x.ReviewPosts.Any());
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
