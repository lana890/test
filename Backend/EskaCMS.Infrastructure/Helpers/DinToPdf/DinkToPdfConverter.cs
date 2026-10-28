using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DinkToPdf;
using System;
using System.IO;
using DinkToPdf.Contracts;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Helpers.DinToPdf
{
    public class DinktoPdfConverter : IpdfConverter
    {
        private IConverter _converter;

        public DinktoPdfConverter(IConverter converter)
        {
            _converter = converter;
        }

        public async  Task<PdfFile> Convert(string htmlContent , IList<PdfData> pdfData , string fileName)
        {
            string filledHtml = FillTemplateWithData(htmlContent, pdfData);

            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    
                    
                },
                Objects = {
                    new ObjectSettings() {
                        HtmlContent = filledHtml,
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            byte[] pdfBytes = _converter.Convert(doc);

            return new PdfFile($"{fileName}.pdf", pdfBytes, "application/pdf");
            
        }

        private  string FillTemplateWithData(string templateHtml, IList<PdfData> pdfData)
        {
            // Replace placeholders in the template HTML with dynamic data
            foreach (var item in pdfData)
            {
              
                templateHtml = templateHtml.Replace(item.Key, item.Value);
            }

            return templateHtml;
        }

        public PdfFile GenerateTableHtml<T>(IEnumerable<T> items)
        {
            // No items → return empty PDF file
            if (items == null || !items.Any())
                return new PdfFile("data.pdf", new byte[0], "application/pdf");

            var props = typeof(T).GetProperties();

            StringBuilder sb = new StringBuilder();
            sb.Append(@"
        <html>
        <head>
            <style>
                table { width:100%; border-collapse:collapse; font-family:Arial; }
                th, td { border:1px solid #ccc; padding:6px; }
                th { background:#e6f2ff; text-align:left; }
            </style>
        </head>
        <body>
    ");

            sb.Append("<table>");

            // Header Row
            sb.Append("<tr>");
            foreach (var prop in props)
            {
                sb.Append($"<th>{prop.Name}</th>");
            }
            sb.Append("</tr>");

            // Data Rows
            foreach (var item in items)
            {
                sb.Append("<tr>");
                foreach (var prop in props)
                {
                    var value = prop.GetValue(item) ?? "";
                    sb.Append($"<td>{value}</td>");
                }
                sb.Append("</tr>");
            }

            sb.Append("</table></body></html>");

            // Build PDF
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
            ColorMode = ColorMode.Color,
            Orientation = Orientation.Portrait,
            PaperSize = PaperKind.A4,
        },
                Objects = {
            new ObjectSettings() {
                HtmlContent = sb.ToString(),
                WebSettings = { DefaultEncoding = "utf-8" }
            }
        }
            };

            byte[] pdfBytes = _converter.Convert(doc);

            return new PdfFile("data.pdf", pdfBytes, "application/pdf");
        }


    }

    #region Helper Models

    public class PdfFile
    {
        public PdfFile()
        {

        }
        public PdfFile(string Name, byte[] FileBytes, string ContentType)
        {
            this.Name = Name;
            this.FileBytes = FileBytes;
            this.ContentType = ContentType;
        }
        public byte[] FileBytes { get; set; }
        public string ContentType { get; set; }
        public string Name { get; set; }
    }

    public class PdfData
    {

        public string Key { get; set; }
        public string Value { get; set; }
    }
    #endregion
}
