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
    /// <summary>
    /// Один инстанс для генерации одной задачи
    /// </summary>
    public class RGProblemGenerator
    {
        private bool _yesNoAnswer { get; set; }
        private ApplicationDbContext _dbContext { get; set; }
        private ChainResult _currentChain { get; set; }
        private RegularGrammar _currentGrammar { get; set; }
        private Random _rand { get; set; }
        private int MaxTry { get; set; } = 1000;

        public RGProblemGenerator(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _rand = new Random();
        }

        public async Task<TaskViewModel> GenerateProblemAsync(int taskNumber, ApplicationUser user)
        {
            var reqTask = _dbContext.Tasks.FirstOrDefault(x => x.TaskNumber == taskNumber);
            var alphabet = Alphabet.GenerateRandom(
                reqTask.AlphabetNonTerminalsCount,
                reqTask.AlphabetTerminalsCount);
            int generation = 0;

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
            //Для да/нет ответа выбираем случайный
            _yesNoAnswer = (_rand.Next(2) == 1);

            RGrammar cGrammar;
            if (reqTask.IsGrammarGenerated)
            {
                //генерируем грамматику пока она удовлетворяет условию
                do
                {
                    _currentGrammar = RGGenerator.Instance.Generate(
                        ntRuleCount: reqTask.NonTerminalRuleCount,
                        tRuleCount: reqTask.TerminalRuleCount,
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
                _currentGrammar = RegularGrammar.Parse(reqTask.FixedGrammar.GrammarJson);
                cGrammar = reqTask.FixedGrammar;
            }


            if (taskNumber == 6 || taskNumber == 7)
            {
                _currentChain = _currentGrammar.GenerateRandomChain(reqTask.ChainMinLength);
                if (taskNumber == 7 && !_yesNoAnswer)
                {
                    var isSuccess = PermutateChainForUnrepresentable();
                    while (!isSuccess && generation < MaxTry)
                    {
                        _currentChain = _currentGrammar.GenerateRandomChain(reqTask.ChainMinLength);
                        isSuccess = PermutateChainForUnrepresentable();
                        generation++;
                    }
                    if (generation > MaxTry && !isSuccess) //если не подошли 1000 перегенеренных цепочек - меняем условие
                        _yesNoAnswer = true;
                }
            }

            var answer = GetAnswer(taskNumber);
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
            var vm = new TaskViewModel(_currentGrammar);
            vm.TaskText = GetTaskDescription(taskNumber);
            vm.TaskTitle = reqTask.TaskTitle;
            vm.AnswerType = answerType;
            vm.Id = cTask.ProblemId;
            vm.MaxAttempts = reqTask.MaxAttempts;
            vm.Generation = generation;
            return vm;
        }

        private bool PermutateChainForUnrepresentable()
        {
            var chainLength = _currentChain.Chain.Length;
            List<Tuple<int, Queue<char>>> permList = new List<Tuple<int, Queue<char>>>();
            foreach (var term in _currentChain.Chain.Select((x, i) => new { t = x, ind = i }))
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
                chainForTest = new StringBuilder(_currentChain.Chain); //берем дефолтную
                chainForTest[currentItem.Item1] = currentItem.Item2.Dequeue(); //меняем нетерминал
            } while (_currentGrammar.RulesForChainRepresentable(chainForTest.ToString()).Count > 0); //выводима ли цепочка?

            if (!isFail)
                _currentChain = new ChainResult { Chain = chainForTest.ToString(), ChainRules = null };
            return !isFail;
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
