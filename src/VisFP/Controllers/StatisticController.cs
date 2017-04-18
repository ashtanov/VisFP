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
                    .Include(x => x.TaskType)
                    .Where(x => x.User == user).ToList();
                List<VariantStat> statVariant = new List<VariantStat>();
                foreach (var variant in variants)
                {
                    var problems = _dbContext.GetVariantProblems(variant);
                    var totalScore = problems.Sum(x => x.Score);
                    statVariant.Add(new VariantStat
                    {
                        Id = variant.VariantId,
                        TasksType = variant.TaskType.TaskTypeName,
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
        public async Task<IActionResult> GroupStat(Guid id)
        {
            var currentUser = await GetCurrentUserAsync();
            var group = await _dbContext
                .UserGroups
                .Include(x => x.Members)
                .SingleOrDefaultAsync(x => x.GroupId == id);
            if (group != null)
            {
                if (await _userManager.IsInRoleAsync(currentUser, "Teacher"))
                {
                    if (!await _dbContext.UserGroups.AnyAsync(x => x.Creator == currentUser && x.GroupId == id))
                        return NotFound();
                }
                return View(GetGroupStat(group));
            }
            return NotFound();
        }

        private GroupStatViewModel GetGroupStat(UserGroup group)
        {
            List<UserStatViewModel> groupUsers = new List<UserStatViewModel>();
            foreach (var user in group.Members)
            {
                var variants = _dbContext
                    .Variants
                    .Include(x => x.TaskType)
                    .Include(x => x.Problems)
                    .Where(x => x.User == user).ToList();
                List<VariantStat> statVariant = new List<VariantStat>();
                foreach (var variant in variants)
                {
                    var problems = _dbContext.GetVariantProblems(variant);
                    var totalScore = problems.Sum(x => x.Score);
                    statVariant.Add(new VariantStat
                    {
                        Id = variant.VariantId,
                        TasksType = variant.TaskType.TaskTypeNameToView,
                        DateStart = variant.CreateDate,
                        FailProblems = problems.Count(x => x.State == ProblemState.FailFinished),
                        SuccessProblems = problems.Count(x => x.State == ProblemState.SuccessFinished),
                        UnfinishedProblems = problems.Count(x => x.State == ProblemState.Unfinished),
                        TotalScore = totalScore
                    });
                }
                groupUsers.Add(new UserStatViewModel
                {
                    Login = user.UserName,
                    RealName = user.RealName,
                    Group = user.UserGroup.Name,
                    Variants = statVariant,
                    Id = user.Id
                });
            }

            return new GroupStatViewModel
            {
                Users = groupUsers,
                Id = group.GroupId,
                Name = group.Name,
                TasksType = groupUsers.SelectMany(x => x.Variants.Select(y => y.TasksType)).Distinct()
            };
        }

        public async Task<FileResult> DownloadReport(Guid groupId, string types)
        {
            var currentUser = await GetCurrentUserAsync();
            var group = await _dbContext
                .UserGroups
                .Include(x => x.Members)
                .SingleOrDefaultAsync(x => x.GroupId == groupId);
            if (group != null)
            {
                if (await _userManager.IsInRoleAsync(currentUser, "Teacher"))
                {
                    if (!await _dbContext.UserGroups.AnyAsync(x => x.Creator == currentUser && x.GroupId == groupId))
                        throw new Exception();
                }
                var neededTypes = types.Split(new[] { "___" }, StringSplitOptions.RemoveEmptyEntries);
                var groupStat = GetGroupStat(group);
                var fileName = $"{group.Name}_{string.Join("_",neededTypes)}.csv";
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                System.IO.StreamWriter sw = new System.IO.StreamWriter(ms, System.Text.Encoding.UTF8);
                sw.WriteLine($"ФИО;{string.Join(";", neededTypes)}");
                foreach (var user in groupStat.Users)
                {
                    sw.Write($"{user.RealName}");
                    foreach (var type in neededTypes)
                    {
                        var curr = user.Variants.FirstOrDefault(x => x.TasksType == type);
                        sw.Write($";{curr?.TotalScore.ToString() ?? ""}");
                    }
                    sw.WriteLine();
                }
                sw.Flush();
                ms.Position = 0;
                return File(ms, "text/csv", fileName);
            }
            throw new Exception();
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
            catch (Exception ex)
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