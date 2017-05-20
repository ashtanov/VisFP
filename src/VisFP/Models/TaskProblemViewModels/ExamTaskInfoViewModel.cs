using System.Collections.Generic;

namespace VisFP.Models.TaskProblemViewModels
{
    public class ExamTaskInfoViewModel : TaskInfoViewModel
    {
        public IEnumerable<ExamProblem> OtherProblems { get; set; }

        public ExamTaskInfoViewModel(
            TaskBaseInfo taskBase, 
            ComponentRepository components,
            string moduleName, 
            IEnumerable<ExamProblem> otherProblems)
            :base(taskBase, components, moduleName)
        {
            BaseInfo.IsControlProblem = true;
            OtherProblems = otherProblems;
        }
    }
}
