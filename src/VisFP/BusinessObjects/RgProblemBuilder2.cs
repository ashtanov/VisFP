using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisFP.Data;
using VisFP.Data.DBModels;
using VisFP.Utils;

namespace VisFP.BusinessObjects
{
    public class RGProblemResult
    {
        public RegularGrammar Grammar { get; set; }
        public RgTaskProblem Problem { get; set; }
    }

    public class RgProblemBuilder2
    {
        private ApplicationDbContext _dbContext { get; set; }
        public RgProblemBuilder2(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<RGProblemResult> GenerateProblemAsync(
            RgTask templateTask,
            ApplicationUser user,
            DbControlVariant variant = null)
        {
            RgProblemTemplate tp = GetProblemTemplate(templateTask.TaskNumber);
            Alphabet alphabet = tp.GetAlphabet(templateTask);
            int generation = 0;
            int chainTry = 0;

            RGrammar cGrammar = null;
            while (chainTry < 10)
            {
                if (templateTask.IsGrammarGenerated)
                {
                    //генерируем грамматику до тех пор, пока она удовлетворяет условию "неподходимости"
                    do
                    {
                        tp.GenerateGrammar(templateTask, alphabet);
                        generation++;
                    } while (tp.ConditionUntilForGrammar());

                    cGrammar = new RGrammar
                    {
                        GrammarJson = tp.CurrentGrammar.Serialize()
                    };
                    await _dbContext.RGrammars.AddAsync(cGrammar);
                }
                else
                {
                    tp.CurrentGrammar = RegularGrammar.Parse(templateTask.FixedGrammar.GrammarJson);
                    cGrammar = templateTask.FixedGrammar;
                }
                try
                {
                    tp.SetCurrentChain(templateTask); //работа с цепочками
                    break;
                }
                catch (CantGenerateChainException)
                {
                    chainTry++;
                    if (chainTry == 10 || !templateTask.IsGrammarGenerated)
                        throw new Exception("Не удалось за 10 попыток сгенерировать грамматику с нужными цепочками");
                }
            }
            //записываем проблему в базу
            var cTask = new RgTaskProblem
            {
                RightAnswer = tp.GetAnswer(),
                Task = templateTask,
                CurrentGrammar = cGrammar,
                User = user,
                MaxAttempts = templateTask.MaxAttempts,
                AnswerType = tp.AnswerType,
                CreateDate = DateTime.Now,
                TaskQuestion = tp.GetTaskDescription(),
                Variant = variant,
                Generation = generation,
                TaskNumber = templateTask.TaskNumber,
                TaskTitle = templateTask.TaskTitle
            };
            await _dbContext.RgTaskProblems.AddAsync(cTask);
            return new RGProblemResult { Grammar = tp.CurrentGrammar, Problem = cTask };
        }
        protected virtual RgProblemTemplate GetProblemTemplate(int taskNumber)
        {
            RgProblemTemplate tp;
            switch (taskNumber)
            {
                case 1:
                    tp = new RgProblem1();
                    break;
                case 2:
                    tp = new RgProblem2();
                    break;
                case 3:
                    tp = new RgProblem3();
                    break;
                case 4:
                    tp = new RgProblem4();
                    break;
                case 5:
                    tp = new RgProblem5();
                    break;
                case 6:
                    tp = new RgProblem6();
                    break;
                case 7:
                    tp = new RgProblem7();
                    break;
                case 8:
                    tp = new RgProblem8();
                    break;
                default:
                    throw new NotImplementedException();
            }
            return tp;
        }
    }

    public abstract class RgProblemTemplate
    {
        public ChainResult CurrentChain { get; set; }
        public RegularGrammar CurrentGrammar { get; set; }
        public bool YesNoAnswer { get; set; }
        private Random _rand;

        public RgProblemTemplate()
        {
            _rand = new Random();
            //Для да/нет ответа выбираем случайный ответ
            YesNoAnswer = (_rand.Next(2) == 1);
        }

        public virtual void GenerateGrammar(RgTask templateTask, Alphabet alphabet)
        {
            CurrentGrammar = Generator.Instance.GenerateRg(
                ntRuleCount: templateTask.NonTerminalRuleCount,
                tRuleCount: templateTask.TerminalRuleCount,
                alph: alphabet);
        }

        public virtual Alphabet GetAlphabet(RgTask templateTask)
        {
            return Alphabet.GenerateRandomRg(
                templateTask.AlphabetNonTerminalsCount,
                templateTask.AlphabetTerminalsCount);
        }

        public abstract void SetCurrentChain(RgTask templateTask);

        public abstract TaskAnswerType AnswerType { get; }

        public abstract bool ConditionUntilForGrammar();

        public abstract string GetAnswer();

        public abstract string GetTaskDescription();

        protected bool ChangeChainToUnrepresentable(List<ChainResult> allChains)
        {
            var rand = new Random();
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
                                    CurrentGrammar
                                    .Alph
                                    .Terminals
                                    .Except(new[] { term.t })
                                    .OrderBy(x => rand.Next()))
                            )
                       );
                }
                Queue<Tuple<int, Queue<char>>> permQueue = new Queue<Tuple<int, Queue<char>>>(permList.OrderBy(x => rand.Next()));
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
                    CurrentChain = new ChainResult { Chain = chainForTest.ToString(), ChainRules = null };
                    isUnrepresentable = true;
                    break;
                }
            }
            return isUnrepresentable;
        }
    }
    public class RgProblem1 : RgProblemTemplate
    {
        public override bool ConditionUntilForGrammar()
        {
            return CurrentGrammar.ReachableNonterminals.Value.Length == CurrentGrammar.Alph.NonTerminals.Count;
        }

        public override string GetAnswer()
        {
            return string.Join(" ", CurrentGrammar.Alph.NonTerminals.Except(CurrentGrammar.ReachableNonterminals.Value).OrderBy(z => z));
        }

        public override TaskAnswerType AnswerType
        {
            get { return TaskAnswerType.SymbolsAnswer; }
        }

        public override string GetTaskDescription()
        {
            return "Отметьте ВСЕ недостижимые символы (нетерминалы)";
        }

        public override void SetCurrentChain(RgTask templateTask)
        {
        }
    }
    public class RgProblem2 : RgProblemTemplate
    {
        public override bool ConditionUntilForGrammar()
        {
            return CurrentGrammar.GeneratingNonterminals.Value.Length == CurrentGrammar.Alph.NonTerminals.Count;
        }

        public override string GetAnswer()
        {
            return string.Join(" ", CurrentGrammar.Alph.NonTerminals.Except(CurrentGrammar.GeneratingNonterminals.Value).OrderBy(z => z));
        }

        public override TaskAnswerType AnswerType
        {
            get { return TaskAnswerType.SymbolsAnswer; }
        }

        public override string GetTaskDescription()
        {
            return "Отметьте ВСЕ пустые символы (нетерминалы)";
        }

        public override void SetCurrentChain(RgTask templateTask)
        {
        }
    }
    public class RgProblem3 : RgProblemTemplate
    {
        public override bool ConditionUntilForGrammar()
        {
            return CurrentGrammar.CyclicNonterminals.Value.Length == 0;
        }

        public override string GetAnswer()
        {
            return string.Join(" ", CurrentGrammar.CyclicNonterminals.Value.OrderBy(z => z));
        }

        public override TaskAnswerType AnswerType
        {
            get { return TaskAnswerType.SymbolsAnswer; }
        }

        public override string GetTaskDescription()
        {
            return "Отметьте ВСЕ циклические символы (нетерминалы)";
        }

        public override void SetCurrentChain(RgTask templateTask)
        {
        }
    }
    public class RgProblem4 : RgProblemTemplate
    {
        public override bool ConditionUntilForGrammar()
        {
            return CurrentGrammar.IsProper != YesNoAnswer;
        }

        public override string GetAnswer()
        {
            return YesNoAnswer ? "yes" : "no";
        }

        public override TaskAnswerType AnswerType
        {
            get { return TaskAnswerType.YesNoAnswer; }
        }

        public override string GetTaskDescription()
        {
            return "Является ли заданая грамматика приведенной?";
        }

        public override void SetCurrentChain(RgTask templateTask)
        {
        }
    }
    public class RgProblem5 : RgProblemTemplate
    {
        public override bool ConditionUntilForGrammar()
        {
            return CurrentGrammar.IsEmptyLanguage != YesNoAnswer;
        }

        public override string GetAnswer()
        {
            return YesNoAnswer ? "yes" : "no";
        }

        public override TaskAnswerType AnswerType
        {
            get { return TaskAnswerType.YesNoAnswer; }
        }

        public override string GetTaskDescription()
        {
            return "Является ли язык, порожденный заданной грамматикой, пустым?";
        }

        public override void SetCurrentChain(RgTask templateTask)
        {
        }
    }
    public class RgProblem6 : RgProblemTemplate
    {
        public override bool ConditionUntilForGrammar()
        {
            return CurrentGrammar.IsProper == false;
        }

        public override string GetAnswer()
        {
            return CurrentGrammar.RulesForChainRepresentable(CurrentChain.Chain).SerializeJsonListOfStrings();
        }

        public override TaskAnswerType AnswerType
        {
            get { return TaskAnswerType.TextMulty; }
        }

        public override string GetTaskDescription()
        {
            return $"Выпишите последовательность правил (через пробел), при помощи которых можно составить данную цепочку: <strong>{CurrentChain.Chain}</strong>";
        }

        public override void SetCurrentChain(RgTask templateTask)
        {
            var allChains = CurrentGrammar.GetAllChains(templateTask.ChainMinLength);
            CurrentChain = allChains[new Random().Next(allChains.Count)];
        }
    }
    public class RgProblem7 : RgProblemTemplate
    {
        public override bool ConditionUntilForGrammar()
        {
            return CurrentGrammar.IsProper == false;
        }

        public override string GetAnswer()
        {
            return YesNoAnswer ? "yes" : "no";
        }

        public override TaskAnswerType AnswerType
        {
            get { return TaskAnswerType.YesNoAnswer; }
        }

        public override string GetTaskDescription()
        {
            return $"Выводима ли цепочка <strong>{CurrentChain.Chain}</strong> в этой грамматике?";
        }

        public override void SetCurrentChain(RgTask templateTask)
        {
            var allChains = CurrentGrammar.GetAllChains(templateTask.ChainMinLength);
            CurrentChain = allChains[new Random().Next(allChains.Count)];
            if (!YesNoAnswer)
            {
                var isSuccess = ChangeChainToUnrepresentable(allChains); //заменяем символы в существующих цепочках пока 
                if (!isSuccess) //если все возможные цепочки выводимы, меняем условие
                    YesNoAnswer = true;
            }
        }
    }
    public class RgProblem8 : RgProblemTemplate
    {
        public override bool ConditionUntilForGrammar()
        {
            return CurrentGrammar.IsProper == false;
        }

        public override string GetAnswer()
        {
            return YesNoAnswer ? "yes" : "no";
        }

        public override TaskAnswerType AnswerType
        {
            get { return TaskAnswerType.YesNoAnswer; }
        }

        public override string GetTaskDescription()
        {
            return $"Выводима ли цепочка <strong>{CurrentChain.Chain}</strong> двумя и более способами?";
        }

        public override void SetCurrentChain(RgTask templateTask)
        {
            var allChains = CurrentGrammar.GetAllChains(templateTask.ChainMinLength);
            CurrentChain = allChains[new Random().Next(allChains.Count)];
            IGrouping<string, ChainResult> chain;
            if (YesNoAnswer) //если должно быть более 2х выводов
            {
                chain = allChains.GroupBy(x => x.Chain).Where(x => x.Count() > 1).FirstOrDefault();
                if (chain == null) //нет такой цепочки
                    YesNoAnswer = false; //оставляем выбраную, меняем ответ
                else
                    CurrentChain = chain.FirstOrDefault();
            }
            else
            {
                chain = allChains.GroupBy(x => x.Chain).Where(x => x.Count() == 1).FirstOrDefault();
                if (chain == null) //все цепочки выводимы более 1 раза
                    YesNoAnswer = true; //оставляем выбраную, меняем ответ
                else
                    CurrentChain = chain.FirstOrDefault();
            }
        }
    }
}
