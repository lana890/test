using EskaCMS.Core.Models;
using EskaCMS.Core.ViewModels;
using EskaCMS.DocumentsNumbering.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EskaCMS.DocumentsNumbering.Interfaces
{
    public interface IDocumentLoockupsService
    {
        Task<long> CreateType(DocumentTypeAddVM request);
        Task<long> CreateClassification(DocumentClassificationAddVM request);
        Task<DocumentTypeVM> GetDocumentType(long id);
        Task<DocumentClassificationVM> GetDocumentClassification(long id);
        Task<List<DocumentClassificationVM>> GetAllDocumentClassifications();
        Task<List<DocumentTypeVM>> GetAllDocumentTypes();
        Task<List<CustomerGroupVM>> GetAuthorizedDepartments(string username);
        Task<PreparedByVM> GetUserAndManager();
        Task<List<DocumentCountryVM>> GetAllCountries();
        Task<List<UserListVM>> GetUsersByParentUsername(string username,bool? isManagerIncluded);
        Task<List<UserListVM>> GetAllUsers();
        Task<List<CustomerGroupVM>> GetAllDepartments();
        Task<List<CustomerGroupVM>> GetManagementDepartments();
    }

}
