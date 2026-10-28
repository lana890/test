using iText.Html2pdf;
using System;
using System.IO;

namespace EskaCMS.Infrastructure.Helpers
{
    public static class PdfHelper
    {
        // if you add destination Path and file name  it will save pdf file then return it
        public static PdfFile Generate(string template, dynamic data, string fileName)
        {
            string filledHtml = FillTemplateWithData(template, data);

            using (MemoryStream ms = new MemoryStream())
            {
                // iText7 (pdfhtml) HTML->PDF conversion. Replaces the legacy iTextSharp 5.x path
                // (PdfWriter.GetInstance + HTMLWorker), which is a .NET Framework-only library and
                // fails at runtime on .NET 10. iText7 8.x is already referenced and is .NET 10 compatible.
                HtmlConverter.ConvertToPdf(filledHtml, ms);

                byte[] pdfBytes = ms.ToArray();

                return new PdfFile($"{fileName}.pdf", pdfBytes, "application/pdf");
            }
        }

        public static PdfFile GenerateWithSave(string template, dynamic data, string fileName, string destinationPath)
        {
            if (string.IsNullOrEmpty(destinationPath) || string.IsNullOrEmpty(fileName))
            {
                throw new Exception("Please provide  file name and path  to save the PDF.");
            }

            var file = Path.Combine(destinationPath, $"{fileName}.pdf");
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            var pdfData = Generate(template, data, fileName);

            File.WriteAllBytes(file, pdfData.FileBytes);

            return pdfData;

        }
        private static string FillTemplateWithData(string templateHtml, dynamic data)
        {
            // Replace placeholders in the template HTML with dynamic data
            foreach (var property in data.GetType().GetProperties())
            {
                string placeholder = $"{{{property.Name}}}";
                var value = Convert.ToString(property.GetValue(data));
                templateHtml = templateHtml.Replace(placeholder, value);
            }

            return templateHtml;
        }

        private static void CreateIfMissing(string path)
        {
            bool folderExists = Directory.Exists(path);
            if (!folderExists)
                Directory.CreateDirectory(path);
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

    #endregion

}
