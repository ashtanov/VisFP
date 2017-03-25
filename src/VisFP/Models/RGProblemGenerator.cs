using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data;
using VisFP.Data.DBModels;
using VisFP.Models.RGViewModels;
using VisFP.Utils;

namespace VisFP.Models
{
    public class RGProblemGenerator
    {
        private bool _yesNoAnswer { get; set; }
        private ApplicationDbContext _dbContext { get; set; }
        private ChainResult _currentChain { get; set; }

        public RGProblemGenerator(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<TaskViewModel> GenerateProblemAsync(int taskNumber, ApplicationUser user)
        {
            var reqTask = _dbContext.Tasks.FirstOrDefault(x => x.TaskNumber == taskNumber);
            var alphabet = Alphabet.GenerateRandom(
                reqTask.AlphabetNonTerminalsCount,
                reqTask.AlphabetTerminalsCount);
            RegularGrammar rg;
            int generation = 0;

            //Проставляем тип ответа
            TaskAnswerType answerType;
            if (taskNumber == 1 || taskNumber == 2 || taskNumber == 3)
                answerType = TaskAnswerType.SymbolsAnswer;
            else if (taskNumber == 4 || taskNumber == 5)
                answerType = TaskAnswerType.YesNoAnswer;
            else if (taskNumber == 6)
                answerType = TaskAnswerType.TextMulty;
            else
                answerType = TaskAnswerType.Text;
            //Для да/нет ответа выбираем случайный
            _yesNoAnswer = (new Random().Next(2) == 1);

            RGrammar cGrammar;
            if (reqTask.IsGrammarGenerated)
            {
                //генерируем грамматику пока она удовлетворяет условию
                do
                {
                    rg = RGGenerator.Instance.Generate(
                        ntRuleCount: reqTask.NonTerminalRuleCount,
                        tRuleCount: reqTask.TerminalRuleCount,
                        alph: alphabet);
                    generation++;
                } while (ConditionUntilForGrammar(rg, taskNumber));

                cGrammar = new RGrammar
                {
                    GrammarJson = rg.Serialize()
                };
                await _dbContext.RGrammars.AddAsync(cGrammar);
            }
            else
            {
                rg = RegularGrammar.Parse(reqTask.FixedGrammar.GrammarJson);
                cGrammar = reqTask.FixedGrammar;
            }
            

            if(taskNumber == 6)
            {
                _currentChain = rg.GenerateRandomChain(reqTask.ChainMinLength);
            }

            var answer = GetAnswer(rg, taskNumber);
            //записываем проблему в базу
            var cTask = new RgTaskProblem
            {
                RightAnswer = answer,
                TaskNumber = taskNumber,
                CurrentGrammar = cGrammar,
                Chain = _currentChain?.Chain,
                User = user,
                MaxAttempts = reqTask.MaxAttempts,
                AnswerType = answerType
            };
            await _dbContext.TaskProblems.AddAsync(cTask);
            await _dbContext.SaveChangesAsync();

            //генерируем модель для вьюхи
            var vm = new TaskViewModel(rg);
            vm.TaskText = GetTaskDescription(taskNumber);
            vm.TaskTitle = reqTask.TaskTitle;
            vm.AnswerType = answerType;
            vm.Id = cTask.ProblemId;
            vm.MaxAttempts = reqTask.MaxAttempts;
            vm.Generation = generation;
            return vm;
                
        }

        private bool ConditionUntilForGrammar(RegularGrammar rg, int taskNum)
        {
            switch (taskNum)
            {
                case 1:
                    return rg.ReachableNonterminals.Value.Length == rg.Alph.NonTerminals.Count;
                case 2:
                    return rg.GeneratingNonterminals.Value.Length == rg.Alph.NonTerminals.Count;
                case 3:
                    return rg.CyclicNonterminals.Value.Length == 0;
                case 4:
                    return rg.IsProper != _yesNoAnswer;
                case 5:
                    return rg.IsEmptyLanguage != _yesNoAnswer;
                case 6:
                    return rg.IsProper == false;
                default:
                    throw new NotImplementedException();
            }
        }

        private string GetAnswer(RegularGrammar rg, int taskNum)
        {
            switch (taskNum)
            {
                case 1:
                    return string.Join(" ", rg.Alph.NonTerminals.Except(rg.ReachableNonterminals.Value).OrderBy(z => z));
                case 2:
                    return string.Join(" ", rg.Alph.NonTerminals.Except(rg.GeneratingNonterminals.Value).OrderBy(z => z));
                case 3:
                    return string.Join(" ", rg.CyclicNonterminals.Value.OrderBy(z => z));
                case 4:
                    return _yesNoAnswer ? "yes" : "no";
                case 5:
                    return _yesNoAnswer ? "yes" : "no";
                case 6:
                    return rg.RulesForChainRepresentable(_currentChain.Chain).SerializeJsonListOfStrings();
                default:
                    throw new NotImplementedException();
            }
        }

        private string GetTaskDescription(int taskNum)
        {
            switch (taskNum)
            {
                case 1:
                    return "Отметьте ВСЕ недостижимые символы (нетерминалы)";
                case 2:
                    return "Отметьте ВСЕ пустые символы (нетерминалы)";
                case 3:
                    return "Отметьте ВСЕ циклические символы (нетерминалы)";
                case 4:
                    return "Является ли заданая грамматика приведенной?";
                case 5:
                    return "Является ли язык, порожденный заданной грамматикой, пустым?";
                case 6:
                    return $"Выпишите последовательность правил, при помощи которых можно составить данную цепочку: <strong>{_currentChain.Chain}</strong>";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
