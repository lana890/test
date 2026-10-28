using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.Interfaces
{
    public interface IExcelGenerateServices
    {
        Task<ExcelFile> Generate(ExcelDataVm model);
    }
}
