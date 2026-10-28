using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Pagination
{
    public class PaginationResult<T> where T:class
    {
        public List<T> Data { get; set; }

        public int TotalPage { get; set; }

        public int TotalCount { get; set; }


        public PaginationResult(List<T> data,int TotalPages,int Totalcount)
        {
            Data = data;
            TotalPage= TotalPages;
            TotalCount = Totalcount;
        }

    }
}
