using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using VisFP.Data.DBModels;

namespace VisFP.Models.TaskProblemViewModels
{
    public class AnswerViewModel
    {
        [Display(Name = "Ответ")]
        public string Answer { get; set; }
        public Guid TaskProblemId { get; set; }
        public int LeftAttemptsCount { get; set; }
        public bool IsControl { get; set; }
        public bool GotRightAnswer { get; set; }
        public IEnumerable<char> SymbolsCheckBox { get; set; }
        public TaskAnswerType AnswerType { get; set; }
    }
}
