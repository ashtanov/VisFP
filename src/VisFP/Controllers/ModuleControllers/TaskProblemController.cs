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
        protected ITaskModule _taskModule;
        protected readonly ILogger _logger;

        public TaskProblemController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = loggerFactory.CreateLogger(GetType());
        }

        protected abstract ITaskModule SetCurrentModule();

        public Guid TaskTypeId
        {
            get
            {
                return ModulesRepository.GetModuleId(TaskModule.GetType());
            }
        }

        protected ITaskModule TaskModule
        {
            get
            {
                return _taskModule ?? (_taskModule = SetCurrentModule());
            }
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = TaskModule.GetModuleNameToView();
            var user = await _userManager.GetUserAsync(User);
            var tasksList = _dbContext.GetTasksForUser(user, false, TaskTypeId);
            var model = new TaskListViewModel
            {
                TaskControllerName = _taskModule.GetModuleName(),
                TasksList = tasksList.Select(x => new Tuple<int, string>(x.TaskNumber, x.TaskTitle))
            };
            return View("TaskShared/Index", model);
        }

        public async Task<IActionResult> ExamVariant()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (_dbContext.Variants.Any(x => x.User == user && x.TaskTypeId == TaskTypeId)) //���� ��� �������� ��������
                {
                    var variant = await _dbContext
                        .Variants
                        .Where(x => x.User == user && x.TaskTypeId == TaskTypeId)
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
                        TaskTypeId = TaskTypeId,
                        User = user
                    };
                    await _dbContext.Variants.AddAsync(variant);

                    ExamVariantViewModel model = await AddTasksToVariant(user, variant);
                    return View("TaskShared/ExamVariant", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        public async Task<IActionResult> Task(int id, Guid? problemId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (!problemId.HasValue) //������������� ������
                {
                    var templateTask = _dbContext
                        .GetTasksForUser(user, false, TaskTypeId)
                        .FirstOrDefault(x => x.TaskNumber == id);
                    var problem = await TaskModule.CreateNewProblemAsync(templateTask.ExternalTaskId);

                    DbTaskProblem dbProblem = CreateDbProblem(user, templateTask, problem);
                    await _dbContext.TaskProblems.AddAsync(dbProblem);
                    await _dbContext.SaveChangesAsync();

                    TaskInfoViewModel viewModel = new TaskInfoViewModel(new TaskBaseInfo
                    {
                        GotRightAnswer = false,
                        IsControlProblem = false,
                        LeftAttempts = templateTask.MaxAttempts,
                        ProblemId = dbProblem.ProblemId
                    }, problem.ProblemComponents, TaskModule.GetModuleName());

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
                            }, problemComponents, TaskModule.GetModuleName());
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
                                TaskModule.GetModuleName(),
                                _dbContext.GetVariantProblems(currentVariant)
                            );
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
            var problem = _dbContext.TaskProblems.Include(x => x.Variant).FirstOrDefault(x => x.ProblemId == avm.TaskProblemId);

            if (problem != null || problem.User != user) //������ ��� ��� ������ �� ����� �����
            {
                int totalAttempts = _dbContext.Attempts.Count(x => x.ProblemId == problem.ProblemId);
                if (totalAttempts < problem.MaxAttempts)
                {
                    if (problem.AnswerType == TaskAnswerType.SymbolsAnswer || problem.AnswerType == TaskAnswerType.CheckBoxAnswer)
                    {
                        avm.Answer = avm.Answer != null
                            ? string.Join(" ", avm.Answer.Split(' ').OrderBy(x => x))
                            : "";
                    }
                    avm.Answer = avm.Answer.Trim();
                    totalAttempts += 1; //�������� ������� �������
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
                    bool isFinished = false;
                    if (problem.VariantId != null)
                    {
                        var problems = _dbContext.GetVariantProblems(problem.Variant);
                        if(problems.All(x => x.State != ProblemState.Unfinished))
                        {
                            isFinished = true;
                            problem.Variant.IsFinished = true;
                        }
                    }
                    await _dbContext.SaveChangesAsync();
                    return new JsonResult(
                        new AnswerResultViewModel
                        {
                            AttemptsLeft = problem.MaxAttempts - totalAttempts,
                            IsCorrect = isCorrect,
                            IsVariantFinished = isFinished
                        });
                }
                else
                    return new JsonResult(new { block = true }); //��������� ������������ ���������� �������
            }
            return new JsonResult("������ �� ������� ��� ���������� �������� ������������") { StatusCode = 404 };
        }

        public IActionResult Error()
        {
            return View();
        }

        #region helpers

        private DbTaskProblem CreateDbProblem(ApplicationUser user, DbTask templateTask, ProblemResult problem, Guid? variantId = null)
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
                UserId = user.Id,
                VariantId = variantId
            };
            return dbProblem;
        }

        private async Task<ExamVariantViewModel> AddTasksToVariant(ApplicationUser user, DbControlVariant variant)
        {
            var templateTasks = _dbContext.GetTasksForUser(user, true, TaskTypeId);

            List<DbTaskProblem> problems = new List<DbTaskProblem>();
            foreach (var template in templateTasks) //������� ������
            {
                var problem = await TaskModule.CreateNewProblemAsync(template.ExternalTaskId);
                problems.Add(CreateDbProblem(user, template, problem, variant.VariantId));
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