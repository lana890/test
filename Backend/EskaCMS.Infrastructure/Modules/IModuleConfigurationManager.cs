using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Infrastructure.Modules
{
    public interface IModuleConfigurationManager
    {
        IEnumerable<ModuleInfo> GetModules();
    }
}
