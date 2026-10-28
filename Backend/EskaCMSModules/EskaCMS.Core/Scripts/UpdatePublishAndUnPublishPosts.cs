using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public static class UpdatePublishAndUnPublishPosts
    {

        public static string UpdatePostsExpiryDate = @"update blogarchive_posts set ExpiryDate = null where id >0 and status =2 and ExpiryDate <  NOW();";
        public static string UpdatePostsPublishDate = @"update blogarchive_posts set PublishDate = null where id >0 and status =1 and PublishDate > NOW();";
    }
}
