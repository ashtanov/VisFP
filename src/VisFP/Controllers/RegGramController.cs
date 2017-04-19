using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisFP.Data;
using Microsoft.AspNetCore.Identity;
using VisFP.Data.DBModels;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using VisFP.Models.TaskProblemViewModels;
using VisFP.BusinessObjects;

namespace VisFP.Controllers
{
    public class RegGramController : TaskProblemController
    {
        protected readonly ILogger _logger;
        private string _areaName;
        private DbTaskType _taskType;

        protected override string AreaName
        {
            get
            {
                return _areaName ?? (_areaName = "Регулярные грамматики");
            }
        }

        protected override DbTaskType ControllerTaskType
        {
            get
            {
                return _taskType ?? (_taskType = DbWorker.TaskTypes[Constants.RgType]);
            }
        }

        protected override ILogger Logger
        {
            get
            {
                return _logger;
            }
        }

        public RegGramController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
            : base(userManager, dbContext)
        {
            _logger = loggerFactory.CreateLogger(GetType());
        }

        protected override async Task<ExamVariantViewModel> AddTasksToVariant(ApplicationUser user, DbControlVariant variant)
        {
            var templateTasks = _dbContext
                                    .GetTasksForUser(user, true, ControllerTaskType.TaskTypeId)
                                    .Cast<RgTask>();

            List<RgTaskProblem> problems = new List<RgTaskProblem>();
            foreach (var template in templateTasks) //Генерим задачи
            {
                RGProblemResult problem = await GetProblem(user, template, variant);
                problems.Add(problem.Problem);
            }
            await _dbContext.SaveChangesAsync();
            ExamVariantViewModel model = new ExamVariantViewModel
            {
                CreateDate = variant.CreateDate,
                Problems = new List<ExamProblem>(
                    problems.Select(
                        x => new ExamProblem
                        {
                            ProblemId = x.ProblemId,
                            State = ProblemState.Unfinished,
                            TaskNumber = x.TaskNumber,
                            TaskTitle = x.TaskTitle
                        })).OrderBy(x => x.TaskNumber)
            };
            return model;
        }

        protected virtual async Task<RGProblemResult> GetProblem(ApplicationUser user, RgTask template, DbControlVariant variant = null)
        {
            return await (new RgProblemBuilder2(_dbContext)).GenerateProblemAsync(template, user, variant);
        }

        protected virtual TaskInfoViewModel GetTaskInfo(DbTaskProblem problem, RegularGrammar grammar, bool isControl)
        {
            var topInfo = new TaskInfoTopModule
            {
                Header = "Алфавит",
                Fields =
                    new Dictionary<string, string>
                    {
                        { "Терминалы", string.Join(", ", grammar.Alph.Terminals) },
                        { "Нетерминалы", string.Join(", ", grammar.Alph.NonTerminals) },
                        { "Начальное состояние", grammar.Alph.InitState.ToString() }
                    }
            };
            var listInfo = new TaskInfoListModule
            {
                Header = "Правила",
                Items = grammar.Rules.Select(x => x.ToString()),
                IsOrdered = true
            };
            
            var attempts = problem.MaxAttempts - (problem.Attempts?.Count ?? 0);
            var gotRightAnswer = problem.Attempts?.Any(x => x.IsCorrect) ?? false;
            var graphModule = new GraphModule
            {
                Graph = (new GrammarGraph(grammar))
            };
            return new TaskInfoViewModel
            {
                TopInfo = topInfo,
                ListInfo = listInfo,
                SymbolsForAnswer = grammar.Alph.NonTerminals,
                TaskQuestion = problem.TaskQuestion,
                TaskTitle = problem.TaskTitle,
                AnswerType = problem.AnswerType,
                ProblemId = problem.ProblemId,
                IsControlProblem = isControl,
                LeftAttempts = attempts,
                GotRightAnswer = gotRightAnswer,
                Graph = graphModule
            };
        }

        public override async Task<IActionResult> Task(int id, Guid? problemId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (!problemId.HasValue) //тренировочная задача
                {
                    RgTask templateTask = _dbContext.GetTasksForUser(user, false, ControllerTaskType.TaskTypeId) //выбираем шаблон таска базовый
                            .Cast<RgTask>()
                            .FirstOrDefault(x => x.TaskNumber == id);
                    RGProblemResult problem = await GetProblem(user, templateTask);
                    await _dbContext.SaveChangesAsync();
                    var viewModel = GetTaskInfo(problem.Problem, problem.Grammar, false);
                    return View("TaskShared/TaskView", viewModel);
                }
                else
                {
                    var currentProblem =
                        await _dbContext
                        .RgTaskProblems
                        .Include(x => x.CurrentGrammar)
                        .Include(x => x.Task)
                        .Include(x => x.Attempts)
                        .FirstOrDefaultAsync(x => x.ProblemId == problemId.Value);
                    if (currentProblem != null)
                    {
                        var grammar = RegularGrammar.Parse(currentProblem.CurrentGrammar.GrammarJson);

                        if (currentProblem.VariantId == null) //задача без варианта
                        {
                            var viewModel = GetTaskInfo(currentProblem, grammar, false);
                            return View("TaskShared/TaskView", viewModel);
                        }
                        else
                        {
                            var taskInfo = GetTaskInfo(currentProblem, grammar, true);
                            var currentVariant =
                               await _dbContext
                               .Variants
                               .FirstOrDefaultAsync(x => x.VariantId == currentProblem.VariantId);
                            var viewModel = new ExamTaskInfoViewModel(taskInfo, _dbContext.GetVariantProblems(currentVariant));
                            return View("TaskShared/ExamTaskView", viewModel);
                        }
                    }
                    else
                        return Error();
                }
            }
            catch (Exception ex)
            {
                return Error();
            }
        }

        [HttpPost]
        public JsonResult SaveGraph(string graph)
        {
            try
            {
                var d = Newtonsoft.Json.JsonConvert.DeserializeObject(graph);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
