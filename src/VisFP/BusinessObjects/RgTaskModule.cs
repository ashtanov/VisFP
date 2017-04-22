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

        public string GetModuleName()
        {
            return Constants.RgType;

        }
        public string GetModuleNameToView()
        {
            return "Регулярные грамматики";
        }

        public async Task<ProblemResult> CreateNewProblemAsync(DbTask taskTemplate)
        {
            ProblemResult problemResult;
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var rgTask = await dbContext.RgTasks.SingleOrDefaultAsync(x => x.Id == taskTemplate.ExternalTaskId);
                var problem = _problemBuilder.GenerateProblem(rgTask, taskTemplate.TaskNumber);

                RGrammar cGrammar;
                if (rgTask.IsGrammarGenerated)
                {
                    cGrammar = new RGrammar
                    {
                        GrammarJson = problem.Grammar.Serialize()
                    };
                    await dbContext.RGrammars.AddAsync(cGrammar);
                }
                else
                    cGrammar = rgTask.FixedGrammar;

                //записываем проблему в базу модуля
                var cTask = new RgTaskProblem
                {
                    CurrentGrammar = cGrammar
                };
                await dbContext.RgTaskProblems.AddAsync(cTask);
                await dbContext.SaveChangesAsync();

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
                problemResult = new ProblemResult
                {
                    ExternalProblemId = cTask.Id,
                    ProblemComponents = repository,
                    Answer = problem.ProblemAnswer
                };
            }

            return problemResult;
        }

        public async Task<ComponentRepository> GetExistingProblemAsync(DbTaskProblem problem)
        {
            ComponentRepository repository;
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var rgProblem = await dbContext
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
                repository = new ComponentRepository(mainComponent);
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
            }
            return repository;
        }

        public async Task<List<TaskSettingsSet>> GetAllTasksSettingsAsync(List<Guid> externalTaskIds)
        {
            List<TaskSettingsSet> result = new List<TaskSettingsSet>();
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var rgTasks = await dbContext.RgTasks.Where(x => externalTaskIds.Contains(x.Id)).ToListAsync();
                foreach (var task in rgTasks)
                {
                    result.Add(new TaskSettingsSet
                    {
                        TaskSettings = ConvertToTaskSettings(task),
                        TaskId = task.Id
                    });
                }
            }
            return result;

        }

        public async Task<TaskSettingsSet> GetTaskSettingsAsync(Guid externalTaskId)
        {
            RgTask rgTask;
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                rgTask = await dbContext.RgTasks.SingleAsync(x => externalTaskId == x.Id);
            }
            return new TaskSettingsSet
            {
                TaskSettings = ConvertToTaskSettings(rgTask),
                TaskId = externalTaskId
            };
        }

        public async Task SaveTaskSettingsAsync(Guid externalTaskId, List<ITaskSetting> updatedSettings)
        {
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
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
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
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
                        TaskNumber = bTask.TaskNumber,
                        AnswerType = bTask.AnswerType
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
                    TaskTitle = x.TaskTitle,
                    AnswerType = x.AnswerType
                }).ToList();
        }

        public async Task<List<NewTaskResult>> GetSeedTasks()
        {
            await EnshureCreated();
            List<RgTask> seedTasks = new List<RgTask>();
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                seedTasks = await dbContext.RgTasks.Where(x => x.IsSeed).ToListAsync();
            }
            return seedTasks
                .Select(x =>
                new NewTaskResult
                {
                    ExternalTaskId = x.Id,
                    TaskNumber = x.TaskNumber,
                    TaskTitle = x.TaskTitle,
                    AnswerType = x.AnswerType
                }).ToList();
        }

        private List<ITaskSetting> ConvertToTaskSettings(RgTask task)
        {
            var result = new List<ITaskSetting>();
            result.Add(new TaskSetting<int> { Name = nameof(task.AlphabetNonTerminalsCount), Value = task.AlphabetNonTerminalsCount, NameForView = "Количество нетерминалов в алфавите" });
            result.Add(new TaskSetting<int> { Name = nameof(task.AlphabetTerminalsCount), Value = task.AlphabetTerminalsCount, NameForView = "Количество терминалов в алфавите" });
            result.Add(new TaskSetting<int> { Name = nameof(task.ChainMinLength), Value = task.ChainMinLength, NameForView = "Оптимальная длина цепочки" });
            result.Add(new TaskSetting<int> { Name = nameof(task.NonTerminalRuleCount), Value = task.NonTerminalRuleCount, NameForView = "Количество нетерминальных правил" });
            result.Add(new TaskSetting<int> { Name = nameof(task.TerminalRuleCount), Value = task.TerminalRuleCount, NameForView = "Количество терминальных правил" });
            return result;
        }

        private async Task EnshureCreated()
        {
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
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
                        IsSeed = true,
                        AnswerType = TaskAnswerType.SymbolsAnswer
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
                        IsSeed = true,
                        AnswerType = TaskAnswerType.SymbolsAnswer
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
                        IsSeed = true,
                        AnswerType = TaskAnswerType.SymbolsAnswer
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
                        IsSeed = true,
                        AnswerType = TaskAnswerType.YesNoAnswer
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
                        IsSeed = true,
                        AnswerType = TaskAnswerType.YesNoAnswer
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
                        IsSeed = true,
                        AnswerType = TaskAnswerType.TextMulty
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
                        IsSeed = true,
                        AnswerType = TaskAnswerType.YesNoAnswer
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
                        IsSeed = true,
                        AnswerType = TaskAnswerType.YesNoAnswer
                    });
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
