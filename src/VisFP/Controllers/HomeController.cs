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

namespace VisFP.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        public HomeController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
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
                    default:
                        return View();
                }
            }
            return View();//Error!

        }

        [NonAction]
        public async Task<IActionResult> TaskSymbolsAnswer(RgTask task,
            Func<RegularGrammar, bool> conditionUntil,
            Func<RegularGrammar, string> getAnswer)
        {
            var alphabet = Alphabet.GenerateRandom(
                task.AlphabetNonTerminalsCount,
                task.AlphabetTerminalsCount);
            RegularGrammar rg;
            do
            {
                rg = RGGenerator.Instance.Generate(
                    ntRuleCount: task.NonTerminalRuleCount,
                    tRuleCount: task.TerminalRuleCount,
                    alph: alphabet);
            } while (conditionUntil(rg));

            var cTask = new RgTaskProblem
            {
                RightAnswer = string.Join(" ", getAnswer(rg)),
                TaskNumber = task.TaskNumber,
                ProblemGrammar = rg.Serialize(),
                User = _userManager.Users.First()
            };
            _dbContext.TaskProblems.Add(cTask);
            await _dbContext.SaveChangesAsync();

            var vm = new TaskViewModel(rg);
            vm.TaskText = task.TaskText;
            vm.TaskTitle = task.TaskTitle;
            vm.Id = cTask.ProblemId;
            return View("TaskView", vm);
        }

        [HttpPost]
        public async Task<JsonResult> Answer(AnswerViewModel avm)
        {
            var problem = _dbContext.TaskProblems.FirstOrDefault(x => x.ProblemId == avm.TaskId);//может не быть таска!
                                                                                                 //if (current.User != problem.User) return Error!
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
            if (isCorrect)
                return new JsonResult(true);
            else
                return new JsonResult(false);
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
