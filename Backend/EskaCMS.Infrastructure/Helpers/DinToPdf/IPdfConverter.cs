using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Helpers.DinToPdf
{
    public interface IpdfConverter
    {
        Task<PdfFile> Convert(string htmlContent, IList<PdfData> pdfData, string fileName);
        PdfFile GenerateTableHtml<T>(IEnumerable<T> items);
    }
}
