using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

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
            foreach (var task in _dbContext.Tasks.Where(x => x.GroupId == Guid.Empty))
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
            await _dbContext.Tasks.AddRangeAsync(newTasks);
            await _dbContext.SaveChangesAsync();
        }
    }
}
