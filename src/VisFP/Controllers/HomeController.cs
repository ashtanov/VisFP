using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisFP.Models;

namespace VisFP.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var rg = RGGenerator.Instance.Generate(6, 3, 
                new Alphabet('S', 
                    new[] { '1', '0', '2' }, 
                    new[] { 'S', 'U', 'V', 'W', 'X' })
                    );


            var g = new GrammarGraph(rg);
            //for (int i = 1; i <= 4; ++i)
            //    g.nodes.Add(new Models.Node { id = i, label = "Node " + i.ToString() });
            //Random r = new Random();
            //for (int i = 0; i < 2; ++i)
            //    g.edges.Add(new Models.Edge { from = r.Next(1, 5), to = r.Next(1, 5), label = "Edge" });
            return View(g);
        }

        [HttpPost]
        public JsonResult SaveGraph(string graph)
        {
            try
            {
                var d = Newtonsoft.Json.JsonConvert.DeserializeObject(graph);
            }
            catch(Exception ex)
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
