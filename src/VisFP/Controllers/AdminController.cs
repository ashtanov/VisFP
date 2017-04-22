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
using VisFP.Data;

namespace VisFP.Controllers
{

    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = loggerFactory.CreateLogger<AdminController>();
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                List<UserForView> users = new List<UserForView>();
                var allUsers = _userManager.Users.ToList();
                foreach (var u in allUsers)
                {
                    var roles = await _userManager.GetRolesAsync(u);
                    var userRole = (DbRole)Enum.Parse(typeof(DbRole), roles.First());
                    users.Add(new UserForView(u, userRole));
                }
                var model = new AdminIndexViewModel
                {
                    AllUsers = users,
                };
                return View(model);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }
    }
}