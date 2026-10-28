using EskaCMS.Entities.Entity.Core;
using EskaCMS.Forms.Model;
using EskaCMS.Infrastructure.Helpers;
using EskaCommerce.Orders.ModelVM;
using EskaCommerce.Orders.ModelVM.Request;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Orders.Services.CustomersService.Interface
{
    public interface ICustomerService
    {
        Task<(List<CustomerDto> Data, int TotalCount)> GetCustomerList(string searchTerm, DateTime? dateFrom, DateTime? dateTo, EStatus? status, int page, int limit, string sortBy, ESortType sortOrder);

        Task<bool> SendEmailToBulkUsers(CustomerActions customerAction);
        Task<bool> CreateAndAssignGroup(CustomerActions customerAction);
        Task<bool> addCouponToGroup(CustomerActions customerActions);
        //Task<File> ExportExcelFile(CustomerActions customerAction, string Name);
        Task<List<User>> GetUsers(CustomerActions customerActions);



    }
}
