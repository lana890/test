using EskaCMS.Entities.Entity.Comments;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Comments
{
    public static class QuerableExtensions
    {
        public static IQueryable<Comment> FilterComment(this IQueryable<Comment> query, SmartTableParam param)
        {

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.Id != null)
                {
                    long id = search.Id;
                    query = query.Where(x => x.Id == id);
                }

                if (search.UserId != null)
                {
                    long userId = search.UserId;
                    query = query.Where(x => x.UserId == userId);
                }

                if (search.Status != null)
                {
                    var status = (CommentStatus)search.Status;
                    query = query.Where(x => x.Status == status);
                }

                if (search.ProductId != null)
                {
                    long ProductId = Convert.ToInt64(search.ProductId);
                    query = query.Where(x => x.CommentProducts.Any(x=>x.ProductId== ProductId));
                }
                if (search.Name != null)
                {
                    string Name = search.Name.ToString();
                    query = query.Where(x => x.User.FullName.Contains(Name));
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
