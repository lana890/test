using EskaCMS.Elements.ViewModels;
using EskaCMS.Entities.Entity.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Elements.Services.IServices
{
    public interface IElementService
    {
        Task<ElementsCategoriesViewModel> GetAllSheardElements(long SiteId, long? CultureId);
        Task<string> ReplaceSharedCodesWithSettings(string Settings);
        Task<bool> RemoveSharedElement(long ElementId);
        Task<List<ShareableElement>> ShareElements(List<ElementsVM> Elements, long SiteId);
        Task<ShareableElement> GetSharedElementById(long ElementId);
        Task<bool> UpdateSharedElementSettings(ElementsVM Element);

        Task<Dictionary<string, string>> GetAllSheardElementsSettings(long SiteId, long CultureId);

        Task<string> GetElementSettingsByCode(string Code, long? Culture);
    }
}
