using EskaCMS.Integrations.Models.Insurance;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EskaCMS.Integrations.Helpers
{
    public static class IntegrationSettingsLoader
    {


		public static T Load<T>(string rootPath, string key)
		{
			var filePath = Path.Combine(rootPath, "integration-settings.json");

			if (!File.Exists(filePath))
				throw new FileNotFoundException(
					$"Settings file not found at: {filePath}");

			var json = File.ReadAllText(filePath);

			var jObject = JObject.Parse(json);

			var section = jObject[key];

			if (section == null)
				throw new InvalidOperationException(
					$"Key '{key}' not found in JSON file.");

			var settings = section.ToObject<T>();

			if (settings == null)
				throw new InvalidOperationException(
					$"Invalid structure for type '{typeof(T).Name}' in JSON file.");

			return settings;
		}


		//public static IntegrationSettings Load(string rootPath,string key)
		//{
		//    var filePath = Path.Combine(
		//        rootPath,
		//        "integration-settings.json");

		//    if (!File.Exists(filePath))
		//        throw new FileNotFoundException(
		//            $"Integration settings file not found at: {filePath}");

		//    var json = File.ReadAllText(filePath);

		//    var jObject = JObject.Parse(json);

		//    var settings = jObject[key]
		//        ?.ToObject<IntegrationSettings>();

		//    if (settings == null)
		//        throw new InvalidOperationException(
		//            "Invalid IntegrationSettings structure in JSON file.");

		//    return settings;
		//}
	}
}