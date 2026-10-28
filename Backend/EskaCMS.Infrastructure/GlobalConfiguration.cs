
using EskaCMS.Infrastructure.Modules;
using System;
using System.Collections.Generic;
using EskaCMS.Infrastructure.Enums;


namespace EskaCMS.Infrastructure
{
    public static class GlobalConfiguration
    {

        public static IList<ModuleInfo> Modules { get; set; } = new List<ModuleInfo>();
       // public static IList<Culture> Cultures { get; set; } = new List<Culture>();
 
        public static string DefaultCulture => "en-US";
        public static string ArCulture => "ar-JO";
        //   public static  IList<CurrencyRates> Currencies { get; set; } = new List<CurrencyRates>();

        public static string WebRootPath { get; set; }

        public static string ContentRootPath { get; set; }
        public static EProvider? CurrentDatabaseProvider { get; set; }
        public static bool ForceModelSeedData { get; set; }
        public static bool IncludeOracleModelSeedData =>
            string.Equals(
                Environment.GetEnvironmentVariable("ESKA_INCLUDE_ORACLE_MODEL_SEED"),
                "1",
                StringComparison.Ordinal);
        public static bool SkipModelSeedData =>
            CurrentDatabaseProvider == EProvider.Oracle &&
            !ForceModelSeedData &&
            !IncludeOracleModelSeedData;
        public static IList<string> AngularModules { get; } = new List<string>();
        public static void RegisterAngularModule(string angularModuleName)
        {
            AngularModules.Add(angularModuleName);
        }
    }
}
