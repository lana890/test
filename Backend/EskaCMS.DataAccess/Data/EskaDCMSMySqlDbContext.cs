using Microsoft.EntityFrameworkCore;

namespace EskaCMS.DataAccess.Data
{
    public class EskaDCMSMySqlDbContext : EskaDCMSDbContext
    {
        public EskaDCMSMySqlDbContext(DbContextOptions<EskaDCMSMySqlDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}