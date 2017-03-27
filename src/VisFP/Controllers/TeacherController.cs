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
            var groups = _dbContext.UserGroups.Where(x => x.Creator == user);
            return View(groups);
        }

        [HttpPost]
        public async Task<IActionResult> UploadList(IFormFile fileInput, Guid groupId) //может перенести в Account с редиректом на группу?
        {
            if (fileInput.Length > 0)//нужна проверка на нулл
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    await fileInput.CopyToAsync(ms);
                    try
                    {
                        ms.Position = 0;
                        using (var sr = new StreamReader(ms))
                        {
                            var content = (await sr.ReadToEndAsync())
                                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var user in content)
                            {
                                var pass = PasswordGenerator.Instance.GeneratePassword();
                                var fio = user.Split(' ');
                                var login =
                                    Transliteration.Front(fio[0]) +
                                    string.Join("", fio.Skip(1).Select(x => Transliteration.Front(x[0].ToString())));
                                var newUser = new ApplicationUser
                                {
                                    UserName = login,
                                    Meta = $"Password:{pass}",
                                    UserGroupId = groupId,
                                    RealName = user
                                };
                                if (await _userManager.Users.AnyAsync(x => x.UserName == login))
                                {
                                    login = login + 1;
                                    newUser.UserName = login;
                                }
                                await _userManager.CreateAsync(newUser, pass);
                                await _userManager.AddToRoleAsync(newUser, "User");
                                _dbContext.SaveChanges();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex); //выскакивают ошибки при добавления списка! надо отлавливать
                    }
                }
            }
            return RedirectToAction("Edit", new { id = groupId }); 
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            var group = await _dbContext.UserGroups.Where(x => x.GroupId == id).Include(x => x.Members).FirstOrDefaultAsync();
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