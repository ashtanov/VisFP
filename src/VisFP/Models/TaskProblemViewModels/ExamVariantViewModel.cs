using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Models.TaskProblemViewModels
{
    public enum ProblemState
    {
        SuccessFinished,
        FailFinished,
        Unfinished
    }

    public class ExamProblem
    {
        public ProblemState State { get; set; }
        public Guid ProblemId { get; set; }
        public string TaskTitle { get; set; }
        public int TaskNumber { get; set; }
        public int Score { get; set;}
    }

    public class ExamVariantViewModel
    {
        [Display(Name = "Дата начала")]
        public DateTime CreateDate { get; set; }
        public IEnumerable<ExamProblem> Problems { get; set; }
    }
}
