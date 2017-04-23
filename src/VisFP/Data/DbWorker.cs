using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.BusinessObjects;
using VisFP.Data.DBModels;
using VisFP.Models.TaskProblemViewModels;

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

        public static async Task SetTasksToNewTeacherAsync(this ApplicationDbContext _dbContext, 
            ITaskModule module, 
            Guid teacherTaskId,
            bool isControl)
        {
            var taskSet = await module.CreateNewTaskSetAsync();
            var currModuleId = ModulesRepository.GetModuleId(module.GetType());
            var dbSet = await _dbContext
                .Tasks
                .Where(x => x.TaskTypeId == currModuleId && x.TeacherTaskId == null)
                .ToListAsync();
            var joinedTasks = taskSet.Join(dbSet, x => x.TaskNumber, y => y.TaskNumber, (rg, db) => new { extId = rg.ExternalTaskId, db = db });
            foreach (var task in joinedTasks)
            {
                _dbContext.Add(new DbTask
                {
                    ExternalTaskId = task.extId,
                    MaxAttempts = task.db.MaxAttempts,
                    TaskNumber = task.db.TaskNumber,
                    TaskTitle = task.db.TaskTitle,
                    TaskTypeId = task.db.TaskTypeId,
                    FailTryScore = task.db.FailTryScore,
                    SuccessScore = task.db.SuccessScore,
                    TeacherTaskId = teacherTaskId,
                    IsControl = isControl
                });
            }
        }

        //public static async Task SetRgTasksToNewTeacherAsync(this ApplicationDbContext _dbContext, string teacherId)
        //{
        //    List<RgTask> newTasks = new List<RgTask>();
        //    var ttlink = new DbTeacherTask
        //    {
        //        TeacherId = teacherId
        //    };
        //    _dbContext.TeacherTasks.Add(ttlink);
        //    foreach (var task in _dbContext
        //        .RgTasks
        //        .Include(x => x.TaskType)
        //        .Where(x => x.TeacherTaskId == null))
        //    {
        //        newTasks.Add(new RgTask
        //        {
        //            AlphabetTerminalsCount = task.AlphabetTerminalsCount,
        //            AlphabetNonTerminalsCount = task.AlphabetNonTerminalsCount,
        //            ChainMinLength = task.ChainMinLength,
        //            IsGrammarGenerated = true,
        //            MaxAttempts = task.MaxAttempts,
        //            TaskNumber = task.TaskNumber,
        //            TaskTitle = task.TaskTitle,
        //            TaskType = task.TaskType,
        //            TerminalRuleCount = task.TerminalRuleCount,
        //            NonTerminalRuleCount = task.NonTerminalRuleCount,
        //            FailTryScore = task.FailTryScore,
        //            SuccessScore = task.SuccessScore,
        //            TeacherTaskId = ttlink.Id,
        //            IsControl = task.IsControl
        //        });
        //    }
        //    await _dbContext.RgTasks.AddRangeAsync(newTasks);
        //}

        public static IEnumerable<DbTask> GetTasksForUser(this ApplicationDbContext _dbContext, ApplicationUser user, bool isControl, Guid taskTypeId)
        {
            string teacherId;
            if (user.UserGroupId == BaseGroupId) //значит админ или препод - отдаем свои задачи
                teacherId = user.Id;
            else
                teacherId = _dbContext.UserGroups
                    .Single(x => x.GroupId == user.UserGroupId).CreatorId;
            var tasks = _dbContext
                .TeacherTasks
                .Include(x => x.Tasks)
                .Single(x => x.TeacherId == teacherId)
                .Tasks
                .Where(x => x.IsControl == isControl && x.TaskTypeId == taskTypeId);
            return tasks;
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
