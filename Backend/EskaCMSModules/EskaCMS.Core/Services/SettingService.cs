using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Infrastructure.Models;

using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extentions.Settings;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Core;

namespace EskaCommerce.Module.Core.Services
{
    public class SettingService : ISettingService
    {
        private readonly UserManager<User> _userManager;
        private readonly SettingDefinitionProvider _settingDefinitionProvider;
        private readonly IWorkContext _workContext;
        private readonly IConfiguration _config;
        public SettingService(
            UserManager<User> userManager,
            SettingDefinitionProvider settingDefinitionProvider,
            IWorkContext workContext,
            IConfiguration config)
        {
            _userManager = userManager;
            _settingDefinitionProvider = settingDefinitionProvider;
            _workContext = workContext;
            _config = config;
        }

        /// <inheritdoc />
        public async Task<string> GetSettingValueAsync(string name,long SiteId)
        {
            return await GetSettingValueForUserAsync(await _workContext.GetCurrentUserId(), name);
        }

        /// <inheritdoc />
        public async Task<string> GetSettingValueForUserAsync(long userId, string name)
        {
            var value = await GetCustomSettingValue(userId, name) ?? _settingDefinitionProvider.GetOrNull(name)?.DefaultValue;
            return value;
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, string>> GetAllSettingsAsync(long SiteId)
        {
            var userId = await _workContext.GetCurrentUserId();
            return await GetAllSettingsForUserAsync(userId);
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, string>> GetAllSettingsForUserAsync(long userId)
        {
            var result = new Dictionary<string, string>();

            var defaultSettings = _settingDefinitionProvider.SettingDefinitions;
            var customSettings = await GetAllCustomSettings(userId) ?? new Dictionary<string, string>();
            foreach (var item in defaultSettings.Values)
            {
                result[item.Name] = item.DefaultValue;

                if (customSettings.ContainsKey(item.Name) && customSettings[item.Name] != item.DefaultValue)
                {
                    result[item.Name] = customSettings.GetOrDefault(item.Name);
                }
            }

            return result;
        }

        /// <inheritdoc />
        public async Task UpdateSettingForUserAsync(User user, string name, string value)
        {
            SetCustomSettingValueForUser(user, name, value);
            await _userManager.UpdateAsync(user);
        }

        /// <inheritdoc />
        public async Task UpdateSettingAsync(string name, string value,long SiteId)
        {
            var user = await _workContext.GetCurrentUser();
            await UpdateSettingForUserAsync(user, name, value);
        }

        /// <inheritdoc />
        public void SetCustomSettingValueForUser(User user, string name, string value)
        {
            var settings = user.GetData<IDictionary<string, string>>(User.SettingsDataKey) ?? new Dictionary<string, string>();
            var defaultSettings = _settingDefinitionProvider.SettingDefinitions;
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (defaultSettings.GetOrDefault(name)?.DefaultValue != value)
                {
                    settings[name] = value;
                }
            }
            else
            {
                settings.Remove(name);
            }
            user.SetData(User.SettingsDataKey, settings);
        }

        private async Task<string> GetCustomSettingValue(long userId, string name)
        {
            var settings = await GetAllCustomSettings(userId);
            return settings.GetOrDefault(name);
        }

        private async Task<IDictionary<string, string>> GetAllCustomSettings(long userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return user.GetData<IDictionary<string, string>>(User.SettingsDataKey) ?? new Dictionary<string, string>();
        }

        public async Task<string> GetShipmentStatus(string trackingNo)
        {
            return await dhlShipment(trackingNo);
        }
        private async Task<string> dhlShipment(string trackingNo)
        {
            var XML = _config.GetValue<string>("DHLCheckTracking");
            var url =  _config.GetValue<string>( "DHLApiURL");
            XML = XML.Replace("[AWBNumber]", trackingNo);
          

            string Response = "";

            try
            {
                HttpWebRequest myReq = null;

                myReq = WebRequest.Create(url) as HttpWebRequest;
                myReq.ContentType = "application/x-www-form-urlencoded";
                myReq.Method = "POST";

                using (System.IO.Stream stream = myReq.GetRequestStream())
                {
                    byte[] arrBytes = ASCIIEncoding.ASCII.GetBytes(XML);
                    stream.Write(arrBytes, 0, arrBytes.Length);
                    stream.Close();
                }

                WebResponse myRes = myReq.GetResponse();
                System.IO.Stream respStream = myRes.GetResponseStream();
                System.IO.StreamReader reader = new System.IO.StreamReader(respStream, System.Text.Encoding.ASCII);
                Response = reader.ReadToEnd();
                myRes.Close();
                myRes = null;
            }
            catch (Exception ex)
            {
                Response = ex.ToString();
            }

            return Response;
        }
        public string SerializeToXML<T>(T toSerialize)
        {
            string Result = "";

            XmlSerializerNamespaces ns = new XmlSerializerNamespaces(); ns.Add("", "");
            using (TextWriter tw = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(tw, new XmlWriterSettings { OmitXmlDeclaration = true }))
                {
                    new XmlSerializer(typeof(T)).Serialize(writer, toSerialize, ns);
                    Result = tw.ToString();
                }
            }

            return Result;
        }
        public T DeserializeFromXML<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(new StringReader(xml));
        }
    }
}
