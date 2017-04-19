using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using VisFP.Data;
using VisFP.Data.DBModels;
using Microsoft.AspNetCore.Authorization;
using VisFP.Models.TaskProblemViewModels;
using VisFP.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace VisFP.Controllers
{
    [Authorize]
    public abstract class TaskProblemController : Controller
    {
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly ApplicationDbContext _dbContext;
        protected abstract string AreaName { get; }
        protected abstract DbTaskType ControllerTaskType { get; }
        protected abstract ILogger Logger { get; }

        public TaskProblemController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = AreaName;
            var user = await _userManager.GetUserAsync(User);
            var tasksList = _dbContext.GetTasksForUser(user, false, ControllerTaskType.TaskTypeId);
            var model = new TaskListViewModel
            {
                TaskControllerName = GetType().Name.Replace("Controller", ""),
                TasksList = tasksList.Select(x => new Tuple<int, string>(x.TaskNumber, x.TaskTitle))
            };
            return View("TaskShared/Index", model);
        }

        public abstract Task<IActionResult> Task(int id, Guid? problemId);

        public async Task<IActionResult> ExamVariant()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (_dbContext.Variants.Any(x => x.User == user && !x.IsFinished && x.TaskTypeId == ControllerTaskType.TaskTypeId)) //если нет текущего варианта
                {
                    var variant = await _dbContext
                        .Variants
                        .Where(x => x.User == user && x.TaskTypeId == ControllerTaskType.TaskTypeId)
                        .OrderByDescending(x => x.CreateDate).FirstOrDefaultAsync();
                    ExamVariantViewModel model = new ExamVariantViewModel
                    {
                        CreateDate = variant.CreateDate,
                        Problems = _dbContext.GetVariantProblems(variant)
                    };
                    return View("TaskShared/ExamVariant", model);
                }
                else
                {
                    DbControlVariant variant = new DbControlVariant
                    {
                        CreateDate = DateTime.Now,
                        IsFinished = false,
                        TaskTypeId = ControllerTaskType.TaskTypeId,
                        User = user
                    };
                    await _dbContext.Variants.AddAsync(variant);

                    ExamVariantViewModel model = await AddTasksToVariant(user, variant);
                    return View("TaskShared/ExamVariant", model);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
                throw;
            }
        }

        protected abstract Task<ExamVariantViewModel> AddTasksToVariant(ApplicationUser user, DbControlVariant variant);

        [HttpPost]
        public async Task<JsonResult> Answer(AnswerViewModel avm)
        {
            var user = await _userManager.GetUserAsync(User);
            var problem = _dbContext.TaskProblems.FirstOrDefault(x => x.ProblemId == avm.TaskProblemId);

            if (problem != null || problem.User != user) //задачи нет или задача не этого юзера
            {
                int totalAttempts = _dbContext.Attempts.Count(x => x.ProblemId == problem.ProblemId);
                if (totalAttempts < problem.MaxAttempts)
                {
                    if (problem.AnswerType == TaskAnswerType.SymbolsAnswer)
                    {
                        avm.Answer = avm.Answer != null
                            ? string.Join(" ", avm.Answer.Split(' ').OrderBy(x => x))
                            : "";
                    }
                    avm.Answer = avm.Answer.Trim();
                    totalAttempts += 1; //добавили текущую попытку
                    bool isCorrect;
                    if (problem.AnswerType == TaskAnswerType.TextMulty)
                        isCorrect = problem.RightAnswer.DeserializeJsonListOfStrings().Contains(avm.Answer);
                    else
                        isCorrect = avm.Answer == problem.RightAnswer;
                    _dbContext.Attempts.Add(
                        new DbAttempt
                        {
                            Answer = avm.Answer,
                            Date = DateTime.Now,
                            IsCorrect = isCorrect,
                            Problem = problem
                        });
                    await _dbContext.SaveChangesAsync();
                    return new JsonResult(
                        new AnswerResultViewModel
                        {
                            AttemptsLeft = problem.MaxAttempts - totalAttempts,
                            IsCorrect = isCorrect
                        });
                }
                else
                    return new JsonResult(new { block = true }); //ѕревышено максимальное количество попыток
            }
            return new JsonResult("«адача не найдена или недоступна текущему пользователю") { StatusCode = 404 };
        }
    }
}