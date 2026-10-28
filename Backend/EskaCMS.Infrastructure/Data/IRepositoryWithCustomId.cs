using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;


namespace EskaCMS.Infrastructure.Data
{
    public interface IRepositoryWithCustomId<T> where T : class
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
    }
}
