using EskaCMS.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EskaCMS.DataAccess.Factory
{
    public class EskaDCMSOracleDbContextFactory : IDesignTimeDbContextFactory<EskaDCMSOracleDbContext>
    {
        public EskaDCMSOracleDbContext CreateDbContext(string[] args)
        {
            var hostPath = DesignTimeDbContextFactoryHelper.ResolveHostPath();
            DesignTimeDbContextFactoryHelper.EnsureModulesLoaded(hostPath);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(hostPath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<EskaDCMSOracleDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseOracle(
                connectionString,
                b => b.MigrationsAssembly("EskaCMS.DataAccess")
            );

            return new EskaDCMSOracleDbContext(optionsBuilder.Options);
        }
    }
}
