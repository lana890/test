using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Security
{
    public class CypherParam: EntityBaseWithTypedId<string>
    {
        public CypherParam()
        {
            Id = Guid.NewGuid().ToString("N");
            AuthId = Guid.NewGuid().ToString("N"); 
        }

        public string AuthId { get; set; }

        public string PublicId { get; set; }    // Client RSA Public Key
        public string MajorKey { get; set; }  // AES Secret Key

        public DateTimeOffset CreationDate { get; set; }


    }

}
