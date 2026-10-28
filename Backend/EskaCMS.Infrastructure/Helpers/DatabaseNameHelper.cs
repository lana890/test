using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Helpers
{
    public static class DatabaseNameHelper
    {
        #region Helper Methods

        public static string GetDataBaseName(IConfiguration _configuration)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var parameters = connectionString.Split(";");
            foreach (var parameter in parameters)
            {
                if (parameter.Trim().StartsWith("database=", StringComparison.OrdinalIgnoreCase))
                {
                    return parameter.Split('=')[1];
                }
            }

            return string.Empty;
        }



        public static void ConfigureDbContext(DbContextOptionsBuilder options, Action configureProvider)
        {
            configureProvider();
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandError));
            options.ConfigureWarnings(warnings=>warnings.Ignore(CoreEventId.SaveChangesFailed));
        }
        #endregion
    }
}
