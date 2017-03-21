using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using VisFP.Models.DBModels;
using VisFP.Models.HomeViewModels;

namespace VisFP.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public HomeController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { });
            }
            else
            {
                return View(
                    new MainPageViewModel
                    {
                        IsAdmin = await _userManager.IsInRoleAsync(user, "Admin")
                    });
            }
        }
    }
}