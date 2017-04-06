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
        private ApplicationDbContext _dbContext;
        public DbWorker(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task SetRgTasksToNewGroup(Guid groupId)
        {
            List<RgTask> newTasks = new List<RgTask>();
            foreach (var task in _dbContext.RgTasks.Where(x => x.GroupId == Guid.Empty))
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
                    TerminalRuleCount = task.TerminalRuleCount,
                    NonTerminalRuleCount = task.NonTerminalRuleCount
                });
            }
            await _dbContext.RgTasks.AddRangeAsync(newTasks);
            await _dbContext.SaveChangesAsync();
        }

        public IOrderedEnumerable<ExamProblem> GetVariantProblems(RgControlVariant variant)
        {
            var problems = _dbContext
                    .RgTaskProblems
                    .Include(x => x.Task)
                    .Include(x => x.Attempts)
                    .Where(x => x.Variant == variant);
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
