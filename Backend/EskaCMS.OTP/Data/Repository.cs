using EskaCMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace EskaCMS.OTP.Data
{
    // Thin repository over OtpDbContext working with the REAL entity classes (EskaCMS.Entities).
    // The main system's implementation lives in the EskaCMS.Core module and is bound to EskaDCMSDbContext,
    // so it cannot be reused without loading the modules; this one exposes the members OTP uses.
    public interface IRepositoryWithTypedId<T, TId> where T : class, IEntityWithTypedId<TId>
    {
        IQueryable<T> Query();
        void Add(T entity);
        void Update(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entity);
        void SaveChanges();
        Task SaveChangesAsync();
    }

    public interface IRepository<T> : IRepositoryWithTypedId<T, long> where T : class, IEntityWithTypedId<long>
    {
    }

    public class RepositoryWithTypedId<T, TId> : IRepositoryWithTypedId<T, TId> where T : class, IEntityWithTypedId<TId>
    {
        public RepositoryWithTypedId(OtpDbContext context)
        {
            Context = context;
            DbSet = Context.Set<T>();
        }

        protected OtpDbContext Context { get; }

        protected DbSet<T> DbSet { get; }

        public IQueryable<T> Query() => DbSet;

        public void Add(T entity) => DbSet.Add(entity);

        public void Update(T entity) => DbSet.Update(entity);

        public void Remove(T entity) => DbSet.Remove(entity);

        public void RemoveRange(IEnumerable<T> entity) => DbSet.RemoveRange(entity);

        public void SaveChanges() => Context.SaveChanges();

        public Task SaveChangesAsync() => Context.SaveChangesAsync();
    }

    public class Repository<T> : RepositoryWithTypedId<T, long>, IRepository<T> where T : class, IEntityWithTypedId<long>
    {
        public Repository(OtpDbContext context) : base(context)
        {
        }
    }
}
