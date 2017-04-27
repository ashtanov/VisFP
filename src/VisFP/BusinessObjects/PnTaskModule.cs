using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data;
using VisFP.Data.DBModels;
using VisFP.Models.TaskProblemViewModels;
using VisFP.Utils;

namespace VisFP.BusinessObjects
{
    public class PnTaskModule : ITaskModule
    {
        IServiceScopeFactory _scopeFactory;
        /// <summary>
        /// Создание модуля
        /// </summary>
        public PnTaskModule(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public string GetModuleName()
        {
            return Constants.PetryNetType;
        }

        public string GetModuleNameToView()
        {
            return "Сети Петри";
        }

        public async Task<ProblemResult> CreateNewProblemAsync(DbTask taskTemplate)
        {
            ProblemResult problemResult;
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var pnTask = await dbContext.PnTasks.SingleOrDefaultAsync(x => x.Id == taskTemplate.ExternalTaskId);

                HashSet<int> rightAnswers = new HashSet<int>(pnTask.RightAnswers.Split(' ').Select(x => int.Parse(x)));
                var answers = pnTask
                    .Answers
                    .DeserializeJsonListOfStrings()
                    .Select((x, i) => new { ans = x, isR = rightAnswers.Contains(i + 1) }).ToList();
                var r = new Random();
                var permutate = answers.OrderBy(x => r.Next()).ToList();
                var newanswers = permutate.Select(x => x.ans).ToList();

                var problem = new PnTaskProblem
                {
                    PetryNetJson = pnTask.PetryNetJson,
                    Answers = newanswers.SerializeJsonListOfStrings()
                };
                //записываем проблему в базу модуля
                await dbContext.PnTaskProblems.AddAsync(problem);
                await dbContext.SaveChangesAsync();
                var mainComponent = new MainInfoComponent
                {
                    Generation = 0,
                    TaskQuestion = pnTask.Question,
                    TaskTitle = taskTemplate.TaskTitle,
                    AnswerType = pnTask.AnswerType,
                    SymbolsForAnswer = null,
                    AnswersList = newanswers
                };
                ComponentRepository repository = new ComponentRepository(mainComponent);
                var net = PetryNet.Deserialize(problem.PetryNetJson);
                repository.AddComponent(new GraphComponent
                {
                    Graph = new PetryNetGraph(net)
                });
                repository.AddComponent(GetTopComponent(net));
                repository.AddComponent(GetListComponent(net));
                var nra = permutate.Select((x, i) => new { x = x, cNum = i + 1 }).Where(x => x.x.isR).Select(x => x.cNum);
                problemResult = new ProblemResult
                {
                    ExternalProblemId = problem.Id,
                    ProblemComponents = repository,
                    Answer = string.Join(" ", nra)
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
                var pnProblem = await dbContext
                .PnTaskProblems
                .SingleOrDefaultAsync(x => x.Id == problem.ExternalProblemId);
                var mainComponent = new MainInfoComponent
                {
                    Generation = problem.Generation,
                    TaskQuestion = problem.TaskQuestion,
                    TaskTitle = problem.TaskTitle,
                    AnswerType = problem.AnswerType,
                    AnswersList = pnProblem.Answers.DeserializeJsonListOfStrings()
                };
                var currPN = PetryNet.Deserialize(pnProblem.PetryNetJson);
                repository = new ComponentRepository(mainComponent);
                repository.AddComponent(new GraphComponent { Graph = new PetryNetGraph(currPN) });
                repository.AddComponent(GetTopComponent(currPN));
                repository.AddComponent(GetListComponent(currPN));
            }
            return repository;
        }

        private static TaskInfoTopComponent GetTopComponent(PetryNet currPN)
        {
            var c = new TaskInfoTopComponent
            {
                Header = "Сеть Петри",
                Fields = new Dictionary<string, string>
                    {
                        { "P", $"({string.Join(", ", currPN.P)})" },
                        { "T", $"({string.Join(", ", currPN.T)})" }
                    }
            };
            if (currPN.Markup != null)
                c.Fields.Add("Маркировка", $"({string.Join(", ", currPN.Markup)})");
            return c;
        }
        private static TaskInfoListComponent GetListComponent(PetryNet currPN)
        {
            var c = new TaskInfoListComponent
            {
                Header = "F",
                Items = currPN.F.Select(x => $"({x.from},{x.to})"),
                IsOrdered = false
            };
            return c;
        }

        public async Task<List<NewTaskResult>> CreateNewTaskSetAsync()
        {
            List<PnTask> newTasks = new List<PnTask>();
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var baseTasks = await dbContext.PnTasks.Where(x => x.IsSeed).ToListAsync();
                foreach (var bTask in baseTasks)
                {
                    newTasks.Add(new PnTask
                    {
                        IsSeed = false,
                        TaskTitle = bTask.TaskTitle,
                        TaskNumber = bTask.TaskNumber,
                        AnswerType = bTask.AnswerType,
                        PetryNetJson = bTask.PetryNetJson,
                        Answers = bTask.Answers,
                        Question = bTask.Question,
                        RightAnswers = bTask.RightAnswers
                    });
                }
                await dbContext.PnTasks.AddRangeAsync(newTasks);
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

        public async Task<List<TaskSettingsSet>> GetAllTasksSettingsAsync(List<Guid> externalTaskIds)
        {
            List<TaskSettingsSet> result = new List<TaskSettingsSet>();
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var pnTasks = await dbContext.PnTasks.Where(x => externalTaskIds.Contains(x.Id)).ToListAsync();
                foreach (var task in pnTasks)
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

        public async Task<List<NewTaskResult>> GetSeedTasks()
        {
            await EnshureCreated();
            List<PnTask> seedTasks = new List<PnTask>();
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                seedTasks = await dbContext.PnTasks.Where(x => x.IsSeed).ToListAsync();
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

        private async Task EnshureCreated()
        {
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                if (!await dbContext.PnTasks.AnyAsync())
                {
                    var init = GetInitModuleTasks();
                    await dbContext.PnTasks.AddRangeAsync(init);
                    await dbContext.SaveChangesAsync();
                }
            }
        }
        //PetryNetJson = "{\"P\":[\"p1\",\"p2\",\"p3\",\"p4\",\"p5\"],\"T\":[\"t1\",\"t2\",\"t3\",\"t4\"],\"F\":[{\"from\":\"p1\",\"to\":\"t1\"},{\"from\":\"p2\",\"to\":\"t2\"},{\"from\":\"p3\",\"to\":\"t2\"},{\"from\":\"p3\",\"to\":\"t3\"},{\"from\":\"p4\",\"to\":\"t4\"},{\"from\":\"p5\",\"to\":\"t2\"},{\"from\":\"t1\",\"to\":\"p2\"},{\"from\":\"t1\",\"to\":\"p3\"},{\"from\":\"t1\",\"to\":\"p5\"},{\"from\":\"t2\",\"to\":\"p5\"},{\"from\":\"t3\",\"to\":\"p4\"},{\"from\":\"t4\",\"to\":\"p2\"},{\"from\":\"t4\",\"to\":\"p3\"}]}",

        protected List<PnTask> GetInitModuleTasks()
        {
            var result = new List<PnTask>();
            result.Add(new PnTask
            {
                Answers = new List<string> { "Задача о производителе/потребителе", "Задача о производителе/потребителе с ограниченным складом", "Задача о чтении/записи", "Задача об обедающих мудрецах" }.SerializeJsonListOfStrings(),
                AnswerType = TaskAnswerType.RadioAnswer,
                PetryNetJson = "{\"P\":[\"p0\",\"p1\",\"p2\",\"p3\",\"p4\",\"p5\"],\"T\":[\"t1\",\"t2\",\"t3\",\"t4\"],\"F\":[{\"from\":\"p1\",\"to\":\"t1\"},{\"from\":\"t1\",\"to\":\"p3\"},{\"from\":\"p3\",\"to\":\"t3\"},{\"from\":\"t3\",\"to\":\"p1\"},{\"from\":\"t3\",\"to\":\"p0\"},{\"from\":\"p0\",\"to\":\"t2\"},{\"from\":\"t2\",\"to\":\"p5\"},{\"from\":\"p5\",\"to\":\"t3\"},{\"from\":\"t2\",\"to\":\"p4\"},{\"from\":\"p4\",\"to\":\"t4\"},{\"from\":\"t4\",\"to\":\"p2\"},{\"from\":\"p2\",\"to\":\"t2\"}],\"markup\":[\"0\",\"1\",\"1\",\"0\",\"0\",\"N\"]}",
                Question = "Выберите задачу, которую описывает данная модель",
                RightAnswers = "2",
                TaskNumber = 1,
                TaskTitle = "Определить задачу",
                IsSeed = true
            });
            return result;
        }

        public async Task<TaskSettingsSet> GetTaskSettingsAsync(Guid externalTaskId)
        {
            PnTask pnTask;
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                pnTask = await dbContext.PnTasks.SingleAsync(x => externalTaskId == x.Id);
            }
            return new TaskSettingsSet
            {
                TaskSettings = ConvertToTaskSettings(pnTask),
                TaskId = externalTaskId
            };
        }

        private List<ITaskSetting> ConvertToTaskSettings(PnTask pnTask)
        {
            List<ITaskSetting> settings = new List<ITaskSetting>();
            settings.Add(new TaskSetting<string> { Name = nameof(pnTask.PetryNetJson), NameForView = "Сеть Петри", Value = pnTask.PetryNetJson });
            settings.Add(new TaskSetting<string> { Name = nameof(pnTask.Question), NameForView = "Вопрос", Value = pnTask.Question });
            settings.Add(new TaskSetting<List<string>> { Name = nameof(pnTask.Answers), NameForView = "Ответы (каждый на новой строке)", Value = pnTask.Answers.DeserializeJsonListOfStrings() });
            settings.Add(new TaskSetting<string> { Name = nameof(pnTask.RightAnswers), NameForView = "Номера правильных ответов (через пробел, с 1)", Value = pnTask.RightAnswers });
            return settings;
        }

        public bool IsAvailableAddTask()
        {
            return true;
        }

        public bool IsAvailableControlProblems()
        {
            return true;
        }

        public bool IsAvailableTestProblems()
        {
            return false;
        }

        public async Task SaveTaskSettingsAsync(Guid externalTaskId, ICollection<SettingValue> updatedSettings)
        {
            using (var scope = _scopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var task = await dbContext.PnTasks.SingleAsync(x => x.Id == externalTaskId);
                task.PetryNetJson = updatedSettings.Single(x => x.Name == nameof(task.PetryNetJson)).Value;
                task.Answers = updatedSettings
                    .Single(x => x.Name == nameof(task.Answers))
                    .Value
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList().SerializeJsonListOfStrings();
                task.RightAnswers = string.Join(" ", updatedSettings.Single(x => x.Name == nameof(task.RightAnswers)).Value.Split(' ').OrderBy(x => x));
                task.Question = updatedSettings.Single(x => x.Name == nameof(task.Question)).Value;
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
