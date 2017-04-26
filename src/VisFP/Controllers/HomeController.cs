using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using VisFP.Data.DBModels;
using VisFP.Models.HomeViewModels;
using VisFP.BusinessObjects;
using VisFP.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace VisFP.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _dbContext = context;
        }

        public async Task<IActionResult> Index()
        {
            // (t 1 , p 2 ), (t 1 , p 3 ), (t 1 , p 5 ), (t 2 , p 5 ), (t 3 , p 4 ), (t 4 , p 2 ), (t 4 , p 3 ) 
            var petryNet = new PetryNet(
                P: new[] { "p1", "p2", "p3", "p4", "p5" },
                T: new[] { "t1", "t2", "t3", "t4" },
                F: new[]
                {
                    new PetryFlowLink { from = "p1", to = "t1" },
                    new PetryFlowLink { from = "p2", to = "t2" },
                    new PetryFlowLink { from = "p3", to = "t2" },
                    new PetryFlowLink { from = "p3", to = "t3", w="2" },
                    new PetryFlowLink { from = "p4", to = "t4" },
                    new PetryFlowLink { from = "p5", to = "t2" },
                    new PetryFlowLink { from = "t1", to = "p2" },
                    new PetryFlowLink { from = "t1", to = "p3" },
                    new PetryFlowLink { from = "t1", to = "p5" },
                    new PetryFlowLink { from = "t2", to = "p5" },
                    new PetryFlowLink { from = "t3", to = "p4" },
                    new PetryFlowLink { from = "t4", to = "p2" },
                    new PetryFlowLink { from = "t4", to = "p3" }
                },
                markup: new[] { "0", "100", "0", "0", "1" });
            var o = petryNet.Serialize();
            PetryNetGraph png = new PetryNetGraph(PetryNet.Deserialize(o));
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { });
            }
            else
            {
                List<TaskTypeViewModel> ttmodels = new List<TaskTypeViewModel>();

                foreach (var t in _dbContext.TaskTypes.ToList())
                {
                    var tasks = _dbContext.GetTasksForUser(user, true, t.TaskTypeId);
                    if (tasks != null) {
                        ttmodels.Add(new TaskTypeViewModel
                        {
                            TaskTypeControllerName = t.TaskTypeName,
                            TaskTypeName = t.TaskTypeNameToView,
                            TasksCount = tasks.Count()
                         });
                    }
                }

                return View(
                    new IndexViewModel
                    {
                        IsAdmin = await _userManager.IsInRoleAsync(user, Enum.GetName(typeof(DbRole), DbRole.Admin)),
                        IsTeacher = await _userManager.IsInRoleAsync(user, Enum.GetName(typeof(DbRole), DbRole.Teacher)),
                        png = png,
                        TaskTypes = ttmodels
                    });
            }
        }
    }
}