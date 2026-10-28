using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Reviews.Models.ReviewModels
{
    public class UserBlockedViewModelRequest
    {
        public string Username { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }

    public class UsersBlockedViewModel
    {
        public string Username { get; set; }
        public long UserId { get; set; }
        public string Permissions { get; set; }

    }
}
