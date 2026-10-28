using System;
using System.Linq;
using System.Security.Claims;
using EskaCMS.Components.Data;
using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Elements.Data;
using EskaCMS.EskaCoreReports.Data;
using EskaCMS.Forms.Data;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Data;
using EskaCommerce.Catalog.Data;
using EskaCommerce.Comments.Data;
using EskaCommerce.Module.EmailSenderSmtp.Data;
using EskaCommerce.Module.ShippingFree.Data;
using EskaCommerce.Payments.Data.PaymentCoD;
using EskaCommerce.Payments.Data.PaymentHyperPay;
using EskaCommerce.Payments.Data.PaymentQuickPay;
using EskaCommerce.Payments.Data;
using EskaCommerce.Shipping.Data;
using EskaCommerce.ShippingJeebly.Data;
using EskaCommerce.ShippingTableRate.Data;
using EskaCommerce.TelrPayment.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.DataAccess.Data;
using EskaCMS.Infrastructure.Enums;
using Microsoft.Extensions.Configuration;
using System.IO;
using EskaCMS.Infrastructure.DbFactory;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.EntityFramework.Storage;
using IdentityModel;
using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EskaCMS.WebHost.IdentityServer
{
    public class SeedData
    {
        private static ServerVersion GetMySqlServerVersion(IConfiguration configuration)
        {
            var configuredVersion = configuration.GetValue<string>("ConnectionStrings:MySqlServerVersion");
            return string.IsNullOrWhiteSpace(configuredVersion)
                ? new MySqlServerVersion(new Version(8, 0, 0))
                : ServerVersion.Parse(configuredVersion);
        }

        public static void EnsureSeedData(string connectionString, IServiceCollection services)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            EProvider Provider = DbProviderHelper.ParseProvider(config.GetValue<string>("ConnectionStrings:DatabaseProvider"));
            var mySqlServerVersion = GetMySqlServerVersion(config);
            services.AddLogging();
            if (Provider == EProvider.PostgreSQL)
                services.AddDbContext<EskaDCMSDbContext, EskaDCMSMyPostgresDbContext>(options =>
                {
                    DatabaseNameHelper.ConfigureDbContext(options, () =>

                    options.UseNpgsql(connectionString,
                      b => b.MigrationsAssembly("EskaCMS.DataAccess")));


                });
            else if (Provider == EProvider.SqlServer)
            {
                services.AddDbContext<EskaDCMSDbContext, EskaDCMSSqlServerDbContext>(options =>
                {

                    DatabaseNameHelper.ConfigureDbContext(options, () =>
                    options.UseSqlServer(connectionString,
                      b =>
                      {
                          b.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                          b.MigrationsAssembly("EskaCMS.DataAccess");
                      }));


                });
            }
            else if (Provider == EProvider.Oracle)
            {
                services.AddDbContext<EskaDCMSDbContext, EskaDCMSOracleDbContext>(options =>
                {
                    DatabaseNameHelper.ConfigureDbContext(options, () =>
                    options.UseOracle(connectionString,
                      b => b.MigrationsAssembly("EskaCMS.DataAccess")));
                });
            }

            else if (Provider == EProvider.MySql)
                services.AddDbContext<EskaDCMSDbContext, EskaDCMSMySqlDbContext>(options =>
                  {
                      DatabaseNameHelper.ConfigureDbContext(options, () =>

                      options.UseMySql(connectionString, mySqlServerVersion,
                        b => b.MigrationsAssembly("EskaCMS.DataAccess")));


                  });

            services.AddIdentity<User, Role>()
        .AddEntityFrameworkStores<EskaDCMSDbContext>()
        .AddDefaultTokenProviders();
            if (Provider == EProvider.PostgreSQL)
            {
                services.AddOperationalDbContext(options =>
                {

                    options.ConfigureDbContext = db =>
                    db.UseNpgsql(connectionString,
                           b => b.MigrationsAssembly("EskaCMS.DataAccess"));
                });
                services.AddConfigurationDbContext(options =>
                {
                    options.ConfigureDbContext = db =>
                      db.UseNpgsql(connectionString,
                             b => b.MigrationsAssembly("EskaCMS.DataAccess"));
                });
            }
            else if (Provider == EProvider.MySql)
            {
                services.AddOperationalDbContext(options =>
                {

                    options.ConfigureDbContext = db =>
                    db.UseMySql(connectionString, mySqlServerVersion,
                           b => b.MigrationsAssembly("EskaCMS.DataAccess"));
                });
                services.AddConfigurationDbContext(options =>
                {
                    options.ConfigureDbContext = db =>
                      db.UseMySql(connectionString, mySqlServerVersion,
                             b => b.MigrationsAssembly("EskaCMS.DataAccess"));
                });
            }
            else if (Provider == EProvider.Oracle)
            {
                services.AddOperationalDbContext(options =>
                {

                    options.ConfigureDbContext = db =>
                    db.UseOracle(connectionString,
                           b => b.MigrationsAssembly("EskaCMS.DataAccess"));
                });
                services.AddConfigurationDbContext(options =>
                {
                    options.ConfigureDbContext = db =>
                      db.UseOracle(connectionString,
                             b => b.MigrationsAssembly("EskaCMS.DataAccess"));
                });
            }
            else if (Provider == EProvider.SqlServer)
            {
                services.AddOperationalDbContext(options =>
                {

                    options.ConfigureDbContext = db =>
                    db.UseSqlServer(connectionString,
                           b =>
                           {
                               b.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                               b.MigrationsAssembly("EskaCMS.DataAccess");
                           });
                });
                services.AddConfigurationDbContext(options =>
                {
                    options.ConfigureDbContext = db =>
                      db.UseSqlServer(connectionString,
                             b =>
                             {
                                 b.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                                 b.MigrationsAssembly("EskaCMS.DataAccess");
                             });
                });
            }
            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                scope.ServiceProvider.GetService<PersistedGrantDbContext>().Database.Migrate();

                var context = scope.ServiceProvider.GetService<ConfigurationDbContext>();
                context.Database.Migrate();

                // EnsureSeedData(scope);
                EnsureSeedData(context);

                var ctx = scope.ServiceProvider.GetService<EskaDCMSDbContext>();
                ctx.Database.Migrate();
                if (Provider == EProvider.Oracle)
                {
                    EnsureOracleModelSeedData(connectionString);
                }
                EnsureUsers(scope);
            }
        }

        private static void EnsureUsers(IServiceScope scope)
        {
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            const string seedUserName = "f.sirhan@eskadenia.com";
            const string seedEmail = "f.sirhan@eskadenia.com";

            var alice = userMgr.FindByNameAsync(seedUserName).Result
                ?? userMgr.FindByEmailAsync(seedEmail).Result;
            if (alice == null)
            {
                alice = new User
                {
                    UserName = seedUserName,
                    Email = seedEmail,
                    EmailConfirmed = true,
                    FullName = "FirasSirhan",
                };
                var result = userMgr.CreateAsync(alice, "Eska@123").Result;
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }
                else
                {
                    result = userMgr.AddClaimsAsync(alice, new Claim[]
                {
                      new Claim(JwtClaimTypes.Name, "Firas Sirhan"),
                      new Claim(JwtClaimTypes.GivenName, "Firas"),
                      new Claim(JwtClaimTypes.FamilyName, "Sirhan"),
                      new Claim(JwtClaimTypes.WebSite, "https://eskadenia.com"),}).Result;
                }


                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

                Log.Debug("alice created");
            }
            else
            {
                Log.Debug("alice already exists");
            }


        }


        //private static void EnsureSeedData(IServiceScope scope)
        //{
        //    IDataSeeder DataSeeder = null;
        //    try
        //    {
        //        // NOTE!!! THE ORDER IS IMPORTANT :YAZAN

        //        var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

        //        // 1 - Core
        //        DataSeeder = new CoreDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        //1 - Core
        //        DataSeeder = new SecurityDataSeeder(scopeFactory);
        //        DataSeeder.Seed();



        //        //// 2 - Component
        //        DataSeeder = new ComponentDataSeeder(scopeFactory);
        //        DataSeeder.Seed(); 
        //        //1 - Core
        //        DataSeeder =new  FormsDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new CatalogDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new ShippingDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new ElementsDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new PaymentCoDDataSeeder(scopeFactory);
        //        DataSeeder.Seed();


        //        DataSeeder = new TaxDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new EmailSenderSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new EskaCoreReportsSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new LocalizationDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new CommentsDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new PaymentCoDDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new PaymentMEPSDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new QuickPayDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new PaymentsDataSeeder(scopeFactory);
        //        DataSeeder.Seed();


        //        DataSeeder = new ProductRecentlyViewedDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new ShippingFreeDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new ShippingJeeblyDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new ShippingTableRateDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //        DataSeeder = new TelrPaymentDataSeeder(scopeFactory);
        //        DataSeeder.Seed();

        //    }
        //    catch (Exception Exp)
        //    {

        //        throw Exp;
        //    }



        //}

        private static void EnsureSeedData(ConfigurationDbContext context)
        {
            try
            {
                if (context.Clients.Count() == 0)
                {
                    Log.Debug("Clients being populated");
                    foreach (var client in IdentityServerConfig.Clients.ToList())
                    {
                        context.Clients.Add(client.ToEntity());
                    }

                    context.SaveChanges();
                }
                else
                {
                    Log.Debug("Clients already populated");
                }

                if (context.IdentityResources.Count() == 0)
                {
                    Log.Debug("IdentityResources being populated");
                    foreach (var resource in IdentityServerConfig.IdentityResources.ToList())
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }

                    context.SaveChanges();
                }
                else
                {
                    Log.Debug("IdentityResources already populated");
                }

                if (context.ApiScopes.Count() == 0)
                {
                    Log.Debug("ApiScopes being populated");
                    foreach (var resource in IdentityServerConfig.ApiScopes.ToList())
                    {
                        context.ApiScopes.Add(resource.ToEntity());
                    }

                    context.SaveChanges();
                }
                else
                {
                    Log.Debug("ApiScopes already populated");
                }

                if (context.ApiResources.Count() == 0)
                {
                    Log.Debug("ApiResources being populated");
                    foreach (var resource in IdentityServerConfig.ApiResources.ToList())
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }

                    context.SaveChanges();
                }
                else
                {
                    Log.Debug("ApiScopes already populated");
                }
            }
            catch (Exception e)
            {
                if (e.ToString().Contains("ORA-00942"))
                {
                    Log.Warning("Skipping IdentityServer configuration seed for Oracle because the IdentityServer tables do not exist yet.");
                    return;
                }

                throw;
            }

        }

        private static void EnsureOracleModelSeedData(string connectionString)
        {
            var originalForceSeed = GlobalConfiguration.ForceModelSeedData;

            try
            {
                var runtimeOptions = new DbContextOptionsBuilder<EskaDCMSOracleDbContext>();
                runtimeOptions.UseOracle(connectionString, b => b.MigrationsAssembly("EskaCMS.DataAccess"));

                using var runtimeContext = new EskaDCMSOracleDbContext(runtimeOptions.Options);

                GlobalConfiguration.ForceModelSeedData = true;
                using var seedContext = new EskaDCMSOracleDbContext(runtimeOptions.Options);
                var designTimeModel = seedContext.GetService<IDesignTimeModel>().Model;

                foreach (var entityType in designTimeModel.GetEntityTypes()
                    .Where(x => x.ClrType != null && !x.IsOwned() && x.FindPrimaryKey() != null))
                {
                    var seedRows = entityType.GetSeedData().ToList();
                    if (!seedRows.Any())
                    {
                        continue;
                    }

                    var runtimeEntityType = runtimeContext.Model.FindEntityType(entityType.Name);
                    if (runtimeEntityType == null)
                    {
                        continue;
                    }

                    foreach (var seedRow in seedRows)
                    {
                        if (SeedRowExists(runtimeContext, runtimeEntityType, seedRow))
                        {
                            continue;
                        }

                        var entity = FormatterServices.GetUninitializedObject(runtimeEntityType.ClrType);
                        var entry = runtimeContext.Entry(entity);

                        foreach (var property in runtimeEntityType.GetProperties())
                        {
                            if (!seedRow.TryGetValue(property.Name, out var value))
                            {
                                continue;
                            }

                            entry.Property(property.Name).CurrentValue = ConvertSeedValue(value, property.ClrType);
                        }

                        entry.State = EntityState.Added;
                    }

                }

                if (runtimeContext.ChangeTracker.HasChanges())
                {
                    runtimeContext.SaveChanges();
                }
            }
            finally
            {
                GlobalConfiguration.ForceModelSeedData = originalForceSeed;
            }
        }

        private static bool SeedRowExists(DbContext context, IEntityType entityType, IDictionary<string, object> seedRow)
        {
            var primaryKey = entityType.FindPrimaryKey();
            if (primaryKey == null)
            {
                return false;
            }

            var keyValues = primaryKey.Properties
                .Select(property => seedRow.TryGetValue(property.Name, out var value)
                    ? ConvertSeedValue(value, property.ClrType)
                    : null)
                .ToArray();

            if (keyValues.Any(value => value == null))
            {
                return false;
            }

            return context.Find(entityType.ClrType, keyValues) != null;
        }

        private static object ConvertSeedValue(object value, Type targetType)
        {
            if (value == null)
            {
                return null;
            }

            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType.IsEnum)
            {
                return Enum.ToObject(underlyingType, value);
            }

            if (underlyingType == typeof(Guid))
            {
                return value is Guid guid ? guid : Guid.Parse(value.ToString());
            }

            if (underlyingType == typeof(DateTimeOffset) && value is DateTime dateTime)
            {
                return new DateTimeOffset(dateTime);
            }

            if (underlyingType == typeof(bool) && value is short shortValue)
            {
                return shortValue == 1;
            }

            if (underlyingType == typeof(bool) && value is int intValue)
            {
                return intValue == 1;
            }

            if (underlyingType.IsAssignableFrom(value.GetType()))
            {
                return value;
            }

            return Convert.ChangeType(value, underlyingType);
        }


    }
}
