using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>The <c>staticType</c> coercion of a static value.</summary>
    public static class StaticValues
    {
        /// <summary>
        /// condition-utils.util.ts <c>normalizeStaticValue</c> (conditions, getDataFrom: static):
        /// <c>true</c>/<c>false</c> → booleans, <c>null</c> → JS null, <c>undefined</c> → C# null,
        /// <c>number</c> → <c>Number(v)</c> (0 when v is null, undefined or ""),
        /// <c>string</c> → <c>v ?? ''</c> (a number stays a number); anything else → v unchanged.
        /// </summary>
        public static JToken? Normalize(JToken? value, string? staticType)
        {
            switch (staticType)
            {
                case "true":
                    return new JValue(true);
                case "false":
                    return new JValue(false);
                case "null":
                    return JValue.CreateNull();
                case "undefined":
                    return null;
                case "number":
                    return NumberOf(value);
                case "string":
                    return Js.IsNil(value) ? new JValue(string.Empty) : value;
                default:
                    return value;
            }
        }

        /// <summary>
        /// dynamic-api-caller.service.ts <c>normalizeStaticValue</c> (dataMapping, typeOfValue
        /// static/categories). Same as <see cref="Normalize"/> except <c>string</c>, which is
        /// <c>String(v)</c> there: a number becomes text and a missing value becomes the
        /// text "undefined" (null → "null") — the TS quirk, kept on purpose.
        /// </summary>
        public static JToken? NormalizeForMapping(JToken? value, string? staticType) =>
            staticType == "string" ? new JValue(Js.ToJsString(value)) : Normalize(value, staticType);

        private static JToken? NumberOf(JToken? value)
        {
            if (Js.IsNil(value)) return Js.FromNumber(0);
            if (Js.TypeOf(value) == JsType.String && Js.StringValue(value!).Length == 0) return Js.FromNumber(0);
            return Js.FromNumber(Js.ToNumber(value));
        }
    }
}
