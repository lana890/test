using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Modules;
using System.Reflection;

namespace EskaCMS.DataAccess.Factory
{
    internal static class DesignTimeDbContextFactoryHelper
    {
        public static string ResolveHostPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();

            var candidates = new[]
            {
                currentDirectory,
                Path.Combine(currentDirectory, "..", "EskaCMS.WebHost"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "EskaCMS.WebHost")
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(Path.Combine(fullPath, "appsettings.json")) &&
                    File.Exists(Path.Combine(fullPath, ModuleConfigurationManager.ModulesFilename)))
                {
                    return fullPath;
                }
            }

            throw new DirectoryNotFoundException("Could not locate EskaCMS.WebHost for design-time configuration.");
        }

        public static void EnsureModulesLoaded(string hostPath)
        {
            if (GlobalConfiguration.Modules.Count > 0)
            {
                return;
            }

            GlobalConfiguration.ContentRootPath = hostPath;
            var modulesConfig = new ModuleConfigurationManager();

            foreach (var module in modulesConfig.GetModules())
            {
                module.Assembly = Assembly.Load(new AssemblyName(module.Id));
                GlobalConfiguration.Modules.Add(module);
            }
        }
    }
}
