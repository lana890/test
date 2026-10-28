using EskaCMS.BlogArchive.Models;
using EskaCMS.Pages.Models;
using EskaCMS.Sites.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models
{
   public class HandShakeVM
   {
        public string Key { get; set; }



    }

    public class DecriptVM
    {
        public string Cypher { get; set; }

    }

    public class PointerIdVM
    {
        public string PointerId { get; set; }

    }
}
