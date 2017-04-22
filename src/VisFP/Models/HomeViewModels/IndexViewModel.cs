using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.BusinessObjects;

namespace VisFP.Models.HomeViewModels
{
    ///RegGram" class="list-group-item">1. Регулярные граматики <span class="badge">8
    public class TaskTypeViewModel
    {
        public string TaskTypeControllerName { get; set; }
        public string TaskTypeName { get; set; }
        public int TasksCount { get; set; }
    }

    public class IndexViewModel
    {
        public bool IsAdmin { get; set; }
        public bool IsTeacher { get; set; }
        public IEnumerable<TaskTypeViewModel> TaskTypes { get; set; }
        public PetryNetGraph png { get; set; }

    }
}
