using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VisFP.Data.DBModels;
using VisFP.Data;

namespace VisFP.Controllers
{
    [Authorize(Roles = "Teacher, Admin")]
    public class TeacherController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        public TeacherController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var groups = _dbContext.UserGroups.Where(x => x.Creator == user);
            return View(groups);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            var group = _dbContext.UserGroups.FirstOrDefault(x => x.GroupId == id);
            if (group.Creator == user || await _userManager.IsInRoleAsync(user, nameof(DbRole.Admin)))
            {
                return View(group);
            }
            else
                return StatusCode(403);
        }

        [HttpGet]
        public IActionResult CreateGroup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup(UserGroup group)
        {
            var user = await _userManager.GetUserAsync(User);
            group.Creator = user;
            await _dbContext.UserGroups.AddAsync(group);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Edit), new { id = group.GroupId });
        }
    }
}