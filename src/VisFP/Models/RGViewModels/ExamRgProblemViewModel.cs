using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.BusinessObjects;
using VisFP.Data.DBModels;
using VisFP.Models.TaskProblemSharedViewModels;

namespace VisFP.Models.RGViewModels
{
    public class ExamRgProblemViewModel : RgProblemViewModel
    {
        public IEnumerable<ExamProblem> OtherProblems { get; set; }
        public ExamRgProblemViewModel(
            RegularGrammar grammar,
            RgTaskProblem problem,
            int leftAttempts,
            bool gotRightAnswer,
            IEnumerable<ExamProblem> otherProblems)
            : base(grammar, problem, leftAttempts, gotRightAnswer)
        {
            OtherProblems = otherProblems;
            IsControlProblem = true;
        }
    }
}
