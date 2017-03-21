using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisFP.Models;
using VisFP.Models.RGViewModels;
using VisFP.Data;
using Microsoft.AspNetCore.Identity;
using VisFP.Models.DBModels;
using Microsoft.Extensions.Logging;

namespace VisFP.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger _logger;

        public HomeController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = loggerFactory.CreateLogger<HomeController>();
        }

        public async Task<IActionResult> Index(int task)
        {
            var reqTask = _dbContext.Tasks.FirstOrDefault(x => x.TaskNumber == task);
            if (reqTask != null)
            {
                switch (task)
                {
                    case 1:
                        return await TaskSymbolsAnswer(reqTask,
                            x => x.ReachableNonterminals.Length == x.Alph.NonTerminals.Count,
                            y => string.Join(" ", y.Alph.NonTerminals.Except(y.ReachableNonterminals).OrderBy(z => z))
                            );
                    case 2:
                        return await TaskSymbolsAnswer(reqTask,
                            x => x.GeneratingNonterminals.Length == x.Alph.NonTerminals.Count,
                            y => string.Join(" ", y.Alph.NonTerminals.Except(y.GeneratingNonterminals).OrderBy(z => z))
                            );
                    case 3:
                        return await TaskSymbolsAnswer(reqTask,
                            x => x.CyclicNonterminals.Length == 0,
                            y => string.Join(" ", y.CyclicNonterminals.OrderBy(z => z))
                            );
                    default:
                        return View();
                }
            }
            return View();//Error!

        }

        /// <summary>
        /// Генерация проблемы для задачи конкретного типа
        /// </summary>
        /// <param name="task">описание задачи</param>
        /// <param name="conditionUntil">условие генарации грамматики (грамматики генерируются пока не выполнено это условие)</param>
        /// <param name="getAnswer">метод получения ответа на задачу</param>
        /// <returns></returns>
        [NonAction]
        public async Task<IActionResult> TaskSymbolsAnswer(RgTask task,
            Func<RegularGrammar, bool> conditionUntil,
            Func<RegularGrammar, string> getAnswer)
        {
            var alphabet = Alphabet.GenerateRandom(
                task.AlphabetNonTerminalsCount,
                task.AlphabetTerminalsCount);
            RegularGrammar rg;
            int generation = 0;

            //генерируем грамматику пока не будет удовлетворять условию
            do
            {
                rg = RGGenerator.Instance.Generate(
                    ntRuleCount: task.NonTerminalRuleCount,
                    tRuleCount: task.TerminalRuleCount,
                    alph: alphabet);
                generation++;
            } while (conditionUntil(rg));

            //записываем проблему в базу
            var cTask = new RgTaskProblem
            {
                RightAnswer = string.Join(" ", getAnswer(rg)),
                TaskNumber = task.TaskNumber,
                ProblemGrammar = rg.Serialize(),
                User = _userManager.Users.First(),
                MaxAttempts = task.MaxAttempts,
                AnswerType = task.AnswerType
            };
            _dbContext.TaskProblems.Add(cTask);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"ProblemId: {cTask.ProblemId}, Generation: {generation}");

            //формируем модель для показа
            var vm = new TaskViewModel(rg);
            vm.TaskText = task.TaskText;
            vm.TaskTitle = task.TaskTitle;
            vm.Id = cTask.ProblemId;
            vm.MaxAttempts = cTask.MaxAttempts;
            vm.Generation = generation;
            return View("TaskView", vm);
        }

        [HttpPost]
        public async Task<JsonResult> Answer(AnswerViewModel avm)
        {
            var problem = _dbContext.TaskProblems.FirstOrDefault(x => x.ProblemId == avm.TaskId);//может не быть таска!
            if (problem != null)
            {
                //if (current.User != problem.User) return Error!
                var totalAttempts = _dbContext.Attempts.Count(x => x.ProblemId == problem.ProblemId);
                if (totalAttempts < problem.MaxAttempts)
                {
                    if (problem.AnswerType == TaskAnswerType.SymbolsAnswer)
                    {
                        avm.Answer = avm.Answer != null
                            ? string.Join(" ", avm.Answer.Split(' ').OrderBy(x => x))
                            : "";
                    }
                    var isCorrect = avm.Answer == problem.RightAnswer;
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
                            CurrentAttempt = totalAttempts + 2, //одна попытка сейчас добавилась, отсчет с 1
                            AttemptsLeft = problem.MaxAttempts - (totalAttempts + 1),
                            IsCorrect = isCorrect
                        });
                }
                else
                    return new JsonResult(new { block = true });
            }
            return new JsonResult("Задача не найдена") { StatusCode = 404 };
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
