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
            switch (task)
            {
                case 1:
                    return await Task1();
                case 2:
                    return Task2();
                default:
                    return View();
            }

        }

        [NonAction]
        public async Task<IActionResult> Task1()
        {
            var alphabet = new Alphabet('S',
                            new[] { '0', '1', '2' },
                            new[] { 'S', 'U', 'V', 'W', 'X' });
            RegularGrammar rg;
            do
            {
                rg = RGGenerator.Instance.Generate(7, 3, alphabet);
            } while (rg.ReachableNonterminals.Length == alphabet.NonTerminals.Count);

            var cTask = new RgTask
            {
                RightAnswer = string.Join(" ", rg.Alph.NonTerminals.Except(rg.ReachableNonterminals).OrderBy(x => x)),
                TaskNumber = 1,
                TaskGrammar = rg.Serialize(),
                User = _userManager.Users.First()
            };
            _dbContext.Tasks.Add(cTask);
            await _dbContext.SaveChangesAsync();

            var vm = new TaskViewModel(rg);
            vm.TaskText = "Найдите недостижимый символ (нетерминал)";
            vm.TaskTitle = "Задача 1. Недостижимые символы";
            vm.Id = cTask.TaskId;
            return View("TaskView", vm);
        }

        [NonAction]
        public IActionResult Task2()
        {
            var rg = RGGenerator.Instance.Generate(6, 4,
                        new Alphabet('S',
                            new[] { '1', '0', '2' },
                            new[] { 'S', 'U', 'V', 'W', 'X' })
                        );
            var gnt = rg.GeneratingNonterminals;
            var vm = new TaskViewModel(rg);
            vm.TaskText = "Найдите пустой символ (нетерминал)";
            vm.TaskTitle = "Задача 2. Пустые символы";
            vm.Id = new Guid();
            return View("TaskView", vm);
        }

        [HttpPost]
        public async Task<JsonResult> Answer(AnswerViewModel avm)
        {
            var problem = _dbContext.Tasks.First(x => x.TaskId == avm.TaskId);//может не быть таска!
           //if (current.User != problem.User) return Error!
            var isCorrect = avm.Answer == problem.RightAnswer;
            _dbContext.Attempts.Add(
                new RgAttempt
                {
                    Answer = avm.Answer,
                    Date = DateTime.Now,
                    IsCorrect = isCorrect,
                    Task = problem
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
