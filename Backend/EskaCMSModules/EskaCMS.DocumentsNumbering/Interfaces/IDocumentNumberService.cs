using EskaCMS.Core.Models;
using EskaCMS.DocumentsNumbering.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EskaCMS.DocumentsNumbering.Interfaces
{
    public interface IDocumentNumberService
    {
        Task<DocumentsNumbersResponseVM> Create(DocumentsNumbersAddVM request);
        Task<string> Update(DocumentsNumbersUpdateVM request);
        Task<ListResponseVM<DocumentsNumbersResponseVM>> GetList(ListRequestVM<DocumentsNumbersListRequestVM> param);
        Task<DocumentsNumbersResponseVM> Get(string id);
        Task<LatestDocumentNumberVM> GetUserLatestDocumentNo();
        Task<string> GetDocumentAuthorizedBy();
        Task LinkSoftCopy(LinkSoftCopyVM Model);
        Task<List<UserDDLVM>> GetUserPersonnel(bool WithManager);
    }

}
