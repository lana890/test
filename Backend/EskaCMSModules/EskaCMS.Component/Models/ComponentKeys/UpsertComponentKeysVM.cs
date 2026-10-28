using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.ViewModels.ComponentKeys
{
    public class UpsertComponentKeysVM
    {
        public long Id { get; set; }
        public long? PageId { get; set; }
        public long? PageTemplateId { get; set; }
        public bool IsConFirmed { get; set; }

        public string Key { get; set; }

    }
}
