using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EskaCMS.Integrations.Helpers
{
    public static class JsonCartExtensions
    {
        public static JToken GetTokenSafe(this JObject obj, params string[] paths)
        {
            if (obj == null) return null;
            foreach (var path in paths)
            {
                var token = obj.SelectToken(path, errorWhenNoMatch: false);
                if (token != null && token.Type != JTokenType.Null)
                    return token;
            }
            return null;
        }

        public static string GetStringSafe(this JObject obj, params string[] paths)
        {
            return obj.GetTokenSafe(paths)?.ToString();
        }

        // Alias so existing code using GetString(...) compiles
        public static string GetString(this JObject obj, params string[] paths)
        {
            return obj.GetStringSafe(paths);
        }

        public static int GetIntSafe(this JObject obj, int defaultValue = 0, params string[] paths)
        {
            var value = obj.GetStringSafe(paths);
            return int.TryParse(value, out var x) ? x : defaultValue;
        }

        public static long GetLongSafe(this JObject obj, long defaultValue = 0, params string[] paths)
        {
            var value = obj.GetStringSafe(paths);
            return long.TryParse(value, out var x) ? x : defaultValue;
        }

        public static decimal GetDecimalSafe(this JObject obj, decimal defaultValue = 0m, params string[] paths)
        {
            var value = obj.GetStringSafe(paths);
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var x) ? x : defaultValue;
        }

        public static DateTime? GetDateSafe(this JObject obj, params string[] paths)
        {
            var token = obj.GetTokenSafe(paths);
            if (token == null) return null;
            if (token.Type == JTokenType.Date) return token.Value<DateTime>();
            if (DateTime.TryParse(token.ToString(), out var dt)) return dt;
            return null;
        }

        public static JObject GetObjectSafe(this JObject obj, params string[] paths)
        {
            var token = obj.GetTokenSafe(paths);
            if (token == null) return null;

            if (token.Type == JTokenType.Object)
                return (JObject)token;

            if (token.Type == JTokenType.String)
            {
                var str = token.ToString();
                if (!string.IsNullOrWhiteSpace(str) && str.TrimStart().StartsWith("{"))
                    return JObject.Parse(str);
            }
            return null;
        }

        public static JObject GetObject(this JObject obj, params string[] paths)
        {
            return obj.GetObjectSafe(paths);
        }

        public static JArray GetArraySafe(this JObject obj, params string[] paths)
        {
            var token = obj.GetTokenSafe(paths);
            if (token == null) return null;
            return token.Type == JTokenType.Array ? (JArray)token : null;
        }


        public static void SetIfMissing(this JObject obj, string key, JToken value)
        {
            if (obj == null || string.IsNullOrWhiteSpace(key) || value == null) return;
            var current = obj[key];
            if (current == null || current.Type == JTokenType.Null || string.IsNullOrWhiteSpace(current.ToString()))
                obj[key] = value.DeepClone();
        }
    }
}