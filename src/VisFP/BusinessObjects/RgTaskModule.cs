using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data;
using VisFP.Data.DBModels;
using VisFP.Models.TaskProblemViewModels;

namespace VisFP.BusinessObjects
{
    public class RgTaskModule : ITaskModule
    {
        private ApplicationDbContext _dbContext { get; set; }
        private RgProblemBuilder2 _problemBuilder { get; set; }

        /// <summary>
        /// Создание модуля. Необходимо выделять отдельный dbContext!
        /// </summary>
        /// <param name="dbContext"></param>
        public RgTaskModule(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _problemBuilder = new RgProblemBuilder2();
        }

        public async Task<ProblemResult> CreateNewProblemAsync(DbTask taskTemplate)
        {
            var rgTask = await _dbContext.RgTasks.SingleOrDefaultAsync(x => x.Id == taskTemplate.ExternalTaskId);
            var problem = _problemBuilder.GenerateProblem(rgTask, taskTemplate.TaskNumber);

            RGrammar cGrammar;
            if (rgTask.IsGrammarGenerated == false)
            {
                cGrammar = new RGrammar
                {
                    GrammarJson = problem.Grammar.Serialize()
                };
                await _dbContext.RGrammars.AddAsync(cGrammar);
            }
            else
                cGrammar = rgTask.FixedGrammar;

            //записываем проблему в базу модуля
            var cTask = new RgTaskProblem
            {
                CurrentGrammar = cGrammar
            };
            await _dbContext.RgTaskProblems.AddAsync(cTask);
            await _dbContext.SaveChangesAsync();

            var mainComponent = new MainInfoComponent
            {
                Generation = problem.Generation,
                TaskQuestion = problem.ProblemQuestion,
                TaskTitle = taskTemplate.TaskTitle,
                AnswerType = problem.AnswerType,
                SymbolsForAnswer = problem.Grammar.Alph.NonTerminals
            };
            ComponentRepository repository = new ComponentRepository(mainComponent);
            repository.AddComponent(new TaskInfoTopComponent
            {
                Header = "Алфавит",
                Fields =
                    new Dictionary<string, string>
                    {
                        { "Терминалы", string.Join(", ", problem.Grammar.Alph.Terminals) },
                        { "Нетерминалы", string.Join(", ",  problem.Grammar.Alph.NonTerminals) },
                        { "Начальное состояние",  problem.Grammar.Alph.InitState.ToString() }
                    }
            });
            repository.AddComponent(new TaskInfoListComponent
            {
                Header = "Правила",
                Items = problem.Grammar.Rules.Select(x => x.ToString()),
                IsOrdered = true
            });
            repository.AddComponent(new GraphComponent
            {
                Graph = (new GrammarGraph(problem.Grammar))
            });

            return new ProblemResult
            {
                ExternalProblemId = cTask.Id,
                ProblemComponents = repository,
                Answer = problem.ProblemAnswer
            };
        }

        public async Task<ComponentRepository> GetExistingProblemAsync(DbTaskProblem problem)
        {
            var rgProblem = await _dbContext
                .RgTaskProblems
                .Include(x => x.CurrentGrammar)
                .SingleOrDefaultAsync(x => x.Id == problem.ExternalProblemId);
            var cGrammar = RegularGrammar.Parse(rgProblem.CurrentGrammar.GrammarJson);
            var mainComponent = new MainInfoComponent
            {
                Generation = problem.Generation,
                TaskQuestion = problem.TaskQuestion,
                TaskTitle = problem.TaskTitle,
                AnswerType = problem.AnswerType,
                SymbolsForAnswer = cGrammar.Alph.NonTerminals
            };
            ComponentRepository repository = new ComponentRepository(mainComponent);
            repository.AddComponent(new TaskInfoTopComponent
            {
                Header = "Алфавит",
                Fields =
                    new Dictionary<string, string>
                    {
                        { "Терминалы", string.Join(", ", cGrammar.Alph.Terminals) },
                        { "Нетерминалы", string.Join(", ",  cGrammar.Alph.NonTerminals) },
                        { "Начальное состояние",  cGrammar.Alph.InitState.ToString() }
                    }
            });
            repository.AddComponent(new TaskInfoListComponent
            {
                Header = "Правила",
                Items = cGrammar.Rules.Select(x => x.ToString()),
                IsOrdered = true
            });
            repository.AddComponent(new GraphComponent
            {
                Graph = (new GrammarGraph(cGrammar))
            });
            return repository;
        }
    }
}
