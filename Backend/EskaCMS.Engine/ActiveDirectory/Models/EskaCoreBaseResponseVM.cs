using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EskaCMS.Engine.Models
{
    public class CoreResponse<T>
    {

        public T content { get; set; }

    }
    public class EskaCoreBaseResponseVM<T> : CoreResponse<T>
    {
        public string code { get; set; }
        public string message { get; set; }
    }
    public class CoreResponseViewModel<T>
    {
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsError { get; set; }

    }

    public class HREmployee
    {
        public int? ID { get; set; }
        public int? MANAGER_ID { get; set; }
        public string NAME { get; set; }
        public string MOBILE { get; set; }
        public string USERNAME { get; set; }
        public string EMPLOYEE_RANK { get; set; }
        public string JOB_TITLE_NAME { get; set; }
        public int CRG_DPT_ID { get; set; }
        public string DEPARTMENT_NAME { get; set; }
        public string DEPARTMENT_NAME2 { get; set; }
        public string NAME2 { get; set; }
        public string EMAIL { get; set; }
        public string COMPANY_EMAIL { get; set; }
        public string PHOTO_PATH { get; set; }
        public string BIRTHDATE { get; set; }
        public string START_DATE { get; set; }
        public string PHONE2 { get; set; }
        [JsonPropertyName("STATUS")]
        public decimal? STATUS_RAW { get; set; }

        [JsonIgnore]
        public int STATUS => (int)(STATUS_RAW ?? 0);
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
    public class CoreAuthViewModel
    {
        public int CompanyId { get; set; }
        public int BranchId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IncludeRefreshToken { get; set; }
    }
    public class UserForm
    {

        public string FullName { get; set; }


        public string Email { get; set; }
        public string UserName { get; set; }


        public string Password { get; set; }

    }
    public class EskaCoreBaseResponseVM
    {
        public string code { get; set; }
        public string message { get; set; }

    }
    public class InsertCoreGroupVM
    {
        public string USERNAME { get; set; }
        public string AuthToken { get; set; }
        public int CSR_GRP_ID { get; set; }
        public int CRG_COM_ID { get; set; }
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
        public int P_APPLICATION_ID { get; set; }
        public int P_USER_TYPE { get; set; }
        public int P_CREDIT_USER_LIMIT { get; set; }
        public int P_DEBIT_USER_LIMIT { get; set; }
        public int P_IS_EXTERNAL_USER { get; set; }
        public int P_CHANNEL { get; set; }

    }
}