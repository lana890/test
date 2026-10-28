using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models.TaskModels
{
    public class TaskListDto
    {

        public long? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<TasksResponseDto> ToDoLists { get; set; }
    }
}
