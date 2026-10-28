using EskaCMS.DataAccess.Data;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EskaCMS.DataAccess.Factory
{
    public class EskaDCMSSqlServerDbContextFactory

     : IDesignTimeDbContextFactory<EskaDCMSSqlServerDbContext>

    {

        public EskaDCMSSqlServerDbContext CreateDbContext(string[] args)

        {
            var hostPath = DesignTimeDbContextFactoryHelper.ResolveHostPath();
            DesignTimeDbContextFactoryHelper.EnsureModulesLoaded(hostPath);

            var configuration = new ConfigurationBuilder()

                .SetBasePath(hostPath)

                .AddJsonFile("appsettings.json", optional: false)

                .Build();



            var optionsBuilder = new DbContextOptionsBuilder<EskaDCMSSqlServerDbContext>();

            var connectionString = configuration.GetConnectionString("DefaultConnection");



            optionsBuilder.UseSqlServer(

                connectionString,

                b =>
                {
                    b.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    b.MigrationsAssembly("EskaCMS.DataAccess");
                }

            );
     

            return new EskaDCMSSqlServerDbContext(optionsBuilder.Options);

        }

    }
}
