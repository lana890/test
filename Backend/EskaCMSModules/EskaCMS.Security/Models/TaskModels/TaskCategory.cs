using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models.TaskModels
{
    public class TaskCategoryDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
    
    public class CreateTaskCategoryVM
    {
        public string Name { get; set; }
      
    }
}
