using System.Collections.Generic;

namespace VisFP.Models.TaskProblemViewModels
{
    public class ExamTaskInfoViewModel : TaskInfoViewModel
    {
        public IEnumerable<ExamProblem> OtherProblems { get; set; }

        public ExamTaskInfoViewModel(TaskInfoViewModel tvm, IEnumerable<ExamProblem> otherProblems)
        {
            AnswerType = tvm.AnswerType;
            GotRightAnswer = tvm.GotRightAnswer;
            Graph = tvm.Graph;
            IsControlProblem = true;
            LeftAttempts = tvm.LeftAttempts;
            ListInfo = tvm.ListInfo;
            OtherProblems = otherProblems;
            ProblemId = tvm.ProblemId;
            SymbolsForAnswer = tvm.SymbolsForAnswer;
            TaskQuestion = tvm.TaskQuestion;
            TaskTitle = tvm.TaskTitle;
            TopInfo = tvm.TopInfo;
        }
    }
}
