using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models.RGViewModels
{
    public class AnswerViewModel
    {
        [Display(Name = "Ответ")]
        public string Answer { get; set; }
        public Guid TaskId { get; set; }
        public int MaxAttemptsCount { get; set; }
        public Alphabet Alph { get; set; }
        public TaskAnswerType AnswerType { get; set; }
    }
}
