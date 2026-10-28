using EskaCMS.EskaBilling.Configurations.Interface;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Configurations
{
    public  class ConfigProvider : IConfigProvider
    {
        //private static IConfigurationRoot _configuration = new ConfigurationBuilder()
        //                        .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Integration.json")).Build();

        private  IConfigurationRoot _configurationRoot;

        

        //public ConfigProvider()
        //{
            
        //    var appSettingsPath = ;
        //    var builder  = new ConfigurationBuilder()
        //                        .AddJsonFile(appSettingsPath);
                

        //    _configuration = builder.Build();
        //}

        public  string GetValue(string val)
        {
            _configurationRoot = new ConfigurationBuilder()
                                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Integration.json")).Build();
            //string value = _configuration.GetSection(Path)?.Value;
            string value = _configurationRoot.GetSection(val)?.Value;
            return value;
        }
    }
}
