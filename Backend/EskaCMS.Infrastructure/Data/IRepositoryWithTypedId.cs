using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore.Storage;


namespace EskaCMS.Infrastructure.Data
{
    public interface IRepositoryWithTypedId<T, TId> where T : IEntityWithTypedId<TId>
    {
        IQueryable<T> Query();

        void Add(T entity);

        void AddRange(IEnumerable<T> entity);

        IDbContextTransaction BeginTransaction();
        Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
        Task ExecuteInTransactionAsync(Func<Task> operation);

        void SaveChanges();

        Task SaveChangesAsync();

        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entity);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entity);
        public Task<int> SQLQuery(string Condition, params object[] QueryParams);
        public IQueryable<T> SQLCondition(string Condition, params object[] QueryParams);
    }
}
