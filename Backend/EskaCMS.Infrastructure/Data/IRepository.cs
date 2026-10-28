
using EskaCMS.Infrastructure.Models;
using EskaCMS.Infrastructure.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;


namespace EskaCMS.Infrastructure.Data
{
    public interface IRepository<T> : IRepositoryWithTypedId<T, long> where T : IEntityWithTypedId<long>
    {
    

        Task<IEnumerable<T>> GetAll();
        Task<IEnumerable<T>> GetByIncludeEntity(Expression<Func<T,bool>> predicate=null,int? page=null,int?pageSize=null, Func<IQueryable<T>,IOrderedQueryable<T>> orderBy=null,
            params Expression<Func<T, object>>[] includeProperties);
        Task<PagedResult<T>> GetByIncludeEntityPageResult(Expression<Func<T, bool>> predicate = null, 
            int? page = null, int? pageSize = null, 
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            params Expression<Func<T, object>>[] includeProperties);
        Task<IEnumerable<T>> GetAllEntity(Expression<Func<T, bool>> predicate = null, int? page = null, int? pageSize = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null );
        Task<PagedResult<T>> GetAllPageResult(Expression<Func<T, bool>> predicate = null, int? page = null, int? pageSize = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null );

  
        Task<T> GetById(object id);
        void Add(T entity);
        void AddRange(IEnumerable<T> entity);
        void Update(T entity);
        void Delete(T entity);
    }
}
