using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisFP.Models;
using VisFP.Models.RGViewModels;

namespace VisFP.Controllers
{
    public class HomeController : Controller
    {
        //public IActionResult Index()
        //{
        //    var rg = RGGenerator.Instance.Generate(7, 3,
        //        new Alphabet('S',
        //            new[] { '1', '0', '2' },
        //            new[] { 'S', 'U', 'V', 'W', 'X' })
        //            );

        //    var rnt = rg.ReachableNonterminals;
        //    var vm = new RGViewModel(rg);
        //    return View(vm);
        //}

        public IActionResult Index(int task)
        {
            switch (task)
            {
                case 1:
                    return Task1();
                default:
                    return Error();
            }

        }

        public IActionResult Task1()
        {
            var rg = RGGenerator.Instance.Generate(7, 3,
                        new Alphabet('S',
                            new[] { '1', '0', '2' },
                            new[] { 'S', 'U', 'V', 'W', 'X' })
                        );
            var rnt = rg.ReachableNonterminals;
            var vm = new TaskViewModel(rg);
            vm.TaskText = "Найдите недостижимый символ";
            vm.TaskTitle = "Задача 1";
            return View("TaskView", vm);
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
