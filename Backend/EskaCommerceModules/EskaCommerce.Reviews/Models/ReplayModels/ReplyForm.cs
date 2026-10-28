using EskaCMS.Entities.Entity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Reviews.Models.ReplayModels
{
    public class ReplyForm
    {
        public string Comment { get; set; }
        public long ReviewId { get; set; }
        public List<IFormFile> files {  get; set; }
        public ReviewStatus? Status { get; set; }
    }
}
