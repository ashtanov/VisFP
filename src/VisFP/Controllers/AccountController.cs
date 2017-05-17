using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VisFP.Data.DBModels;
using VisFP.Models.AccountViewModels;
using Microsoft.AspNetCore.Http;
using System.IO;
using VisFP.Utils;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using VisFP.Data;

namespace VisFP.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly Regex _loginFinder = new Regex(@"(.+)(\d+)$");

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = loggerFactory.CreateLogger<AccountController>();
            _dbContext = dbContext;
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {

            ViewData["ReturnUrl"] = returnUrl;
            if (!string.IsNullOrWhiteSpace(model.Login) && !string.IsNullOrWhiteSpace(model.Password))
            {

                var result = await _signInManager.PasswordSignInAsync(model.Login, model.Password, true, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation(1, "User logged in.");
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Неуспешная попытка логина.");
                    return View(model);
                }
            }
            else
            {
                var group = await _dbContext.UserGroups
                    .Include(x => x.Members)
                    .FirstOrDefaultAsync(x => x.Name == model.GroupName && x.IsOpen);
                if (group != null)
                {
                    return View("GroupLogin",
                        new GroupLoginViewModel
                        {
                            GroupId = group.GroupId,
                            GroupName = group.Name,
                            Users = group.Members
                        });
                }
                else
                {
                    ModelState.AddModelError("NotExistedOrNotOpen", "Указанная группа закрыта либо не существует");
                    return View(model);
                }
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GroupLogin(Guid groupId, Guid userId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId.ToString());
            if (user != null && user.UserGroupId == groupId)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                await _signInManager.RefreshSignInAsync(user);
                return RedirectToAction("Index", "Home");
            }
            return View(nameof(Login));
        }

        [Authorize(Roles = "Admin, Teacher")]
        [HttpPost]
        public async Task<IActionResult> UploadList(IFormFile fileInput, Guid groupId)
        {
            if (fileInput != null && fileInput.Length != 0)
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
                                await CreateNewStudentUser(groupId, user);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                }
            }
            return RedirectToAction("EditGroup", "Teacher", new { id = groupId });
        }

        private async Task CreateNewStudentUser(Guid groupId, string userName, string meta = "")
        {
            var pass = PasswordGenerator.Instance.GeneratePassword();
            var fio = userName.Split(' ');
            var login =
                Transliteration.Front(fio[0]) +
                string.Join("", fio.Skip(1).Select(x => Transliteration.Front(x[0].ToString())));
            while (await _userManager.Users.AnyAsync(x => x.UserName == login))
            {
                var value = _loginFinder.Match(login);
                if (value.Groups.Count == 3)
                    login = value.Groups[1].Value + (int.Parse(value.Groups[2].Value) + 1);
                else
                    login = login + 1;
            }
            var newUser = new ApplicationUser
            {
                UserName = login,
                Meta = $"Password:{pass}\t{meta ?? ""}",
                UserGroupId = groupId,
                RealName = userName
            };
            var res = await _userManager.CreateAsync(newUser, pass);
            if (!res.Succeeded)
                _logger.LogError(string.Join(Environment.NewLine, res.Errors));
            else
                await _userManager.AddToRoleAsync(newUser, "User");
        }

        [Authorize(Roles = "Admin, Teacher")]
        [HttpGet]
        public async Task<IActionResult> CreateStudent(Guid groupId)
        {
            var group = await _dbContext.UserGroups.SingleOrDefaultAsync(x => x.GroupId == groupId);
            if (group != null)
            {
                return View(new CreateStudentViewModel
                {
                    GroupName = group.Name,
                    GroupId = groupId
                });
            }
            return StatusCode(404);
        }

        [Authorize(Roles = "Admin, Teacher")]
        [HttpPost]
        public async Task<IActionResult> CreateStudent(CreateStudentViewModel newUser)
        {
            await CreateNewStudentUser(newUser.GroupId, newUser.RealName, newUser.Meta);
            return RedirectToAction("EditGroup", "Teacher", new { id = newUser.GroupId });
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new CreateUserViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserViewModel newUser)
        {
            if (!await _userManager.Users.AnyAsync(x => x.UserName == newUser.Login))
            {
                if (newUser.Role != DbRole.User)
                {
                    var user = new ApplicationUser
                    {
                        UserName = newUser.Login,
                        Meta = newUser.Meta,
                        UserGroupId = DbWorker.BaseGroupId,
                        RealName = newUser.RealName
                    };
                    var res = await _userManager.CreateAsync(user, newUser.Password);
                    if (!res.Succeeded)
                    {
                        var allErrors = string.Join(Environment.NewLine, res.Errors.Select(x => x.Description));
                        _logger.LogError(allErrors);
                        ModelState.AddModelError("", allErrors);
                        return View();
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, Enum.GetName(typeof(DbRole), newUser.Role));
                        foreach (var module in ModulesRepository.GetAllModules())
                        {
                            var ttlink = new DbTeacherTaskType
                            {
                                TeacherId = user.Id,
                                IsAvailable = true,
                                TypeId = ModulesRepository.GetModuleId(module.GetType())
                            };
                            await _dbContext.TeacherTasks.AddAsync(ttlink);
                            if (module.IsAvailableTestProblems())
                                await _dbContext.SetTasksToNewTeacherAsync(module, ttlink.Id, false);
                            if (module.IsAvailableControlProblems())
                                await _dbContext.SetTasksToNewTeacherAsync(module, ttlink.Id, true);
                        }
                        await _dbContext.SaveChangesAsync();
                    }
                    return RedirectToAction("Index", "Admin");
                }
                else
                    throw new NotImplementedException();
            }
            ModelState.AddModelError("", "Логин пользователя занят!");
            return View();
        }

        //
        // GET: /Account/Register
        //[HttpGet]
        //[AllowAnonymous]
        //public IActionResult Register(string returnUrl = null)
        //{
        //    ViewData["ReturnUrl"] = returnUrl;
        //    return View();
        //}

        ////
        //// POST: /Account/Register
        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        //{
        //    ViewData["ReturnUrl"] = returnUrl;
        //    if (ModelState.IsValid)
        //    {
        //        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        //        var result = await _userManager.CreateAsync(user, model.Password);
        //        if (result.Succeeded)
        //        {
        //            // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
        //            // Send an email with this link
        //            //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        //            //var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
        //            //await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
        //            //    $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");
        //            await _signInManager.SignInAsync(user, isPersistent: false);
        //            _logger.LogInformation(3, "User created a new account with password.");
        //            return RedirectToLocal(returnUrl);
        //        }
        //        AddErrors(result);
        //    }

        //    // If we got this far, something failed, redisplay form
        //    return View(model);
        //}

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private Task<ApplicationUser> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(RegGramController.Index), "Home");
            }
        }

        #endregion
    }
}
