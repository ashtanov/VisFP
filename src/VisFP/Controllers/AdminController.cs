using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using VisFP.Data.DBModels;
using VisFP.Models.AdminViewModels;

namespace VisFP.Controllers
{

    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            ILoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _logger = loggerFactory.CreateLogger<AdminController>();
        }

        public async Task<IActionResult> Index()
        {
            List<UserForView> users = new List<UserForView>();
            foreach (var u in _userManager.Users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var userRole = (DbRole)Enum.Parse(typeof(DbRole), roles.First());
                users.Add(new UserForView(u, userRole));
            }
            var model = new AdminMainPageViewModel
            {
                AllUsers = users
            };
            return View(model);
        }
    }
}