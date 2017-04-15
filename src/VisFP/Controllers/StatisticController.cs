using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VisFP.Data.DBModels;
using Microsoft.AspNetCore.Identity;
using VisFP.Data;
using Microsoft.EntityFrameworkCore;
using VisFP.Models.StatisticViewModels;
using VisFP.Models.TaskProblemSharedViewModels;

namespace VisFP.Controllers
{
    public class StatisticController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public StatisticController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin, Teacher")]
        public async Task<IActionResult> UserStat(string id)
        {
            var currentUser = await GetCurrentUserAsync();
            var user = await _dbContext
                .Users
                .Include(x => x.UserGroup)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (user != null)
            {
                if (await _userManager.IsInRoleAsync(currentUser, "Teacher")) //Если препод - то может изменять и смотреть только своих учеников
                {
                    if (!await _dbContext.UserGroups.AnyAsync(x => x.Creator == currentUser && x.GroupId == user.UserGroupId))
                        return NotFound();
                }
                var variants = _dbContext
                    .Variants
                    .Include(x => x.Problems)
                    .Where(x => x.User == user).ToList();
                List<VariantStat> statVariant = new List<VariantStat>();
                foreach(var variant in variants)
                {
                    var problems = _dbContext.GetVariantProblems(variant);
                    var totalScore = problems.Sum(x => x.Score);
                    statVariant.Add(new VariantStat
                    {
                        Id = variant.VariantId,
                        TasksType = variant.VariantType,
                        DateStart = variant.CreateDate,
                        FailProblems = problems.Count(x => x.State == ProblemState.FailFinished),
                        SuccessProblems = problems.Count(x => x.State == ProblemState.SuccessFinished),
                        UnfinishedProblems = problems.Count(x => x.State == ProblemState.Unfinished),
                        TotalScore = totalScore
                    });
                }
                var model = new UserStatViewModel
                {
                    Login = user.UserName,
                    RealName = user.RealName,
                    Group = user.UserGroup.Name,
                    Variants = statVariant,
                    Id = user.Id
                };
                return View(model);
            }
            return NotFound();
        }

        [Authorize(Roles = "Admin, Teacher")]
        [HttpPost]
        public async Task<IActionResult> DeleteVariant(Guid varId)
        {
            try
            {
                var variant = _dbContext.Variants.Single(x => x.VariantId == varId);
                var userId = variant.UserId;
                _dbContext.Variants.Remove(variant);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(UserStat), new { id = userId });
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(HttpContext.User);
        }

        private async Task<DbRole> GetCurrentUserRole()
        {
            var role = await _userManager.GetRolesAsync(await GetCurrentUserAsync());
            return (DbRole)Enum.Parse(typeof(DbRole), role.First());
        }
    }
}