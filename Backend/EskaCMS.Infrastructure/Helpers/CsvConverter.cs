using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EskaCMS.Infrastructure.Helpers
{
    public static class CsvConverter
    {
        public static IList<T> ReadCsvStream<T>(Stream stream, bool skipFirstLine = true, string csvDelimiter = ",") where T : new()
        {
            var records = new List<T>();
            var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(csvDelimiter.ToCharArray());
                if (skipFirstLine)
                {
                    skipFirstLine = false;
                }
                else
                {
                    var itemTypeInGeneric = records.GetType().GetTypeInfo().GenericTypeArguments[0];
                    var item = new T();
                    var properties = item.GetType().GetProperties();
                    for (int i = 0; i < values.Length; i++)
                    {
                        properties[i].SetValue(item, Convert.ChangeType(values[i], properties[i].PropertyType), null);
                    }

                    records.Add(item);
                }
            }

            return records;
        }

        public static string ExportCsv<T>(IList<T> data, List<string> includedProperties = null, bool includeHeader = true, string csvDelimiter = ",") where T : new()
        {
            var type = data.GetType();
            Type itemType;

            if (type.GetGenericArguments().Length > 0)
            {
                itemType = type.GetGenericArguments()[0];
            }
            else
            {
                itemType = type.GetElementType();
            }

            var stringWriter = new StringWriter();

            if (includeHeader)
            {

                if (includeHeader)
                {
                    stringWriter.WriteLine(
                        string.Join<string>(
                            csvDelimiter, itemType.GetProperties().Select(x => x.Name).Where(x => includedProperties == null || includedProperties.Contains(x))
                        )
                    );

                }
            }

            foreach (var obj in data)
            {
                var dd = obj.GetType().GetProperties().Select(x => x.Name);
                var objType = obj.GetType();
                var valueProperties = objType.GetProperties().Where(x => includedProperties == null || includedProperties.Contains(x.Name));
                var propertyValues = valueProperties.Select(pi => pi.GetValue(obj, null));

                string line = string.Empty;
                foreach (var val in propertyValues)
                {
                    if (val != null)
                    {
                        var escapeVal = val.ToString();
                        //Check if the value contans a comma and place it in quotes if so
                        if (escapeVal.Contains(","))
                        {
                            escapeVal = string.Concat("\"", escapeVal, "\"");
                        }

                        //Replace any \r or \n special characters from a new line with a space
                        if (escapeVal.Contains("\r"))
                        {
                            escapeVal = escapeVal.Replace("\r", " ");
                        }

                        if (escapeVal.Contains("\n"))
                        {
                            escapeVal = escapeVal.Replace("\n", " ");
                        }

                        line = string.Concat(line, escapeVal, csvDelimiter);
                    }
                    else
                    {
                        line = string.Concat(line, string.Empty, csvDelimiter);
                    }
                }

                stringWriter.WriteLine(line.TrimEnd(csvDelimiter.ToCharArray()));
            }

            return stringWriter.ToString();
        }
    }
}
