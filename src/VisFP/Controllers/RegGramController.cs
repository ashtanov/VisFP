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
