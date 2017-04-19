using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using VisFP.BusinessObjects;
using VisFP.Data.DBModels;

namespace VisFP.Models.TaskProblemViewModels
{
    public class TaskInfoViewModel
    {
        public Guid ProblemId { get; set; }
        public string TaskTitle { get; set; }
        [Display(Name = "Задание")]
        public string TaskQuestion { get; set; }
        public IEnumerable<char> SymbolsForAnswer { get; set; }
        public TaskAnswerType AnswerType { get; set; }
        [Display(Name = "Попытки")]
        public int LeftAttempts { get; set; }
        public int Generation { get; set; }
        public bool IsControlProblem { get; set; }
        public bool GotRightAnswer { get; set; }

        #region Modules

        public TaskInfoTopModule TopInfo { get; set; }
        public TaskInfoListModule ListInfo { get; set; }
        public GraphModule Graph { get; set; }

        #endregion

        public AnswerViewModel GetAnswerModel()
        {
            return new AnswerViewModel
            {
                TaskProblemId = ProblemId,
                LeftAttemptsCount = LeftAttempts,
                SymbolsCheckBox = SymbolsForAnswer,
                AnswerType = AnswerType,
                IsControl = IsControlProblem,
                GotRightAnswer = GotRightAnswer
            };
        }
    }
    public class TaskInfoTopModule
    {
        public string Header { get; set; }
        public Dictionary<string, string> Fields { get; set; }
    }
    public class TaskInfoListModule
    {
        public string Header { get; set; }
        public bool IsOrdered { get; set; }
        public IEnumerable<string> Items { get; set; }
    }
    public class GraphModule
    {
        public IGraph Graph { get; set; }
    }

}
