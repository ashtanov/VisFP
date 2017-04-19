using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models.TaskProblemViewModels
{
    public class TaskListViewModel
    {
        public string TaskControllerName { get; set; }
        public IEnumerable<Tuple<int,string>> TasksList { get; set; }
    }
}
