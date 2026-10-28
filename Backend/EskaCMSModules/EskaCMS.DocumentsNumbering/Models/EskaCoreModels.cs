namespace EskaCMS.DocumentsNumbering.Models
{
    public class HREmployee
    {
        public int? ID { get; set; }
        public int? MANAGER_ID { get; set; }
        public string NAME { get; set; }
        public string USERNAME { get; set; }

    }
    public class HRDepartment
    {
        public int? ID { get; set; }
        public string COMPANY_NAME { get; set; }
        public string BRANCH_NAME { get; set; }

    }
    public class CoreBranch
    {
        public int? ID { get; set; }
        public string CODE { get; set; }
        public string NAME { get; set; }

    }
    public class CoreResponse<T>
    {
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsError { get; set; }

    }
    public class EskasCoreResponse<T>
    {
        public T content { get; set; }

    }
    public class EskasCoreBaseResponseVM<T> : EskasCoreResponse<T>
    {
        public string code { get; set; }
        public string message { get; set; }
    }

}
