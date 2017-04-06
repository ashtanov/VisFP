using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VisFP.Data.DBModels;
using VisFP.Data;
using Microsoft.AspNetCore.Http;
using System.IO;
using VisFP.Utils;
using Microsoft.EntityFrameworkCore;

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
            var groups = _dbContext
                .UserGroups
                .Where(x => x.Creator == user)
                .Include(x => x.Members);
            return View(groups);
        }

        [HttpGet]
        public async Task<IActionResult> EditGroup(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            var group = await _dbContext
                .UserGroups
                .Where(x => x.GroupId == id)
                .Include(x => x.Members)
                .FirstOrDefaultAsync();
            if (group.Creator == user || await _userManager.IsInRoleAsync(user, nameof(DbRole.Admin)))
            {
                return View(group);
            }
            else
                return StatusCode(403);
        }

        [HttpPost]
        public async Task<IActionResult> EditGroup(UserGroup group)
        {
            var user = await _userManager.GetUserAsync(User);
            var g = await _dbContext
                .UserGroups
                .Include(x => x.Members)
                .FirstOrDefaultAsync(x => x.GroupId == group.GroupId);
            if (group.Creator == user || await _userManager.IsInRoleAsync(user, nameof(DbRole.Admin)))
            {
                g.Description = group.Description;
                await _dbContext.SaveChangesAsync();
                return View(g);
            }
            else
                return StatusCode(401);
        }

        [HttpPost]
        public async Task<IActionResult> GroupAccess(bool enable, Guid groupId)
        {
            var group = await _dbContext.UserGroups.FirstOrDefaultAsync(x => x.GroupId == groupId);
            if(group != null)
            {
                group.IsOpen = enable;
                await _dbContext.SaveChangesAsync();
            }
            return RedirectToAction(nameof(EditGroup), new { id = groupId });
        }

        [HttpGet]
        public IActionResult CreateGroup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup(UserGroup group)
        {
            if (!await _dbContext.UserGroups.AnyAsync(x => x.Name == group.Name))
            {
                var worker = new DbWorker(_dbContext);
                var user = await _userManager.GetUserAsync(User);
                group.Creator = user;
                await _dbContext.UserGroups.AddAsync(group);
                await _dbContext.SaveChangesAsync();
                await worker.SetRgTasksToNewGroup(group.GroupId);
                return RedirectToAction(nameof(EditGroup), new { id = group.GroupId });
            }
            else
            {
                ModelState.AddModelError("NameExisted", "Группа с таким названием уже существует в системе! Выберите другое название.");
                return View();
            }

        }

        public async Task<IActionResult> GroupRgTaskList(Guid groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            var group = await _dbContext
                .UserGroups
                .FirstOrDefaultAsync(x => x.GroupId == groupId);
            if (group != null)
            {
                if (group.Creator == user || await _userManager.IsInRoleAsync(user, nameof(DbRole.Admin)))
                {
                    return View(_dbContext.RgTasks.Where(x => x.GroupId == groupId));
                }
                else
                    return StatusCode(403);
            }
            return StatusCode(404);
        }

        [HttpGet]
        public async Task<IActionResult> EditRgTask(Guid taskId)
        {
            var user = await _userManager.GetUserAsync(User);
            var task = await _dbContext
                .RgTasks
                .FirstOrDefaultAsync(x => x.TaskId == taskId);
            if (task != null)
            {
                return View(task);
            }
            return StatusCode(404);
        }

        [HttpPost]
        public async Task<IActionResult> EditRgTask(RgTask task)
        {
            var user = await _userManager.GetUserAsync(User);
            var oldTask = await _dbContext
                .RgTasks
                .FirstOrDefaultAsync(x => x.TaskId == task.TaskId);
            if (oldTask != null)
            {
                oldTask.AlphabetNonTerminalsCount = task.AlphabetNonTerminalsCount;
                oldTask.AlphabetTerminalsCount = task.AlphabetTerminalsCount;
                oldTask.ChainMinLength = task.ChainMinLength;
                oldTask.IsGrammarGenerated = task.IsGrammarGenerated;
                oldTask.MaxAttempts = task.MaxAttempts;
                oldTask.NonTerminalRuleCount = task.NonTerminalRuleCount;
                oldTask.TerminalRuleCount = task.TerminalRuleCount;
                await _dbContext.SaveChangesAsync();
                return View(task);
            }
            return StatusCode(404);
        }
    }
}