using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisFP.Data;
using VisFP.Data.DBModels;
using VisFP.Models.RGViewModels;
using VisFP.Utils;

namespace VisFP.Models
{
    public class RGProblemResult
    {
        public RegularGrammar Grammar { get; set; }
        public RgTaskProblem Problem { get; set; }
    }
    /// <summary>
    /// Один инстанс для генерации одной задачи
    /// </summary>
    public class RGProblemBuilder
    {
        private bool _yesNoAnswer { get; set; }
        private ApplicationDbContext _dbContext { get; set; }
        private ChainResult _currentChain { get; set; }
        private RegularGrammar _currentGrammar { get; set; }
        private Random _rand { get; set; }

        public RGProblemBuilder(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _rand = new Random();
        }

        public async Task<RGProblemResult> GenerateProblemAsync(RgTask templateTask, ApplicationUser user, RgControlVariant variant = null) //TODO: отделить от базы
        {
            var alphabet = Alphabet.GenerateRandom(
                templateTask.AlphabetNonTerminalsCount,
                templateTask.AlphabetTerminalsCount);
            int generation = 0;
            int taskNumber = templateTask.TaskNumber;

            TaskAnswerType answerType = SetAnswerType(taskNumber);

            //Для да/нет ответа выбираем случайный
            _yesNoAnswer = (_rand.Next(2) == 1);

            RGrammar cGrammar;
            if (templateTask.IsGrammarGenerated)
            {
                //генерируем грамматику до тех пор, пока она удовлетворяет условию "неподходимости"
                do
                {
                    _currentGrammar = RGGenerator.Instance.Generate(
                        ntRuleCount: templateTask.NonTerminalRuleCount,
                        tRuleCount: templateTask.TerminalRuleCount,
                        alph: alphabet);
                    generation++;
                } while (ConditionUntilForGrammar(taskNumber));

                cGrammar = new RGrammar
                {
                    GrammarJson = _currentGrammar.Serialize()
                };
                await _dbContext.RGrammars.AddAsync(cGrammar);
            }
            else
            {
                _currentGrammar = RegularGrammar.Parse(templateTask.FixedGrammar.GrammarJson);
                cGrammar = templateTask.FixedGrammar;
            }


            if (taskNumber == 6 || taskNumber == 7)
            {
                var allChains = _currentGrammar.GetAllChains(templateTask.ChainMinLength);
                _currentChain = allChains[_rand.Next(allChains.Count)];
                if (taskNumber == 7 && !_yesNoAnswer)
                {
                    var isSuccess = ChangeChainToUnrepresentable(allChains); //заменяем символы в существующих цепочках пока 
                    if (!isSuccess)
                        _yesNoAnswer = true;
                }
            }

            var answer = GetAnswer(taskNumber);
            var question = GetTaskDescription(taskNumber);

            //записываем проблему в базу
            var cTask = new RgTaskProblem
            {
                RightAnswer = answer,
                Task = templateTask,
                CurrentGrammar = cGrammar,
                User = user,
                MaxAttempts = templateTask.MaxAttempts,
                AnswerType = answerType,
                CreateDate = DateTime.Now,
                TaskQuestion = question,
                Variant = variant
            };
            await _dbContext.TaskProblems.AddAsync(cTask);
            await _dbContext.SaveChangesAsync();

            return new RGProblemResult { Grammar = _currentGrammar, Problem = cTask };
        }

        private static TaskAnswerType SetAnswerType(int taskNumber)
        {

            //Проставляем тип ответа
            TaskAnswerType answerType;
            if (taskNumber == 1 || taskNumber == 2 || taskNumber == 3)
                answerType = TaskAnswerType.SymbolsAnswer;
            else if (taskNumber == 4 || taskNumber == 5 || taskNumber == 7)
                answerType = TaskAnswerType.YesNoAnswer;
            else if (taskNumber == 6)
                answerType = TaskAnswerType.TextMulty;
            else
                answerType = TaskAnswerType.Text;
            return answerType;
        }

        private bool ChangeChainToUnrepresentable(List<ChainResult> allChains)
        {
            var isUnrepresentable = false;
            foreach (var currentChain in allChains.Select(x => x.Chain).Distinct()) //проходимся по всем допустимым цепочкам
            {
                var chainLength = currentChain.Length;
                List<Tuple<int, Queue<char>>> permList = new List<Tuple<int, Queue<char>>>();
                foreach (var term in currentChain.Select((x, i) => new { t = x, ind = i }))
                {
                    permList.Add(
                        new Tuple<int, Queue<char>>
                            (
                                term.ind,
                                new Queue<char>(
                                    _currentGrammar
                                    .Alph
                                    .Terminals
                                    .Except(new[] { term.t })
                                    .OrderBy(x => _rand.Next()))
                            )
                       );
                }
                Queue<Tuple<int, Queue<char>>> permQueue = new Queue<Tuple<int, Queue<char>>>(permList.OrderBy(x => _rand.Next()));
                Tuple<int, Queue<char>> currentItem = permQueue.Dequeue();
                StringBuilder chainForTest = null;
                var isFail = false;
                do
                {
                    if (currentItem.Item2.Count == 0)
                        if (permQueue.Count == 0)
                        {
                            isFail = true;
                            break;
                        }
                        else
                            currentItem = permQueue.Dequeue();
                    chainForTest = new StringBuilder(currentChain); //берем дефолтную
                    chainForTest[currentItem.Item1] = currentItem.Item2.Dequeue(); //меняем нетерминал
                } while (allChains.Any(x => x.Chain == chainForTest.ToString())); //пока содержится в списке

                if (!isFail)
                {
                    _currentChain = new ChainResult { Chain = chainForTest.ToString(), ChainRules = null };
                    isUnrepresentable = true;
                    break;
                }
            }
            return isUnrepresentable;
        }

        private bool ConditionUntilForGrammar(int taskNum)
        {
            switch (taskNum)
            {
                case 1:
                    return _currentGrammar.ReachableNonterminals.Value.Length == _currentGrammar.Alph.NonTerminals.Count;
                case 2:
                    return _currentGrammar.GeneratingNonterminals.Value.Length == _currentGrammar.Alph.NonTerminals.Count;
                case 3:
                    return _currentGrammar.CyclicNonterminals.Value.Length == 0;
                case 4:
                    return _currentGrammar.IsProper != _yesNoAnswer;
                case 5:
                    return _currentGrammar.IsEmptyLanguage != _yesNoAnswer;
                case 6:
                    return _currentGrammar.IsProper == false;
                case 7:
                    return _currentGrammar.IsProper == false;
                default:
                    throw new NotImplementedException();
            }
        }

        private string GetAnswer(int taskNum)
        {
            switch (taskNum)
            {
                case 1:
                    return string.Join(" ", _currentGrammar.Alph.NonTerminals.Except(_currentGrammar.ReachableNonterminals.Value).OrderBy(z => z));
                case 2:
                    return string.Join(" ", _currentGrammar.Alph.NonTerminals.Except(_currentGrammar.GeneratingNonterminals.Value).OrderBy(z => z));
                case 3:
                    return string.Join(" ", _currentGrammar.CyclicNonterminals.Value.OrderBy(z => z));
                case 4:
                    return _yesNoAnswer ? "yes" : "no";
                case 5:
                    return _yesNoAnswer ? "yes" : "no";
                case 6:
                    return _currentGrammar.RulesForChainRepresentable(_currentChain.Chain).SerializeJsonListOfStrings();
                case 7:
                    return _yesNoAnswer ? "yes" : "no";
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
                    return $"Выпишите последовательность правил (через пробел), при помощи которых можно составить данную цепочку: <strong>{_currentChain.Chain}</strong>";
                case 7:
                    return $"Выводима ли цепочка <strong>{_currentChain.Chain}</strong> в этой грамматике?";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
