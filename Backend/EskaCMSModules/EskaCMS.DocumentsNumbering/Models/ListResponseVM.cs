using System.Collections.Generic;

namespace EskaCMS.DocumentsNumbering.Models
{
    public class ListResponseVM<T>
    {
        public List<T> Items { get; set; }
        public int TotalRecord { get; set; }
    }
}
