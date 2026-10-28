 
using EskaCMS.Core.Data;
using EskaCMS.DataAccess.Data;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Infrastructure.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EskaCMS.Module.Core.Data
{
    public class Repository<T> : RepositoryWithTypedId<T, long>, IRepository<T>
        where T : class, IEntityWithTypedId<long>
    {

        protected readonly EskaDCMSDbContext _context;
        internal DbSet<T> DbSet;
        public Repository(EskaDCMSDbContext context) : base(context)
        {
            _context = context;
            DbSet = _context.Set<T>();
        }

      

        public async Task<IEnumerable<T>> GetAll()
        {

            return await DbSet.AsNoTracking().ToListAsync();

        }

        public async Task<T> GetById(object id)
        {
            DbSet.AsNoTracking();
            var entryToFind = await DbSet.FindAsync(id);
            if (entryToFind != null)
                _context.Entry(entryToFind).State = EntityState.Detached;
            return entryToFind;
        }

        public void  Add(T entity)
        {
         
            //DateHelper.ConvertDatesToUtc(entity);
            DbSet.Add(entity);
        }

        public void Update(T entity)
        {
         
             //   DateHelper.ConvertDatesToUtc(entity);
            DbSet.Update(entity);
        }
        public void Delete(T entity)
        {
            DbSet.Remove(entity);
        }

      
         public async Task AddRange(IEnumerable<T> entities)
        {
            //foreach (var obj in entities)
            //    DateHelper.ConvertDatesToUtc(obj);
            await _context.Set<T>().AddRangeAsync(entities);
        }

        public async Task<IEnumerable<T>> GetByIncludeEntity(Expression<Func<T, bool>> predicate = null, int? page = null, int? pageSize = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, params Expression<Func<T, object>>[] includeProperties)
        { 

            IQueryable<T> results=GetAllIncluding(withTrack:false,
                includeProperties:includeProperties);
            if (predicate != null)
                results = results.Where(predicate);

            if (page.GetValueOrDefault()>0 && pageSize.GetValueOrDefault()>0)

                results = results.ApplyPaging(page.Value,pageSize.Value);

            orderBy?.Invoke(results);

            return await results.ToListAsync();
        }

        private IQueryable<T> GetAllIncluding(bool withTrack, params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> queryable = null;
            if (withTrack)
                queryable = DbSet.AsQueryable();
            else
                queryable = DbSet.AsNoTracking();
            return includeProperties.Aggregate(queryable, (current, includeProperty) =>
            current.Include(includeProperty));

        }
        private IQueryable<T> GetAllIncludingPagintion(bool withTrack, params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> queryable = null;
            if (withTrack)
                queryable = DbSet.AsQueryable();
            else
                queryable = DbSet.AsNoTracking();
            return includeProperties.Aggregate(queryable, (current, includeProperty) =>
            current.Include(includeProperty));

        }
        public async Task<PagedResult<T>> GetByIncludeEntityPageResult
            (Expression<Func<T, bool>> predicate = null, int? page = null, int? pageSize = null
            , Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, 
            params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> results = GetAllIncluding(withTrack: false, includeProperties: includeProperties);
            int count = 0;

            if (predicate != null)
                results = results.Where(predicate);
            count = await results.CountAsync();
            if (page.GetValueOrDefault() > 0 && pageSize.GetValueOrDefault() > 0)
            {
             
                results = results.ApplyPaging(page.Value, pageSize.Value);
               
            }
            orderBy?.Invoke(results);

            return new PagedResult<T>()
            {
                Result = await results.ToListAsync(),
                TotalCount = count
            };
        }

        public async Task<IEnumerable<T>> GetAllEntity(Expression<Func<T, bool>> predicate = null, int? page = null, int? pageSize = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null)
        {
            IQueryable<T> results = DbSet.AsNoTracking();
            if (predicate != null)
                results = results.Where(predicate);

            if (page.GetValueOrDefault() > 0 && pageSize.GetValueOrDefault() > 0)
            {
                 results = results.ApplyPaging(page.Value, pageSize.Value);

            }
            orderBy?.Invoke(results);
            return await results.ToListAsync();

        }

        public async Task<Infrastructure.Pagination.PagedResult<T>> GetAllPageResult(Expression<Func<T, bool>> predicate = null, int? page = null, int? pageSize = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null)
        {
            IQueryable<T> results = DbSet.AsNoTracking();
            if (predicate != null)
                results = results.Where(predicate);
            int count = 0;
            if (page.GetValueOrDefault() > 0 && pageSize.GetValueOrDefault() > 0)
            {
                count = await results.CountAsync();
                results = results.ApplyPaging(page.Value, pageSize.Value);

            }
            orderBy?.Invoke(results);
            return new PagedResult<T>()
            {
                Result = await results.ToListAsync(),
                TotalCount = count
            };
        }
 


        //public Repository(EskaDCMSDbContext mySqlContext) : base(mySqlContext)
        //{
        //}
    }
}
