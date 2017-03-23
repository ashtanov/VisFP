using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using VisFP.Data.DBModels;

namespace VisFP.Models.RGViewModels
{
    public class AnswerViewModel
    {
        [Display(Name = "Ответ")]
        public string Answer { get; set; }
        public Guid TaskId { get; set; }
        public int MaxAttemptsCount { get; set; }
        public IEnumerable<char> SymbolsCheckBox { get; set; }
        public TaskAnswerType AnswerType { get; set; }
    }
}
