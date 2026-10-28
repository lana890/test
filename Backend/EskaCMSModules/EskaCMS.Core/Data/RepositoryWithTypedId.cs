using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Data;
using EskaCMS.Infrastructure.Models;
using EskaCMS.DataAccess.Data;
using System;
using MailKit;
using EskaCMS.Infrastructure.Enums;
using EskaCMS.Infrastructure.Helpers;

namespace EskaCMS.Module.Core.Data
{
    public class RepositoryWithTypedId<T, TId> : IRepositoryWithTypedId<T, TId> where T : class, IEntityWithTypedId<TId>
    {
        public RepositoryWithTypedId(EskaDCMSDbContext context)
        {
            Context = context;
            DbSet = Context.Set<T>();
        }


        protected EskaDCMSDbContext Context { get; }
      

        protected DbSet<T> DbSet { get; }

        public void Add(T entity)
        {
            //DateHelper.ConvertDatesToUtc(entity);
            DbSet.Add(entity);
        }


        public void Update(T entity)
        {
           // DateHelper.ConvertDatesToUtc(entity);
            DbSet.Update(entity);
        }
        public void UpdateRange(IEnumerable<T> entity)
        { 
        //{
        //    foreach (var obj in entity)
        //        DateHelper.ConvertDatesToUtc(obj);
            DbSet.UpdateRange(entity);
        }

        public void AddRange(IEnumerable<T> entity)
        {
            //foreach(var obj in entity )
            //    DateHelper.ConvertDatesToUtc(obj);
            DbSet.AddRange(entity);
        }

        public IDbContextTransaction BeginTransaction()
        {
            return Context.Database.BeginTransaction();
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
        {
            var strategy = Context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await Context.Database.BeginTransactionAsync();
                try
                {
                    var result = await operation();
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            var strategy = Context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await Context.Database.BeginTransactionAsync();
                try
                {
                    await operation();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public void SaveChanges()
        {
            Context.SaveChanges();
        }

        public Task SaveChangesAsync()
        {
            return Context.SaveChangesAsync();
        }

        public IQueryable<T> Query()
        {
            //var now = Context.Database.ProviderName.Contains("Npgsql") ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
                   

            return DbSet.Select(x=>x);
        }

        /// <summary>
        /// 
        /// Returns Number Of Rows Effected
        /// </summary>
        /// <param name="Query"></param>
        /// <param name="QueryParams"></param>
        /// <returns></returns>
        public async Task<int> SQLQuery(string Query, params object[] QueryParams)
        {
        
            return await this.Context.Database.ExecuteSqlRawAsync(Query, QueryParams);
        }

        public IQueryable<T> SQLCondition(string Condition, params object[] QueryParams)
        {
            this.DbSet.FromSqlInterpolated($"SELECT * FROM  {typeof(T).FullName} where {string.Format(Condition, QueryParams)}");
            return DbSet;
        }


        public void Remove(T entity)
        {
            DbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entity)
        {
            DbSet.RemoveRange(entity);
        }

       

    }
}
