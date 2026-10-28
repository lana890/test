using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemForm
    {
        public ItemVm Item { get; set; }

        public IFormFile ThumbnailImage { get; set; }

        public IList<IFormFile> ItemImages { get; set; } = new List<IFormFile>();

        public IList<IFormFile> ItemDocuments { get; set; } = new List<IFormFile>();
    }
}
