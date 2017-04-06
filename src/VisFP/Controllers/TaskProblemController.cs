using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using VisFP.Data;
using VisFP.Data.DBModels;
using Microsoft.AspNetCore.Authorization;
using VisFP.Models.TaskProblemSharedViewModels;
using VisFP.Utils;

namespace VisFP.Controllers
{
    public abstract class TaskProblemController : Controller
    {
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly ApplicationDbContext _dbContext;

        public TaskProblemController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public abstract Task<IActionResult> Task(int id, Guid problemId);

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> Answer(AnswerViewModel avm)
        {
            var user = await _userManager.GetUserAsync(User);
            var problem = _dbContext.RgTaskProblems.FirstOrDefault(x => x.ProblemId == avm.TaskProblemId);
            if (problem != null || problem.User != user) //задачи нет или задача не этого юзера
            {
                int totalAttempts = GetTotalAttempts(problem.ProblemId);
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
                        new RgAttempt
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

        protected virtual int GetTotalAttempts(Guid problemId)
        {
            return _dbContext.Attempts.Count(x => x.ProblemId == problemId);
        }
    }
}