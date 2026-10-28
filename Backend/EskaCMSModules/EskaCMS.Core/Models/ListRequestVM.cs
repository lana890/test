using System.Collections.Generic;

namespace EskaCMS.Core.Models
{
    public class ListRequestVM<T>
    {
        public T SearchParams { get; set; }
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
    }
}
