using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data;
using VisFP.Data.DBModels;
using VisFP.Models.TaskProblemViewModels;

namespace VisFP.BusinessObjects
{
    public class RgTaskModule : ITaskModule
    {
        private IServiceScopeFactory _scopeFactory { get; set; }
        private static RgProblemBuilder2 _problemBuilder = new RgProblemBuilder2();

        /// <summary>
        /// Создание модуля
        /// </summary>
        public RgTaskModule(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<ProblemResult> CreateNewProblemAsync(DbTask taskTemplate)
        {
            var rgTask = await _dbContext.RgTasks.SingleOrDefaultAsync(x => x.Id == taskTemplate.ExternalTaskId);
            var problem = _problemBuilder.GenerateProblem(rgTask, taskTemplate.TaskNumber);

            RGrammar cGrammar;
            if (rgTask.IsGrammarGenerated == false)
            {
                cGrammar = new RGrammar
                {
                    GrammarJson = problem.Grammar.Serialize()
                };
                await _dbContext.RGrammars.AddAsync(cGrammar);
            }
            else
                cGrammar = rgTask.FixedGrammar;

            //записываем проблему в базу модуля
            var cTask = new RgTaskProblem
            {
                CurrentGrammar = cGrammar
            };
            await _dbContext.RgTaskProblems.AddAsync(cTask);
            await _dbContext.SaveChangesAsync();

            var mainComponent = new MainInfoComponent
            {
                Generation = problem.Generation,
                TaskQuestion = problem.ProblemQuestion,
                TaskTitle = taskTemplate.TaskTitle,
                AnswerType = problem.AnswerType,
                SymbolsForAnswer = problem.Grammar.Alph.NonTerminals
            };
            ComponentRepository repository = new ComponentRepository(mainComponent);
            repository.AddComponent(new TaskInfoTopComponent
            {
                Header = "Алфавит",
                Fields =
                    new Dictionary<string, string>
                    {
                        { "Терминалы", string.Join(", ", problem.Grammar.Alph.Terminals) },
                        { "Нетерминалы", string.Join(", ",  problem.Grammar.Alph.NonTerminals) },
                        { "Начальное состояние",  problem.Grammar.Alph.InitState.ToString() }
                    }
            });
            repository.AddComponent(new TaskInfoListComponent
            {
                Header = "Правила",
                Items = problem.Grammar.Rules.Select(x => x.ToString()),
                IsOrdered = true
            });
            repository.AddComponent(new GraphComponent
            {
                Graph = (new GrammarGraph(problem.Grammar))
            });

            return new ProblemResult
            {
                ExternalProblemId = cTask.Id,
                ProblemComponents = repository,
                Answer = problem.ProblemAnswer
            };
        }

        public async Task<ComponentRepository> GetExistingProblemAsync(DbTaskProblem problem)
        {
            var rgProblem = await _dbContext
                .RgTaskProblems
                .Include(x => x.CurrentGrammar)
                .SingleOrDefaultAsync(x => x.Id == problem.ExternalProblemId);
            var cGrammar = RegularGrammar.Parse(rgProblem.CurrentGrammar.GrammarJson);
            var mainComponent = new MainInfoComponent
            {
                Generation = problem.Generation,
                TaskQuestion = problem.TaskQuestion,
                TaskTitle = problem.TaskTitle,
                AnswerType = problem.AnswerType,
                SymbolsForAnswer = cGrammar.Alph.NonTerminals
            };
            ComponentRepository repository = new ComponentRepository(mainComponent);
            repository.AddComponent(new TaskInfoTopComponent
            {
                Header = "Алфавит",
                Fields =
                    new Dictionary<string, string>
                    {
                        { "Терминалы", string.Join(", ", cGrammar.Alph.Terminals) },
                        { "Нетерминалы", string.Join(", ",  cGrammar.Alph.NonTerminals) },
                        { "Начальное состояние",  cGrammar.Alph.InitState.ToString() }
                    }
            });
            repository.AddComponent(new TaskInfoListComponent
            {
                Header = "Правила",
                Items = cGrammar.Rules.Select(x => x.ToString()),
                IsOrdered = true
            });
            repository.AddComponent(new GraphComponent
            {
                Graph = (new GrammarGraph(cGrammar))
            });
            return repository;
        }

        public async Task<List<List<ITaskSetting>>> GetAllTasksSettingsAsync(List<Guid> externalTaskIds)
        {
            List<List<ITaskSetting>> result = new List<List<ITaskSetting>>();
            using (var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var rgTasks = await dbContext.RgTasks.Where(x => externalTaskIds.Contains(x.Id)).ToListAsync();
                foreach (var task in rgTasks)
                {
                    result.Add(ConvertToTaskSettings(task));
                }
            }
            return result;

        }

        private List<ITaskSetting> ConvertToTaskSettings(RgTask task)
        {
            var result = new List<ITaskSetting>();
            result.Add(new TaskSetting<int> { Name = nameof(task.AlphabetNonTerminalsCount), Value = task.AlphabetNonTerminalsCount });
            result.Add(new TaskSetting<int> { Name = nameof(task.AlphabetTerminalsCount), Value = task.AlphabetTerminalsCount });
            result.Add(new TaskSetting<int> { Name = nameof(task.ChainMinLength), Value = task.ChainMinLength });
            result.Add(new TaskSetting<int> { Name = nameof(task.NonTerminalRuleCount), Value = task.NonTerminalRuleCount });
            result.Add(new TaskSetting<int> { Name = nameof(task.TerminalRuleCount), Value = task.TerminalRuleCount });
            return result;
        }

        public async Task<List<ITaskSetting>> GetTaskSettingsAsync(Guid externalTaskId)
        {
            RgTask rgTask;
            using (var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                rgTask = await dbContext.RgTasks.SingleAsync(x => externalTaskId == x.Id);
            }
            return ConvertToTaskSettings(rgTask);
        }

        public async Task SaveTaskSettingsAsync(Guid externalTaskId, List<ITaskSetting> updatedSettings)
        {
            using (var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var task = await dbContext.RgTasks.SingleAsync(x => x.Id == externalTaskId);
                task.AlphabetNonTerminalsCount = (updatedSettings.Single(x => x.Name == nameof(task.AlphabetNonTerminalsCount)) as TaskSetting<int>).Value;
                task.AlphabetTerminalsCount = (updatedSettings.Single(x => x.Name == nameof(task.AlphabetTerminalsCount)) as TaskSetting<int>).Value;
                task.ChainMinLength = (updatedSettings.Single(x => x.Name == nameof(task.ChainMinLength)) as TaskSetting<int>).Value;
                task.NonTerminalRuleCount = (updatedSettings.Single(x => x.Name == nameof(task.NonTerminalRuleCount)) as TaskSetting<int>).Value;
                task.TerminalRuleCount = (updatedSettings.Single(x => x.Name == nameof(task.TerminalRuleCount)) as TaskSetting<int>).Value;
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<List<NewTaskResult>> CreateNewTaskSetAsync()
        {
            List<RgTask> newTasks = new List<RgTask>();
            using (var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var baseTasks = await dbContext.RgTasks.Where(x => x.IsSeed).ToListAsync();
                foreach (var bTask in baseTasks)
                {
                    newTasks.Add(new RgTask
                    {
                        AlphabetNonTerminalsCount = bTask.AlphabetNonTerminalsCount,
                        AlphabetTerminalsCount = bTask.AlphabetTerminalsCount,
                        ChainMinLength = bTask.ChainMinLength,
                        IsGrammarGenerated = true,
                        IsSeed = false,
                        NonTerminalRuleCount = bTask.NonTerminalRuleCount,
                        TaskTitle = bTask.TaskTitle,
                        TerminalRuleCount = bTask.TerminalRuleCount,
                        TaskNumber = bTask.TaskNumber
                    });
                }
                await dbContext.RgTasks.AddRangeAsync(newTasks);
                await dbContext.SaveChangesAsync();
            }
            return newTasks
                .Select(x =>
                new NewTaskResult
                {
                    ExternalTaskId = x.Id,
                    TaskNumber = x.TaskNumber,
                    TaskTitle = x.TaskTitle
                }).ToList();
        }

        public async Task EnshureCreated()
        {
            using (var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                if (await dbContext.RgTasks.CountAsync() == 0)
                {
                    dbContext.RgTasks.Add(new RgTask
                    {
                        TaskTitle = "Недостижимые символы",
                        NonTerminalRuleCount = 7,
                        IsGrammarGenerated = true,
                        TerminalRuleCount = 3,
                        AlphabetNonTerminalsCount = 5,
                        AlphabetTerminalsCount = 3,
                        TaskNumber = 1,
                        IsSeed = true
                    });

                    dbContext.RgTasks.Add(new RgTask
                    {
                        TaskTitle = "Пустые символы",
                        IsGrammarGenerated = true,
                        NonTerminalRuleCount = 7,
                        TerminalRuleCount = 3,
                        AlphabetNonTerminalsCount = 5,
                        AlphabetTerminalsCount = 3,
                        TaskNumber = 2,
                        IsSeed = true
                    });

                    dbContext.RgTasks.Add(new RgTask
                    {
                        TaskTitle = "Циклические символы",
                        IsGrammarGenerated = true,
                        NonTerminalRuleCount = 7,
                        TerminalRuleCount = 3,
                        AlphabetNonTerminalsCount = 5,
                        AlphabetTerminalsCount = 3,
                        TaskNumber = 3,
                        IsSeed = true
                    });

                    dbContext.RgTasks.Add(new RgTask
                    {
                        TaskTitle = "Приведенные грамматики",
                        IsGrammarGenerated = true,
                        NonTerminalRuleCount = 7,
                        TerminalRuleCount = 2,
                        AlphabetNonTerminalsCount = 4,
                        AlphabetTerminalsCount = 2,
                        TaskNumber = 4,
                        IsSeed = true
                    });

                    dbContext.RgTasks.Add(new RgTask
                    {
                        TaskTitle = "Пустые языки",
                        IsGrammarGenerated = true,
                        NonTerminalRuleCount = 7,
                        TerminalRuleCount = 2,
                        AlphabetNonTerminalsCount = 3,
                        AlphabetTerminalsCount = 2,
                        TaskNumber = 5,
                        IsSeed = true
                    });

                    dbContext.RgTasks.Add(new RgTask
                    {
                        TaskTitle = "Построение цепочки",
                        IsGrammarGenerated = true,
                        NonTerminalRuleCount = 7,
                        TerminalRuleCount = 2,
                        AlphabetNonTerminalsCount = 3,
                        AlphabetTerminalsCount = 2,
                        TaskNumber = 6,
                        ChainMinLength = 5,
                        IsSeed = true
                    });

                    dbContext.RgTasks.Add(new RgTask
                    {
                        TaskTitle = "Выводима ли цепочка?",
                        IsGrammarGenerated = true,
                        NonTerminalRuleCount = 7,
                        TerminalRuleCount = 2,
                        AlphabetNonTerminalsCount = 3,
                        AlphabetTerminalsCount = 2,
                        TaskNumber = 7,
                        ChainMinLength = 6,
                        IsSeed = true
                    });

                    dbContext.RgTasks.Add(new RgTask
                    {
                        TaskTitle = "Выводима ли цепочка двумя и более способами?",
                        IsGrammarGenerated = true,
                        NonTerminalRuleCount = 7,
                        TerminalRuleCount = 2,
                        AlphabetNonTerminalsCount = 3,
                        AlphabetTerminalsCount = 2,
                        TaskNumber = 8,
                        ChainMinLength = 6,
                        IsSeed = true
                    });
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
