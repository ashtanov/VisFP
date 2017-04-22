using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VisFP.Data.DBModels;
using VisFP.Data;
using Microsoft.EntityFrameworkCore;
using VisFP.Models.TeacherViewModels;

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
            var modules = _dbContext.TaskTypes.Include(x => x.Tasks);
            var mfv = modules.Select(x => new TaskModuleType
            {
                TypeName = x.TaskTypeName,
                TypeNameForView = x.TaskTypeNameToView,
                ControlAvailable = x.Tasks.Any(y => y.IsControl && y.TeacherTaskId != null),
                TestAvailable = x.Tasks.Any(y => y.IsControl && y.TeacherTaskId != null)
            });
            var viewModel = new TeacherIndexViewModel
            {
                Groups = groups,
                Modules = mfv
            };
            return View(viewModel);
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
            if (group != null)
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
                var user = await _userManager.GetUserAsync(User);
                group.Creator = user;
                await _dbContext.UserGroups.AddAsync(group);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(EditGroup), new { id = group.GroupId });
            }
            else
            {
                ModelState.AddModelError("NameExisted", "Группа с таким названием уже существует в системе! Выберите другое название.");
                return View();
            }

        }

        public async Task<IActionResult> TeacherTaskList(string typeName, bool isControl)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var ttlink = await _dbContext
                    .TeacherTasks
                    .SingleAsync(x => x.Teacher == user);

                var module = ModulesRepository.GetTaskModuleByName(typeName);
                var moduleId = ModulesRepository.GetModuleId(module.GetType());

                var tasks = _dbContext
                    .Tasks
                    .Where(x => x.IsControl == isControl
                    && x.TaskTypeId == moduleId
                    && x.TeacherTaskId == ttlink.Id);
                ViewData["Type"] = module.GetModuleNameToView();

                var mTasks = await module.GetAllTasksSettingsAsync(await tasks.Select(x => x.ExternalTaskId).ToListAsync());
                var allSettings = mTasks.Join(tasks,
                            x => x.TaskId,
                            y => y.ExternalTaskId,
                            (mt, dbt) => new CombinedTaskViewModel
                            {
                                InternalSettings = dbt,
                                ExternalSettings = mt.TaskSettings
                            }).OrderBy(x => x.InternalSettings.TaskNumber);

                var viewModel = new ModuleTaskSettingsViewModel { Tasks = allSettings };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(404);
            }
        }

        //[HttpGet]
        //public async Task<IActionResult> EditRgTask(Guid taskId)
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    var task = await _dbContext
        //        .RgTasks
        //        .FirstOrDefaultAsync(x => x.TaskId == taskId);
        //    if (task != null)
        //    {
        //        return View(task);
        //    }
        //    return StatusCode(404);
        //}

        //[HttpPost]
        //public async Task<IActionResult> EditRgTask(RgTask task)
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    var oldTask = await _dbContext
        //        .RgTasks
        //        .Include(x => x.TaskType)
        //        .FirstOrDefaultAsync(x => x.TaskId == task.TaskId);
        //    if (oldTask != null)
        //    {
        //        oldTask.AlphabetNonTerminalsCount = task.AlphabetNonTerminalsCount;
        //        oldTask.AlphabetTerminalsCount = task.AlphabetTerminalsCount;
        //        oldTask.ChainMinLength = task.ChainMinLength;
        //        oldTask.MaxAttempts = task.MaxAttempts;
        //        oldTask.NonTerminalRuleCount = task.NonTerminalRuleCount;
        //        oldTask.TerminalRuleCount = task.TerminalRuleCount;
        //        oldTask.FailTryScore = task.FailTryScore;
        //        oldTask.SuccessScore = task.SuccessScore;
        //        await _dbContext.SaveChangesAsync();
        //        return RedirectToAction(nameof(TeacherRgTaskList), new { isControl = oldTask.IsControl, typeName = oldTask.TaskType.TaskTypeName });
        //    }
        //    return StatusCode(404);
        //}
    }
}