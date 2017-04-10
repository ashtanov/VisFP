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
            var model = new TaskListViewModel
            {
                TaskControllerName = this.GetType().Name.Replace("Controller", ""),
                TasksList = _dbContext
                .RgTasks
                .Where(x => x.GroupId == DbWorker.BaseGroupId && x.TaskType == ControllerType)
                .Select(x => new Tuple<int, string>(x.TaskNumber, x.TaskTitle))
            };
            return View("TaskShared/Index", model);
        }

        public override async Task<IActionResult> ExamVariant(Guid? groupId)
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
                    DbWorker worker = new DbWorker(_dbContext);
                    ExamVariantViewModel model = new ExamVariantViewModel
                    {
                        CreateDate = variant.CreateDate,
                        Problems = worker.GetVariantProblems(variant)
                    };
                    return View("TaskShared/ExamVariant", model);
                }
                else
                {
                    if (groupId.HasValue)
                        throw new NotImplementedException(); //преподы могут свои группы выбирать

                    var templateTasks = _dbContext //выбираем шаблоны тасков переопределенные для группы
                                    .RgTasks
                                    .Where(x => x.GroupId == user.UserGroupId && x.TaskType == ControllerType);
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
                                    TaskNumber = x.Task.TaskNumber,
                                    TaskTitle = x.Task.TaskTitle
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
                    RgTask templateTask = _dbContext //выбираем шаблон таска базовый
                            .RgTasks
                            .FirstOrDefault(
                                x => x.TaskNumber == id &&
                                x.GroupId == DbWorker.BaseGroupId && x.TaskType == ControllerType);
                    RGProblemResult problem = await GetProblem(user, templateTask);
                    await _dbContext.SaveChangesAsync();
                    var viewModel = new RgProblemViewModel(problem.Grammar, problem.Problem);
                    return View("TaskView", viewModel);
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
                            return View("TaskView", viewModel);
                        }
                        else
                        {
                            var currentVariant =
                               await _dbContext
                               .Variants
                               .Include(x => x.Problems)
                               .FirstOrDefaultAsync(x => x.VariantId == currentProblem.VariantId);
                            DbWorker worker = new DbWorker(_dbContext);
                            var viewModel =
                                new ExamRgProblemViewModel(
                                    RegularGrammar.Parse(currentProblem.CurrentGrammar.GrammarJson),
                                    currentProblem,
                                    currentProblem.MaxAttempts - currentProblem.Attempts.Count,
                                    worker.GetVariantProblems(currentVariant));
                            return View("ExamTaskView", viewModel);
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
