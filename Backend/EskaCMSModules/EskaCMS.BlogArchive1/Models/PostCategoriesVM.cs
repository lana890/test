using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.BlogArchive.Models
{
   public class PostCategoriesVM
    {

        public long Id { get; set; }
        public long PostId { get; set; }
        public long CategoryId { get; set; }

        public string  CategoryName { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public long? ThumbnailImageId { get; set; }

    }
}
