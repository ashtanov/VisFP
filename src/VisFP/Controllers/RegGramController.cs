using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisFP.Models;
using VisFP.Models.RGViewModels;
using VisFP.Data;
using Microsoft.AspNetCore.Identity;
using VisFP.Data.DBModels;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using VisFP.Utils;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace VisFP.Controllers
{
    public class RegGramController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger _logger;

        public RegGramController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = loggerFactory.CreateLogger<RegGramController>();
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            return View(_dbContext
                .Tasks
                .Where(x => x.GroupId == Guid.Empty)
                .Select(x => new Tuple<int, string>(x.TaskNumber, x.TaskTitle)));
        }

        public async Task<IActionResult> ExamVariant(Guid? groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (_dbContext.Variants.Any(x => x.User == user && !x.IsFinished))
            {
                var variant = await _dbContext
                    .Variants
                    .Where(x => x.User == user)
                    .OrderByDescending(x => x.CreateDate).FirstOrDefaultAsync();
                var problems = _dbContext
                    .TaskProblems
                    .Include(x => x.Task)
                    .Include(x => x.Attempts)
                    .Where(x => x.Variant == variant);
                ExamVariantViewModel model = new ExamVariantViewModel
                {
                    CreateDate = variant.CreateDate,
                    Problems = new List<ExamProblem>(
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
                            })).OrderBy(x => x.TaskNumber)
                };
                return View(model);
            }
            else
            {
                if (groupId.HasValue)
                    throw new NotImplementedException(); //преподы могут свои группы выбирать

                var templateTasks = _dbContext //выбираем шаблоны тасков переопределенные для группы
                                .Tasks
                                .Where(x => x.GroupId == user.UserGroupId);
                RgControlVariant variant = new RgControlVariant
                {
                    CreateDate = DateTime.Now,
                    IsFinished = false,
                    User = user
                };
                await _dbContext.Variants.AddAsync(variant);

                List<RgTaskProblem> problems = new List<RgTaskProblem>();
                foreach (var template in templateTasks) //Генерим задачи
                {
                    var problem = await (new RGProblemBuilder(_dbContext)).GenerateProblemAsync(template, user, variant);
                    problems.Add(problem.Problem);
                }
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
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> Task(int id, Guid problemId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (problemId.Equals(default(Guid)))
                {
                    var builder = new RGProblemBuilder(_dbContext);
                    RgTask templateTask = _dbContext //выбираем шаблон таска базовый
                            .Tasks
                            .FirstOrDefault(
                                x => x.TaskNumber == id &&
                                x.GroupId == Guid.Empty);
                    RGProblemResult problem = await builder.GenerateProblemAsync(templateTask, user);
                    var viewModel = new TaskViewModel(problem.Grammar, problem.Problem);
                    return View("TaskView", viewModel);
                }
                else
                {
                    var currentProblem =
                        await _dbContext
                        .TaskProblems
                        .Include(x => x.CurrentGrammar)
                        .Include(x => x.Task)
                        .Include(x => x.Attempts)
                        .FirstOrDefaultAsync(x => x.ProblemId == problemId);
                    if (currentProblem != null)
                    {
                        var viewModel =
                            new TaskViewModel(
                                RegularGrammar.Parse(currentProblem.CurrentGrammar.GrammarJson),
                                currentProblem,
                                currentProblem.MaxAttempts - currentProblem.Attempts.Count);
                        return View("TaskView", viewModel);
                    }
                    else
                        throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                return Error();
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> Answer(AnswerViewModel avm)
        {
            var user = await _userManager.GetUserAsync(User);
            var problem = _dbContext.TaskProblems.FirstOrDefault(x => x.ProblemId == avm.TaskId);
            if (problem != null || problem.User != user) //задачи нет или задача не этого юзера
            {
                var totalAttempts = _dbContext.Attempts.Count(x => x.ProblemId == problem.ProblemId);
                if (totalAttempts < problem.MaxAttempts)
                {
                    if (problem.AnswerType == TaskAnswerType.SymbolsAnswer)
                    {
                        avm.Answer = avm.Answer != null
                            ? string.Join(" ", avm.Answer.Split(' ').OrderBy(x => x))
                            : "";
                    }
                    avm.Answer = avm.Answer.Trim();
                    totalAttempts += 1; //добавили текущую попытку
                    bool isCorrect;
                    if (problem.AnswerType == TaskAnswerType.TextMulty)
                        isCorrect = problem.RightAnswer.DeserializeJsonListOfStrings().Contains(avm.Answer);
                    else
                        isCorrect = avm.Answer == problem.RightAnswer;
                    _dbContext.Attempts.Add(
                        new RgAttempt
                        {
                            Answer = avm.Answer,
                            Date = DateTime.Now,
                            IsCorrect = isCorrect,
                            Problem = problem
                        });
                    await _dbContext.SaveChangesAsync();
                    return new JsonResult(
                        new AnswerResultViewModel
                        {
                            AttemptsLeft = problem.MaxAttempts - totalAttempts,
                            IsCorrect = isCorrect
                        });
                }
                else
                    return new JsonResult(new { block = true }); //Превышено максимальное количество попыток
            }
            return new JsonResult("Задача не найдена или недоступна текущему пользователю") { StatusCode = 404 };
        }

        [Authorize]
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
