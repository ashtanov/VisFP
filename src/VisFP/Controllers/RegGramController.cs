using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisFP.Models.RGViewModels;
using VisFP.Data;
using Microsoft.AspNetCore.Identity;
using VisFP.Data.DBModels;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using VisFP.Models.TaskProblemSharedViewModels;
using VisFP.BusinessObjects;

namespace VisFP.Controllers
{
    public class RegGramController : TaskProblemController
    {
        protected readonly ILogger _logger;
        protected string ControllerType = Constants.RgType;
        protected string AreaName = "Регулярные грамматики";

        public RegGramController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
            : base(userManager, dbContext)
        {
            _logger = loggerFactory.CreateLogger(GetType());
        }

        public override async Task<IActionResult> Index()
        {
            ViewData["Title"] = AreaName;
            var user = await _userManager.GetUserAsync(User);
            var tasksList = _dbContext.GetTasksForUser(user, false)
                .Where(x => x is RgTask)
                .Cast<RgTask>()
                .Where(x => x.TaskType == ControllerType);
            var model = new TaskListViewModel
            {
                TaskControllerName = this.GetType().Name.Replace("Controller", ""),
                TasksList = tasksList.Select(x => new Tuple<int, string>(x.TaskNumber, x.TaskTitle))
            };
            return View("TaskShared/Index", model);
        }

        public override async Task<IActionResult> ExamVariant()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (_dbContext.Variants.Any(x => x.User == user && !x.IsFinished && x.VariantType == ControllerType)) //если нет текущего варианта
                {
                    var variant = await _dbContext
                        .Variants
                        .Where(x => x.User == user && x.VariantType == ControllerType)
                        .OrderByDescending(x => x.CreateDate).FirstOrDefaultAsync();
                    ExamVariantViewModel model = new ExamVariantViewModel
                    {
                        CreateDate = variant.CreateDate,
                        Problems = _dbContext.GetVariantProblems(variant)
                    };
                    return View("TaskShared/ExamVariant", model);
                }
                else
                {
                    var templateTasks = _dbContext
                        .GetTasksForUser(user, true)
                        .Where(x => x is RgTask)
                        .Cast<RgTask>()
                        .Where(x => x.TaskType == ControllerType);
                    DbControlVariant variant = new DbControlVariant
                    {
                        CreateDate = DateTime.Now,
                        IsFinished = false,
                        VariantType = ControllerType,
                        User = user
                    };
                    await _dbContext.Variants.AddAsync(variant);

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
                    return View("TaskShared/ExamVariant", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        protected virtual async Task<RGProblemResult> GetProblem(ApplicationUser user, RgTask template, DbControlVariant variant = null)
        {
            return await (new RgProblemBuilder2(_dbContext)).GenerateProblemAsync(template, user, variant);
        }

        public override async Task<IActionResult> Task(int id, Guid? problemId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (!problemId.HasValue) //тренировочная задача
                {
                    RgTask templateTask = _dbContext.GetTasksForUser(user,false) //выбираем шаблон таска базовый
                            .Where(x => x is RgTask)
                            .Cast<RgTask>()
                            .FirstOrDefault(x => x.TaskNumber == id && x.TaskType == ControllerType);
                    RGProblemResult problem = await GetProblem(user, templateTask);
                    await _dbContext.SaveChangesAsync();
                    var viewModel = new RgProblemViewModel(problem.Grammar, problem.Problem);
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
                        if (currentProblem.VariantId == null) //задача без варианта
                        {
                            var viewModel =
                                new RgProblemViewModel(
                                    RegularGrammar.Parse(currentProblem.CurrentGrammar.GrammarJson),
                                    currentProblem,
                                    currentProblem.MaxAttempts - currentProblem.Attempts.Count);
                            var gnt = viewModel.Grammar.GeneratingNonterminals.Value;
                            return View("TaskShared/TaskView", viewModel);
                        }
                        else
                        {
                            var currentVariant =
                               await _dbContext
                               .Variants
                               .Include(x => x.Problems)
                               .FirstOrDefaultAsync(x => x.VariantId == currentProblem.VariantId);
                            var viewModel =
                                new ExamRgProblemViewModel(
                                    RegularGrammar.Parse(currentProblem.CurrentGrammar.GrammarJson),
                                    currentProblem,
                                    currentProblem.MaxAttempts - currentProblem.Attempts.Count,
                                    _dbContext.GetVariantProblems(currentVariant));
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
