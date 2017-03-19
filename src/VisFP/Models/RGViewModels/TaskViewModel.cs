using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models.RGViewModels
{
    public class TaskViewModel : RGViewModel
    {
        public string TaskTitle { get; set; }

        [Display(Name = "Задание")]
        public string TaskText { get; set; }

        public TaskViewModel(RegularGrammar grammar)
            : base(grammar)
        {

        }
    }
}
