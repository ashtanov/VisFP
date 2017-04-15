using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;
using VisFP.Models.TaskProblemSharedViewModels;

namespace VisFP.Data
{
    public static class DbWorker
    {
        private static Guid _baseGroupId;
        public static Guid BaseGroupId
        {
            get
            {
                if (_baseGroupId != default(Guid))
                    return _baseGroupId;
                else
                    throw new NotImplementedException();
            }
            set
            {
                _baseGroupId = value;
            }
        }

        public static async Task SetRgTasksToNewTeacherAsync(this ApplicationDbContext _dbContext, string teacherId)
        {
            List<RgTask> newTasks = new List<RgTask>();
            var ttlink = new DbTeacherTask
            {
                TeacherId = teacherId
            };
            _dbContext.TeacherTasks.Add(ttlink);
            foreach (var task in _dbContext.RgTasks.Where(x => x.TeacherTaskId == null))
            {
                newTasks.Add(new RgTask
                {
                    AlphabetTerminalsCount = task.AlphabetTerminalsCount,
                    AlphabetNonTerminalsCount = task.AlphabetNonTerminalsCount,
                    ChainMinLength = task.ChainMinLength,
                    IsGrammarGenerated = true,
                    MaxAttempts = task.MaxAttempts,
                    TaskNumber = task.TaskNumber,
                    TaskTitle = task.TaskTitle,
                    TaskType = task.TaskType,
                    TerminalRuleCount = task.TerminalRuleCount,
                    NonTerminalRuleCount = task.NonTerminalRuleCount,
                    FailTryScore = task.FailTryScore,
                    SuccessScore = task.SuccessScore,
                    TeacherTaskId = ttlink.Id,
                    IsControl = task.IsControl
                });
            }
            await _dbContext.RgTasks.AddRangeAsync(newTasks);
        }

        public static IEnumerable<DbTask> GetTasksForUser(this ApplicationDbContext _dbContext, ApplicationUser user, bool isControl)
        {
            string teacherId;
            if (user.UserGroupId == BaseGroupId) //значит админ или препод - отдаем свои задачи
                teacherId = user.Id;
            else
                teacherId = _dbContext.UserGroups
                    .Single(x => x.GroupId == user.UserGroupId).CreatorId;
            return _dbContext
                .TeacherTasks
                .Include(x => x.Tasks)
                .Single(x => x.TeacherId == teacherId)
                .Tasks
                .Where(x => x.IsControl == isControl);
        }

        public static IOrderedEnumerable<ExamProblem> GetVariantProblems(this ApplicationDbContext _dbContext, DbControlVariant variant)
        {
            var problems = _dbContext
                    .TaskProblems
                    .Include(x => x.Attempts)
                    .Include(x => x.Task)
                    .Where(x => x.Variant == variant).ToList();
            var handledProblems = new List<ExamProblem>(
                    problems.Select(
                        x =>
                        {
                            var currState = GetState(x);
                            int currScore = (currState != ProblemState.SuccessFinished)
                                ? 0 : (x.Task.SuccessScore - (x.Task.FailTryScore * (x.Attempts.Count - 1)));
                            return new ExamProblem
                            {
                                ProblemId = x.ProblemId,
                                State = currState,
                                TaskNumber = x.TaskNumber,
                                TaskTitle = x.TaskTitle,
                                Score = currScore
                            };
                        })).OrderBy(x => x.TaskNumber);
            return handledProblems;
        }

        private static ProblemState GetState(DbTaskProblem problem)
        {
            return problem.Attempts.Any(a => a.IsCorrect == true)
                                        ? ProblemState.SuccessFinished
                                        : problem.Attempts.Count == problem.MaxAttempts
                                            ? ProblemState.FailFinished
                                            : ProblemState.Unfinished;
        }
    }
}
