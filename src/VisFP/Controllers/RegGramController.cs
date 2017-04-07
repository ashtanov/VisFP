using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisFP.Models;
using VisFP.Models.RGViewModels;
using VisFP.Data;
using Microsoft.AspNetCore.Identity;
using VisFP.Data.DBModels;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using VisFP.Models.TaskProblemSharedViewModels;
using VisFP.BusinessObjects;

namespace VisFP.Controllers
{
    public class RegGramController : TaskProblemController
    {
        private readonly ILogger _logger;

        public RegGramController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
            :base(userManager,dbContext)
        {
            _logger = loggerFactory.CreateLogger<RegGramController>();
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            return View(_dbContext
                .RgTasks
                .Where(x => x.GroupId == DbWorker.BaseGroupId)
                .Select(x => new Tuple<int, string>(x.TaskNumber, x.TaskTitle)));
        }

        public async Task<IActionResult> ExamVariant(Guid? groupId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (_dbContext.Variants.Any(x => x.User == user && !x.IsFinished)) //если нет текущего варианта
                {
                    var variant = await _dbContext
                        .Variants
                        .Where(x => x.User == user)
                        .OrderByDescending(x => x.CreateDate).FirstOrDefaultAsync();
                    DbWorker worker = new DbWorker(_dbContext);
                    ExamVariantViewModel model = new ExamVariantViewModel
                    {
                        CreateDate = variant.CreateDate,
                        Problems = worker.GetVariantProblems(variant)
                    };
                    return View(model);
                }
                else
                {
                    if (groupId.HasValue)
                        throw new NotImplementedException(); //преподы могут свои группы выбирать

                    var templateTasks = _dbContext //выбираем шаблоны тасков переопределенные для группы
                                    .RgTasks
                                    .Where(x => x.GroupId == user.UserGroupId);
                    RgControlVariant variant = new RgControlVariant
                    {
                        CreateDate = DateTime.Now,
                        IsFinished = false,
                        User = user
                    };
                    await _dbContext.Variants.AddAsync(variant);

                    List<RgTaskProblem> problems = new List<RgTaskProblem>();
                    foreach (var template in templateTasks) //Генерим задачи
                    {
                        var problem = await (new RGProblemBuilder(_dbContext)).GenerateProblemAsync(template, user, variant);
                        problems.Add(problem.Problem);
                    }
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
                                    TaskNumber = x.Task.TaskNumber,
                                    TaskTitle = x.Task.TaskTitle
                                })).OrderBy(x => x.TaskNumber)
                    };
                    return View(model);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        [Authorize]
        public override async Task<IActionResult> Task(int id, Guid problemId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (problemId.Equals(default(Guid))) //тренировочная задача
                {
                    var builder = new RGProblemBuilder(_dbContext);
                    RgTask templateTask = _dbContext //выбираем шаблон таска базовый
                            .RgTasks
                            .FirstOrDefault(
                                x => x.TaskNumber == id &&
                                x.GroupId == DbWorker.BaseGroupId);
                    RGProblemResult problem = await builder.GenerateProblemAsync(templateTask, user);
                    await _dbContext.SaveChangesAsync();
                    var viewModel = new RgProblemViewModel(problem.Grammar, problem.Problem);
                    return View("TaskView", viewModel);
                }
                else
                {
                    var currentProblem =
                        await _dbContext
                        .RgTaskProblems
                        .Include(x => x.CurrentGrammar)
                        .Include(x => x.Task)
                        .Include(x => x.Attempts)
                        .FirstOrDefaultAsync(x => x.ProblemId == problemId);
                    if (currentProblem != null)
                    {
                        if (currentProblem.VariantId == Guid.Empty) //задача без варианта
                        {
                            var viewModel =
                                new RgProblemViewModel(
                                    RegularGrammar.Parse(currentProblem.CurrentGrammar.GrammarJson),
                                    currentProblem,
                                    currentProblem.MaxAttempts - currentProblem.Attempts.Count);
                            return View("TaskView", viewModel);
                        }
                        else
                        {
                            var currentVariant =
                               await _dbContext
                               .Variants
                               .Include(x => x.Problems)
                               .FirstOrDefaultAsync(x => x.VariantId == currentProblem.VariantId);
                            DbWorker worker = new DbWorker(_dbContext);
                            var viewModel =
                                new ExamRgProblemViewModel(
                                    RegularGrammar.Parse(currentProblem.CurrentGrammar.GrammarJson),
                                    currentProblem,
                                    currentProblem.MaxAttempts - currentProblem.Attempts.Count,
                                    worker.GetVariantProblems(currentVariant));
                            return View("ExamTaskView", viewModel);
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

        [Authorize]
        [HttpPost]
        public JsonResult SaveGraph(string graph)
        {
            try
            {
                var d = Newtonsoft.Json.JsonConvert.DeserializeObject(graph);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
