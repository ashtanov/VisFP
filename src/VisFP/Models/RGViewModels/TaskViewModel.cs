using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models.RGViewModels
{
    public class TaskViewModel : RGViewModel
    {
        public Guid Id { get; set; }

        public string TaskTitle { get; set; }
        public TaskAnswerType AnswerType { get; set; }

        [Display(Name = "Задание")]
        public string TaskText { get; set; }

        [Display(Name = "Попытки")]
        public int MaxAttempts { get; set; }

        public int Generation { get; set; }

        public TaskViewModel(RegularGrammar grammar)
            : base(grammar)
        {

        }
    }
}
