using EskaCMS.Core.Areas.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services.Interfaces
{
    public interface ICultureService
    {

        Task<bool> AddCulture(CultureViewModel cultureViewModel);

        Task<bool> RemoveCulture(string Id);

        Task<List<CultureViewModel>> GetCultures();

        Task<CultureViewModel> GetById(string Id);

        Task<bool> UpdateCulture(CultureViewModel cultureViewModel);
      




    }
}
