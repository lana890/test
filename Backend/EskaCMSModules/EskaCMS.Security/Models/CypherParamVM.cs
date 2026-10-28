using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Security.Models
{
    public class CypherParamVM
    {
        public string MajorKey { get; set; }
        public string PointerId { get; set; }

        public string AuthId { get; set; }

        public string PublicId { get; set; }

        public DateTimeOffset CreationDate { get; set; }

    }


    public class CypherVM
    {

        public CypherParamVM CypherParam { get; set; }

        public string PointerId { get; set; }


    }



}
