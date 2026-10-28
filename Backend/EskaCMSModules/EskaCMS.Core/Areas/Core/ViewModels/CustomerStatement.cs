using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class CustomerStatementRequest
    {
        public SearchCriteria SearchCriteria { get; set; }
        public EResponseType ResponseType { get; set; }
    }
    public class FinancialSettingsDb
    {
        public string Environment { get; set; }
        public string AuthToken { get; set; }
        public string Secret { get; set; }
        public int AppID { get; set; }
        public string BaseUrl { get; set; }
        public string secretContent { get; set; }
    }

    public class SearchCriteria
    {
        public string Branches { get; set; }="";
        public long CustomerId { get; set; }
        public int? IsPosted { get; set; } = 2;
        public DateTime? FromDate { get; set; } = null;
        public DateTime? ToDate { get; set; } = null;
        public string Currency { get; set; } = "JOD";
        public string Account { get; set; } = null;
        public int? CompanyId { get; set; } = 1;
        public int? StatementType { get; set; } = 1;
        public string CoreToken { get; set; } = null;
        public string SessionId { get; set; } = null;
        public decimal? ExRate { get; set; } = null;
        public int? ShowInForeign { get; set; } = 1;
    }

    public class SecretContent
    {
        public string Secret { get; set; }
    }
    public class CustomerStatementResponse
    {
        public List<CustomerStatementData> Data { get; set; }
        public string ErrorMessage { get; set; }
        public string SuccessCollection { get; set; }
        public string ErrorInnerMessage { get; set; }
        public string ErrorCode { get; set; }
        public bool IsError { get; set; }
        public string TotalRecords { get; set; }
    }

    public class CustomerStatementData
    {
        public double CUSTOMER_ID { get; set; }
        public string CUSTOMER_SEGMENT_CODE { get; set; }
        public string CUSTOMER_NAME { get; set; }
        public string TRANSACTION_TYPE { get; set; }
        public DateTime JOURNAL_DATE { get; set; }
        public string JOURNAL_NO { get; set; }
        public double TRANSACTION_ID { get; set; }
        public string TRANSACTION_SOURCE { get; set; }
        public double DR_AMOUNT { get; set; }
        public double CR_AMOUNT { get; set; }
        public double DUE_AMOUNT { get; set; }
        public double DUE_AMOUNT_FC { get; set; }
        public double BALANCE { get; set; }
        public double CLOSE_BALANCE { get; set; }
        public double? DR_CR { get; set; }
        public string NOTES { get; set; }
        public double AMOUNT_LC { get; set; }
        public double AMOUNT { get; set; }
        public double OPEN_BALANCE { get; set; }
        public double IS_INVOICE_TRANSACTION { get; set; }
    }

}
