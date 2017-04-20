using System.Collections.Generic;

namespace VisFP.Models.TaskProblemViewModels
{
    public class ExamTaskInfoViewModel : TaskInfoViewModel
    {
        public IEnumerable<ExamProblem> OtherProblems { get; set; }

        public ExamTaskInfoViewModel(
            TaskBaseInfo taskBase, 
            ComponentRepository components, 
            IEnumerable<ExamProblem> otherProblems)
            :base(taskBase, components)
        {
            BaseInfo.IsControlProblem = true;
        }
    }
}
