using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.ADIntegration.ViewModels
{
    public class CoreResponseViewModel<T>
    {
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsError { get; set; }

    }
    public class EskaCoreResponseViewModel<T>
    {
        public T content { get; set; }

    }
    public class EskaCoreBaseResponseViewModel<T> : EskaCoreResponseViewModel<T>
    {
        public string code { get; set; }
        public string message { get; set; }
    }

    public class EskaCoreBaseResponseViewModel
    {
        public string code { get; set; }
        public string message { get; set; }

    }
    public class CoreAuthResponseVM
    {
        public int CompanyId { get; set; }
        public int BranchId { get; set; }
        public string Username { get; set; }
        public string SessionId { get; set; }
        public double Timeout { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
    public class InsertUserBranchVM
    {
        public string Username { get; set; }
        public string AuthToken { get; set; }
        public string CREATED_BY { get; set; }
        public int CRG_COM_ID { get; set; }
        public int CRG_BRN_ID { get; set; }
    }
    public class CoreGroupViewModel
    {
        public int ID { get; set; }
        public string NAME { get; set; }
        public string NAME2 { get; set; }
    }
    public class InsertCoreGroupVM
    {
        public string USERNAME { get; set; }
        public string AuthToken { get; set; }
        public int CSR_GRP_ID { get; set; }
        public int CRG_COM_ID { get; set; }
    }
    public class CoreUserViewModel
    {
        public int ID { get; set; }
        public string NAME { get; set; }
        public string USERNAME { get; set; }
    }
    public class CoreAuthViewModel
    {
        public int CompanyId { get; set; }
        public int BranchId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IncludeRefreshToken { get; set; }
    }

    public class CoreWPAuthViewModel
    {
        public int CompanyId { get; set; }
        public int BranchId { get; set; }
        public string Username { get; set; }
        public bool IncludeRefreshToken { get; set; }
    }
    public class CoreAuthRequestVM
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }

    }
    public class EskaCoreInsertUserVM
    {
        public string USERNAME { get; set; }
        public string NAME { get; set; }
        public string EFFECTIVE_DATE { get; set; }
        public string EXPIRY_DATE { get; set; }
        public int STATUS { get; set; }
        public string STATUS_DATE { get; set; }
        public string IP_ADDRESS { get; set; }
        public string PASSWORD { get; set; }
        public int CRG_COM_ID { get; set; }
        public int CRG_BRN_ID { get; set; }
        public string EMAIL { get; set; }
        public int PROF_ID { get; set; }
        public string CREATED_BY { get; set; }
        public int FAILED_LOGIN_ATTEMPTS { get; set; }
        public DateTime BIRTH_DATE { get; set; }
        public int UI_THEME { get; set; }

    }

}
