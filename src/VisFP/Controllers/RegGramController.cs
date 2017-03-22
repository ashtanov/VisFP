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
using Microsoft.AspNetCore.Authorization;

namespace VisFP.Controllers
{
    public class RegGramController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly Random _rand;

        public RegGramController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = loggerFactory.CreateLogger<RegGramController>();
            _rand = new Random();
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            return View(_dbContext.Tasks.Select(x => x.TaskNumber));
        }

        [Authorize]
        public async Task<IActionResult> Task(int id, Guid taskId)
        {
            var reqTask = _dbContext.Tasks.FirstOrDefault(x => x.TaskNumber == id);
            if (reqTask != null)
            {
                switch (id)
                {
                    case 1:
                        return await GenerateProblem(reqTask,
                            x => x.ReachableNonterminals.Length == x.Alph.NonTerminals.Count,
                            y => string.Join(" ", y.Alph.NonTerminals.Except(y.ReachableNonterminals).OrderBy(z => z))
                            );
                    case 2:
                        return await GenerateProblem(reqTask,
                            x => x.GeneratingNonterminals.Length == x.Alph.NonTerminals.Count,
                            y => string.Join(" ", y.Alph.NonTerminals.Except(y.GeneratingNonterminals).OrderBy(z => z))
                            );
                    case 3:
                        return await GenerateProblem(reqTask,
                            x => x.CyclicNonterminals.Length == 0,
                            y => string.Join(" ", y.CyclicNonterminals.OrderBy(z => z))
                            );
                    case 4:
                        bool isProper = _rand.Next(2) == 1;
                        return await GenerateProblem(reqTask,
                            x => x.IsProper != isProper,
                            y => isProper ? "yes" : "no"
                            );
                    case 5:
                        bool isEmptyLang = _rand.Next(2) == 1;
                        return await GenerateProblem(reqTask,
                            x => x.IsEmptyLanguage != isEmptyLang,
                            y => isEmptyLang ? "yes" : "no"
                            );
                        
                    default:
                        return RedirectToAction("Index");
                }
            }
            return Error();//Error!

        }

        /// <summary>
        /// Генерация проблемы для задачи конкретного типа
        /// </summary>
        /// <param name="task">описание задачи</param>
        /// <param name="conditionUntil">условие генарации грамматики (грамматики генерируются пока не выполнено это условие)</param>
        /// <param name="getAnswer">метод получения ответа на задачу</param>
        /// <returns></returns>
        [NonAction]
        public async Task<IActionResult> GenerateProblem(RgTask task,
            Func<RegularGrammar, bool> conditionUntil,
            Func<RegularGrammar, string> getAnswer)
        {
            
            var alphabet = Alphabet.GenerateRandom(
                task.AlphabetNonTerminalsCount,
                task.AlphabetTerminalsCount);
            RegularGrammar rg;
            int generation = 0;

            //генерируем грамматику пока она удовлетворяет условию
            do
            {
                rg = RGGenerator.Instance.Generate(
                    ntRuleCount: task.NonTerminalRuleCount,
                    tRuleCount: task.TerminalRuleCount,
                    alph: alphabet);
                generation++;
            } while (conditionUntil(rg));

            var user = await _userManager.GetUserAsync(User);
            //записываем проблему в базу
            var cTask = new RgTaskProblem
            {
                RightAnswer = getAnswer(rg),
                TaskNumber = task.TaskNumber,
                ProblemGrammar = rg.Serialize(),
                User = user,
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
            vm.AnswerType = task.AnswerType;
            vm.Id = cTask.ProblemId;
            vm.MaxAttempts = cTask.MaxAttempts;
            vm.Generation = generation;
            return View("TaskView", vm);
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> Answer(AnswerViewModel avm)
        {
            var user = await _userManager.GetUserAsync(User);
            var problem = _dbContext.TaskProblems.FirstOrDefault(x => x.ProblemId == avm.TaskId);
            if (problem != null || user != problem.User) //задача не принадлежит текущему юзеру или задачи нет
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
