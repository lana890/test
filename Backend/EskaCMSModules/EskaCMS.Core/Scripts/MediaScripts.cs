
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public static class MediaScripts
    {
        public static string UpdateMediaStatus = @"update core_media set status = 2 where id > 0;";
        public static string UpdateFolderMediaFolder = @"update core_mediafolders set status = 2 where id > 0;";
       
        
    }
}
