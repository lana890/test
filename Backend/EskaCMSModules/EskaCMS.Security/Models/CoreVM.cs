using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models
{
    public class CoreVM
    {
    }
    public class GetAllGroupsResponseVM
    {
        public EskaCoreBaseResponseVM<List<CoreGroupVM>> Groups { get; set; }
        public string Token { get; set; }
    }
    
    public class CoreGroupVM
    {
        public int ID { get; set; }
        public int CSR_GRP_ID { get; set; }
        public string NAME { get; set; }
        public string NAME2 { get; set; }
    }
    public class CoreResponse<T>
    {

        public T content { get; set; }

    }
    public class EskaCoreBaseResponseVM<T> : CoreResponse<T>
    {
        public string code { get; set; }
        public string message { get; set; }
    }
    public class EskaCoreBaseResponseVM
    {
        public string code { get; set; }
        public string message { get; set; }

    }
    public class HRResponseVM<T>
    {
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsError { get; set; }

    }
}
