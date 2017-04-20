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
using VisFP.BusinessObjects;

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
        protected abstract ITaskModule TaskModule { get; }

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

        public async Task<IActionResult> Task(int id, Guid? problemId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (!problemId.HasValue) //тренировочна€ задача
                {
                    var templateTask = _dbContext
                        .GetTasksForUser(user, false, ControllerTaskType.TaskTypeId)
                        .FirstOrDefault(x => x.TaskNumber == id);
                    var problem = await TaskModule.CreateNewProblemAsync(templateTask);

                    DbTaskProblem dbProblem = CreateDbProblem(user, templateTask, problem);
                    await _dbContext.TaskProblems.AddAsync(dbProblem);
                    await _dbContext.SaveChangesAsync();

                    TaskInfoViewModel viewModel = new TaskInfoViewModel(new TaskBaseInfo
                    {
                        GotRightAnswer = false,
                        IsControlProblem = false,
                        LeftAttempts = templateTask.MaxAttempts,
                        ProblemId = dbProblem.ProblemId
                    }, problem.ProblemComponents);
                    return View("TaskShared/TaskView", viewModel);
                }
                else
                {
                    var currentProblem = await _dbContext
                        .TaskProblems
                        .Include(x => x.Task)
                        .Include(x => x.Attempts)
                        .FirstOrDefaultAsync(x => x.ProblemId == problemId.Value);
                    if (currentProblem != null)
                    {
                        var problemComponents = await TaskModule.GetExistingProblemAsync(currentProblem);


                        if (currentProblem.VariantId == null)
                        {
                            TaskInfoViewModel viewModel = new TaskInfoViewModel(new TaskBaseInfo
                            {
                                GotRightAnswer = currentProblem.Attempts?.Any(x => x.IsCorrect) ?? false,
                                IsControlProblem = false,
                                LeftAttempts = currentProblem.MaxAttempts - (currentProblem.Attempts?.Count ?? 0),
                                ProblemId = currentProblem.ProblemId
                            }, problemComponents);
                            return View("TaskShared/TaskView", viewModel);
                        }
                        else
                        {
                            var currentVariant = await _dbContext
                                       .Variants
                                       .FirstOrDefaultAsync(x => x.VariantId == currentProblem.VariantId);
                            var examViewModel = new ExamTaskInfoViewModel(new TaskBaseInfo
                            {
                                GotRightAnswer = currentProblem.Attempts?.Any(x => x.IsCorrect) ?? false,
                                IsControlProblem = true,
                                LeftAttempts = currentProblem.MaxAttempts - (currentProblem.Attempts?.Count ?? 0),
                                ProblemId = currentProblem.ProblemId
                            },
                            problemComponents,
                            _dbContext.GetVariantProblems(currentVariant));
                            return View("TaskShared/ExamTaskView", examViewModel);
                        }
                    }
                    else
                        return Error();
                }
            }
            catch (Exception ex)
            {
                return Error();
            }
        }

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

        public IActionResult Error()
        {
            return View();
        }

        #region helpers

        private DbTaskProblem CreateDbProblem(ApplicationUser user, DbTask templateTask, ProblemResult problem)
        {
            var mainModule = problem.ProblemComponents.GetComponent<MainInfoComponent>();
            var dbProblem = new DbTaskProblem
            {
                AnswerType = mainModule.AnswerType,
                MaxAttempts = templateTask.MaxAttempts,
                CreateDate = DateTime.Now,
                ExternalProblemId = problem.ExternalProblemId,
                Generation = mainModule.Generation,
                RightAnswer = problem.Answer,
                TaskId = templateTask.TaskId,
                TaskNumber = templateTask.TaskNumber,
                TaskQuestion = mainModule.TaskQuestion,
                TaskTitle = templateTask.TaskTitle,
                UserId = user.Id
            };
            return dbProblem;
        }

        private async Task<ExamVariantViewModel> AddTasksToVariant(ApplicationUser user, DbControlVariant variant)
        {
            var templateTasks = _dbContext.GetTasksForUser(user, true, ControllerTaskType.TaskTypeId);

            List<DbTaskProblem> problems = new List<DbTaskProblem>();
            foreach (var template in templateTasks) //√енерим задачи
            {
                var problem = await TaskModule.CreateNewProblemAsync(template);
                problems.Add(CreateDbProblem(user, template, problem));
            }
            await _dbContext.TaskProblems.AddRangeAsync(problems);
            await _dbContext.SaveChangesAsync();
            ExamVariantViewModel model = new ExamVariantViewModel
            {
                CreateDate = variant.CreateDate,
                Problems = new List<ExamProblem>(
                    problems.Select(
                        x => new ExamProblem
                        {
                            ProblemId = x.ProblemId,
                            State = ProblemState.Unfinished,
                            TaskNumber = x.TaskNumber,
                            TaskTitle = x.TaskTitle
                        })).OrderBy(x => x.TaskNumber)
            };
            return model;
        }

        #endregion
    }
}