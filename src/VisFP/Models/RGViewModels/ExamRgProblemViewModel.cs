using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Models.RGViewModels
{
    public class ExamRgProblemViewModel : RgProblemViewModel
    {
        public IEnumerable<ExamProblem> OtherProblems { get; set; }
        public ExamRgProblemViewModel(
            RegularGrammar grammar,
            RgTaskProblem problem,
            int leftAttempts,
            IEnumerable<ExamProblem> otherProblems
            )
            : base(grammar, problem, leftAttempts)
        {
            OtherProblems = otherProblems;
        }
    }
}
