using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;

namespace EskaCMS.Core.Models
{
    public class ApiLogVM
    {
        public string ApiUrl { get; set; }
        public string FullUrl { get; set; }
        public string HttpMethod { get; set; }
        public long StatusCode { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public long? SiteId { get; set; }
        public Site Site { get; set; }
        public User User { get; set; }
        public long? UserId { get; set; }
    }
}
