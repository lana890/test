using EskaCMS.Core.Areas.Core.Interfaces;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Helpers;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.Services
{
    public class ExcelGenerateServices : IExcelGenerateServices
    {
        public async Task<ExcelFile> Generate(ExcelDataVm data)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Excel_Generator");

                    AddHeaders(worksheet, data.Headers);
                    AddContent(worksheet, data.Headers, data.Sections);
                    
                    return new ExcelFile("output" + ".xlsx", package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void AddHeaders(ExcelWorksheet worksheet, List<ViewModels.KeyValuePair>? headers)
        {
            try
            {

                if (headers == null) return;

                int rowIndex = 1;
                int colIndex = 1;

                foreach (var header in headers)
                {
                    worksheet.Cells[rowIndex, colIndex].Value = header.Value;
                    colIndex++;
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void AddContent(ExcelWorksheet worksheet, List<ViewModels.KeyValuePair>? headers, List<ExcelSectionVm> sections)
        {
            try
            {

                int rowIndex = 2;

                foreach (var section in sections)
                {
                    AddSectionHeader(worksheet, section, headers, ref rowIndex);
                    AddSectionContent(worksheet, headers, section.Content, ref rowIndex);
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void AddSectionHeader(ExcelWorksheet worksheet, ExcelSectionVm section, List<ViewModels.KeyValuePair>? headers, ref int rowIndex)
        {
            try
            {

                if (string.IsNullOrEmpty(section.SectionName)) return;

                int lastColumnIndex = headers?.Count ?? ((IDictionary<string, object>)section.Content[0]).Count;
                worksheet.Cells[rowIndex, 1, rowIndex, lastColumnIndex].Merge = true;
                worksheet.Cells[rowIndex, 1].Value = section.SectionName;
                worksheet.Cells[rowIndex, 1].Style.Font.Bold = true;
                rowIndex++;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void AddSectionContent(ExcelWorksheet worksheet, List<ViewModels.KeyValuePair>? headers, List<ExpandoObject> content, ref int rowIndex)
        {
            try
            {

                foreach (var contentItem in content)
                {
                    var contentDict = (IDictionary<string, object>)contentItem;

                    if (headers == null)
                    {
                        AddContentWithoutHeaders(worksheet, contentDict, ref rowIndex);
                    }
                    else
                    {
                        AddContentWithHeaders(worksheet, headers, contentDict, ref rowIndex);
                    }
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void AddContentWithoutHeaders(ExcelWorksheet worksheet, IDictionary<string, object> contentDict, ref int rowIndex)
        {
            try
            {

                int colIndex = 1;

                foreach (var content in contentDict)
                {
                    worksheet.Cells[rowIndex, colIndex].Value = content.Value;
                    colIndex++;
                }

                rowIndex++;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void AddContentWithHeaders(ExcelWorksheet worksheet, List<ViewModels.KeyValuePair> headers, IDictionary<string, object> contentDict, ref int rowIndex)
        {
            try
            {
                int colIndex = 1;

                foreach (var header in headers)
                {
                    if (contentDict.ContainsKey(header.Key))
                    {
                        worksheet.Cells[rowIndex, colIndex].Value = contentDict[header.Key];
                    }
                    colIndex++;
                }

                rowIndex++;

            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}