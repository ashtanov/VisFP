using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;
using VisFP.Models.RGViewModels;
using VisFP.Models.TaskProblemSharedViewModels;

namespace VisFP.Data
{
    public class DbWorker
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

        private ApplicationDbContext _dbContext;

        public DbWorker(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task SetRgTasksToNewGroup(Guid groupId)
        {
            List<RgTask> newTasks = new List<RgTask>();
            foreach (var task in _dbContext.RgTasks.Where(x => x.GroupId == BaseGroupId))
            {
                newTasks.Add(new RgTask
                {
                    AlphabetTerminalsCount = task.AlphabetTerminalsCount,
                    AlphabetNonTerminalsCount = task.AlphabetNonTerminalsCount,
                    GroupId = groupId,
                    ChainMinLength = task.ChainMinLength,
                    IsGrammarGenerated = true,
                    MaxAttempts = task.MaxAttempts,
                    TaskNumber = task.TaskNumber,
                    TaskTitle = task.TaskTitle,
                    TaskType = task.TaskType,
                    TerminalRuleCount = task.TerminalRuleCount,
                    NonTerminalRuleCount = task.NonTerminalRuleCount,
                    FailTryScore = task.FailTryScore,
                    SuccessScore = task.SuccessScore
                });
            }
            await _dbContext.RgTasks.AddRangeAsync(newTasks);
            await _dbContext.SaveChangesAsync();
        }

        public IOrderedEnumerable<ExamProblem> GetVariantProblems(DbControlVariant variant)
        {
            var problems = _dbContext
                    .RgTaskProblems
                    .Include(x => x.Task)
                    .Include(x => x.Attempts)
                    .Where(x => x.Variant == variant).ToList();
            var handledProblems = new List<ExamProblem>(
                    problems.Select(
                        x => new ExamProblem
                        {
                            ProblemId = x.ProblemId,
                            State =
                                x.Attempts.Any(a => a.IsCorrect == true)
                                        ? ProblemState.SuccessFinished
                                        : x.Attempts.Count == x.MaxAttempts
                                            ? ProblemState.FailFinished
                                            : ProblemState.Unfinished,
                            TaskNumber = x.Task.TaskNumber,
                            TaskTitle = x.Task.TaskTitle
                        })).OrderBy(x => x.TaskNumber);
            return handledProblems;
        }
    }
}
