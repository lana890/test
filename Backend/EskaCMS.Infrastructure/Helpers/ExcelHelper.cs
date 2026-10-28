using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OfficeOpenXml;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using System.Reflection.Metadata;

namespace EskaCMS.Infrastructure.Helpers
{
    public static class ExcelHelper
    {
        #region Create Template For Variations

        public static byte[] CreateTemplate<T>(string ProductVariations) where T : class
        {
            try
            {

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                    PropertyInfo[] properties = typeof(T).GetProperties();

                    for (int i = 0; i < properties.Length; i++)
                    {
                        var property = properties[i];
                        worksheet.Cells[1, i + 1].Value = property.Name;
                        worksheet.Cells[2, i + 1].Value = property.PropertyType.Name;

                        if ("ProductId".ToLower() == property.Name.ToLower())

                        {
                            worksheet.Cells[3, 2].Value = "NOTE!";
                            worksheet.Cells[4, 2].Value = "Please Delete The Data Types And This Note Before Using The Template.";
                            worksheet.Cells[4, i + 1].Value = "Note Please Insert ProductId From Apperance Values:" + System.Environment.NewLine + ProductVariations;

                        }

                    }

                    var headerCells = worksheet.Cells[1, 1, 1, properties.Length];
                    headerCells.Style.Font.Bold = true;

                    worksheet.Cells.AutoFitColumns();

                    return package.GetAsByteArray();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion

        private const char EndOfColumnKey = 'Æ';



        #region Import Excel 

        public static async Task<List<T>> ImportExcel<T>(IFormFile file, int sheetIdx) where T : new()
        {
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(stream);
                var sheet = package.Workbook.Worksheets[sheetIdx];
                if (sheet == null)
                {
                    throw new Exception($"The sheet with index {sheetIdx} does not exist.");
                }

                var headers = new List<Cell>();
                var properties = typeof(T).GetProperties();
                var result = new List<T>();

                for (var rowIndex = 1; rowIndex <= sheet.Dimension.End.Row; rowIndex++)
                {
                    if (rowIndex == 1)
                    {
                        headers = sheet.Cells[rowIndex, 1, rowIndex, sheet.Dimension.End.Column]
                            .Select(cell => new Cell()
                            {
                                Value = cell.Value.ToString().Trim(),
                                Y = cell.Address.FirstOrDefault().ToString().Trim(),
                                X = cell.Address.LastOrDefault().ToString().Trim()
                            })
                            .ToList();
                    }
                    else
                    {
                        var rowCells = sheet.Cells[rowIndex, 1, rowIndex, sheet.Dimension.End.Column]
                                .Select(cell => new Cell()
                                {
                                    Value = cell.Value == null ? null : cell.Value.ToString().Trim(),
                                    Y = cell.Address.FirstOrDefault().ToString().Trim(),
                                    X = cell.Address.LastOrDefault().ToString().Trim()
                                })
                                .ToList();
                        var obj = new T();

                        foreach (var property in properties)
                        {
                            var header = headers.FirstOrDefault(h => h.Value == property.Name);
                            if (header != null)
                            {
                                var cell = rowCells.FirstOrDefault(c => c.Y == header.Y);
                                if (cell != null)
                                {
                                    if (property.PropertyType == typeof(DateTimeOffset))
                                    {
                                        var value = ConvertFromString(cell.Value);
                                        property.SetValue(obj, value);
                                    }
                                    else
                                    {
                                        var value = Convert.ChangeType(cell.Value, property.PropertyType);
                                        property.SetValue(obj, value);
                                    }

                                }
                            }


                        }

                        result.Add(obj);
                    }
                }

                return result;
            }
        }

        public static async Task<List<T>> ImportExcel<T>(string filePath) where T : new()
        {
            IFormFile formFile = GetFormFileFromPath(filePath);
            return await ImportExcel<T>(formFile, 0);
        }

        public static async Task<List<T>> ImportExcel<T>(string filePath, int sheetIdx) where T : new()
        {
            IFormFile formFile = GetFormFileFromPath(filePath);
            return await ImportExcel<T>(formFile, sheetIdx);
        }

        public static async Task<List<T>> ImportExcel<T>(IFormFile file) where T : new()
        {

            if (file == null)
            {
                throw new FileNotFoundException($"The file is null.");
            }

            return await ImportExcel<T>(file, 0);
        }


        static IFormFile GetFormFileFromPath(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"The file {filePath} does not exist.");
            }
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var formFile = new FormFile(stream, 0, stream.Length, fileInfo.Name, fileInfo.Name);
            return formFile;
        }

        #endregion

        #region Exporting Excel

        public static async Task<ExcelFile> ExportExcelTemplate<T>(string FileName, string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            return await ExportExcelTemplateWithPassword<T>(FileName, null, ContentType);


        }


        public static async Task<ExcelFile> ExportExcelTemplateWithPassword<T>(string FileName, string Password, string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                StringBuilder content = FillFileHeaders<T>();
                content = FillDataTypes<T>(content);

                content.Append("NOTE!");
                content.Append(Environment.NewLine);
                content.Append("Please Delete The Data Types And This Note Before Using The Template.");
                return await GenerateExcelFile(package, content, FileName, ContentType, EndOfColumnKey, Password);
            }


        }
        public static async Task<ExcelFile> ExportExcelFile<T>(List<T> DataList, string FileName, string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",List<string>? ExcludedProperties=null)
        {
            return await ExportExcelFileWithPassword<T>(DataList, FileName, null, ContentType, ExcludedProperties);

        }


        public static async Task<ExcelFile> ExportExcelFileWithPassword<T>(List<T> DataList, string FileName, string Password, string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", List<string>? ExcludedProperties = null)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                StringBuilder content = FillFileHeaders<T>(ExcludedProperties);
                content = FillFileData<T>(content, DataList, ExcludedProperties);

                return await GenerateExcelFile(package, content, FileName, ContentType, EndOfColumnKey, Password);
            }


        }


        #endregion


        #region Helper Functions

        private static DateTimeOffset ConvertFromString(string offsetString)
        {

            DateTimeOffset offset;
            if (!DateTimeOffset.TryParse(offsetString, out offset))
            {
                offset = DateTimeOffset.Now;
            }

            return offset;
        }


        public static async Task<ExcelFile> GenerateExcelFile(ExcelPackage package, StringBuilder content, string FileName, string ContentType, char Delimiter, string Password = null)
        {
            // Add a new worksheet to the Excel package
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Sheet1");

            worksheet.Cells.LoadFromText(content.ToString(), new ExcelTextFormat() { Delimiter = Delimiter });

            if (!string.IsNullOrEmpty(Password))
            {
                worksheet.Protection.IsProtected = false;
                worksheet.Protection.SetPassword(Password);
                worksheet.Protection.IsProtected = true;

                package.Encryption.Password = Password;
            }

            // Save the Excel package to a file
            byte[] fileContents = await package.GetAsByteArrayAsync();
            return new ExcelFile(FileName + ".xlsx", fileContents, ContentType);
        }



        private static StringBuilder FillFileHeaders<T>(List<string>? ExcludedProperties=null)
        {
            var content = new StringBuilder();

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (ExcludedProperties != null)
            {
                properties = properties.Where(x => ExcludedProperties.Contains(x.Name)).ToArray();
              
            }

            for (int i = 0; i < properties.Length; i++)
            {
                content.Append(properties[i].Name);
                if (i < properties.Length - 1)
                {
                    content.Append(EndOfColumnKey);
                }
            }



            content.Append(Environment.NewLine);
            return content;
        }

        private static StringBuilder FillDataTypes<T>(StringBuilder content)
        {

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < properties.Length; i++)
            {
                content.Append(properties[i].PropertyType.Name);
                if (i < properties.Length - 1)
                {
                    content.Append(",");
                }
            }

            content.Append(Environment.NewLine);
            return content;
        }


        private static StringBuilder FillFileData<T>(StringBuilder fileContent, List<T> DataList,List<string>? ExcludedProperties= null)
        {

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (ExcludedProperties != null)
            {
                properties = properties.Where(x => ExcludedProperties.Contains(x.Name)).ToArray();

            }
            foreach (T Item in DataList)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    dynamic dynamicObject = Item;
                    var value = dynamicObject.GetType().GetProperty(properties[i].Name).GetValue(dynamicObject);
                    if (value != null)
                    {
                        fileContent.Append(value.ToString());

                    }


                    if (i < properties.Length - 1)
                    {
                        fileContent.Append(EndOfColumnKey);
                    }
                }
                fileContent.Append(Environment.NewLine);
            }
            return fileContent;
        }

        #endregion
    }



    #region Helper Models
    internal class Cell
    {
        public string X { get; set; }
        public string Y { get; set; }
        public string Value { get; set; }
    }

    public class ExcelFile
    {
        public ExcelFile()
        {

        }
        public ExcelFile(string Name, byte[] FileBytes, string ContentType)
        {
            this.Name = Name;
            this.FileBytes = FileBytes;
            this.ContentType = ContentType;
        }
        public byte[] FileBytes { get; set; }
        public string ContentType { get; set; }
        public string Name { get; set; }
    }

    #endregion


}
