using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VisFP.Data;
using Microsoft.AspNetCore.Identity;
using VisFP.Data.DBModels;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using VisFP.Models.TaskProblemViewModels;
using VisFP.BusinessObjects;

namespace VisFP.Controllers
{
    public class FsmController : RegGramController
    {
        private string _areaName;
        private DbTaskType _taskType;

        protected override string AreaName
        {
            get
            {
                return _areaName ?? (_areaName = "Конечные автоматы");
            }
        }

        protected override DbTaskType ControllerTaskType
        {
            get
            {
                return _taskType ?? (_taskType = DbWorker.TaskTypes[Constants.FsmType]);
            }
        }
        public FsmController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILoggerFactory loggerFactory)
            : base(userManager, dbContext, loggerFactory)
        {
        }


        protected override async Task<RGProblemResult> GetProblem(ApplicationUser user, RgTask template, DbControlVariant variant = null)
        {
            return await (new FSMProblemBuilder(_dbContext)).GenerateProblemAsync(template, user, variant);
        }

        protected override TaskInfoViewModel GetTaskInfo(DbTaskProblem problem, RegularGrammar grammar, bool isControl)
        {
            var topInfo = new TaskInfoTopModule
            {
                Header = "Описание",
                Fields =
                    new Dictionary<string, string>
                    {
                        { "Входные символы", string.Join(", ", grammar.Alph.Terminals) },
                        { "Внутренние состояния", string.Join(", ", grammar.Alph.NonTerminals) },
                        { "Начальное состояние", grammar.Alph.InitState.ToString() },
                        { "Конечное состояние", grammar.Alph.FiniteState.ToString() }
                    }
            };
            var listInfo = new TaskInfoListModule
            {
                Header = "Функция переходов",
                Items = grammar.GetTransitionFunc(),
                IsOrdered = false
            };
            var attempts = problem.MaxAttempts - (problem.Attempts?.Count ?? 0);
            var gotRightAnswer = problem.Attempts?.Any(x => x.IsCorrect) ?? false;
            var graphModule = new GraphModule
            {
                Graph = (new GrammarGraph(grammar))
            };
            return new TaskInfoViewModel
            {
                TopInfo = topInfo,
                ListInfo = listInfo,
                SymbolsForAnswer = grammar.Alph.NonTerminals,
                TaskQuestion = problem.TaskQuestion,
                TaskTitle = problem.TaskTitle,
                AnswerType = problem.AnswerType,
                ProblemId = problem.ProblemId,
                IsControlProblem = isControl,
                LeftAttempts = attempts,
                GotRightAnswer = gotRightAnswer,
                Graph = graphModule
            };
        }
    }
}
