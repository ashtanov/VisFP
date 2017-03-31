using System;
using System.ComponentModel.DataAnnotations;
using VisFP.Data.DBModels;

namespace VisFP.Models.RGViewModels
{
    public class RgProblemViewModel : RGViewModel
    {
        public Guid Id { get; set; }

        public string TaskTitle { get; set; }
        public TaskAnswerType AnswerType { get; set; }

        [Display(Name = "Задание")]
        public string TaskText { get; set; }

        [Display(Name = "Попытки")]
        public int MaxAttempts { get; set; }

        public int Generation { get; set; }

        public bool IsControlProblem { get; set; }

        public RgProblemViewModel(RegularGrammar grammar, RgTaskProblem problem)
            : base(grammar)
        {
            TaskText = problem.TaskQuestion;
            TaskTitle = problem.Task.TaskTitle;
            AnswerType = problem.AnswerType;
            Id = problem.ProblemId;
            MaxAttempts = problem.MaxAttempts;
            Generation = problem.Generation;
            IsControlProblem = false;
        }

        public RgProblemViewModel(
            RegularGrammar grammar, 
            RgTaskProblem problem, 
            int leftAttempts)
            : base(grammar)
        {
            TaskText = problem.TaskQuestion;
            TaskTitle = problem.Task.TaskTitle;
            AnswerType = problem.AnswerType;
            Id = problem.ProblemId;
            MaxAttempts = leftAttempts;
            Generation = problem.Generation;
        }
    }
}
