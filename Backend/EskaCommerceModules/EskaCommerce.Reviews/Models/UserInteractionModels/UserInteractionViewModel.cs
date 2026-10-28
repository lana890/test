using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Reviews.Models.UserInteractionModels
{
    public class UserInteractionViewModel
    {
        public long? Id { get; set; }
        public int MaxNumberOfReviews { get; set; }
        public int MaxNumberOfComments { get; set; }
        public int MaxNumberOfReplays { get; set; }
        public bool AllowCommnetIfPayOrder { get; set; }
        public ERoleTypes ERoleTypes { get; set; }
    }
}
