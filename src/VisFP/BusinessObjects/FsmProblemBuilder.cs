using System;
using System.Linq;
using VisFP.Data.DBModels;
using VisFP.Utils;

namespace VisFP.BusinessObjects
{
    public class FSMProblemBuilder : RgProblemBuilder2
    {
        protected override RgProblemTemplate GetProblemTemplate(int taskNumber)
        {
            switch (taskNumber)
            {
                case 1:
                    return new FsmProblem1();
                case 2:
                    return new FsmProblem2();
                case 3:
                    return new FsmProblem3();
                case 4:
                    return new FsmProblem4();
                case 5:
                    return new FsmProblem5();
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public abstract class FsmProblemTemplate : RgProblemTemplate
    {
        public override void GenerateGrammar(RgTask templateTask, Alphabet alphabet)
        {
            CurrentGrammar = Generator.Instance.GenerateFsm(
                ntRuleCount: templateTask.NonTerminalRuleCount,
                tRuleCount: templateTask.TerminalRuleCount,
                alph: alphabet);
        }

        public override Alphabet GetAlphabet(RgTask templateTask)
        {
            return Alphabet.GenerateRandomFsm(
                templateTask.AlphabetNonTerminalsCount,
                templateTask.AlphabetTerminalsCount);
        }
    }

    //1)допускает ли КА хотя бы одну цепочку?
    public class FsmProblem1 : FsmProblemTemplate
    {
        public override bool ConditionUntilForGrammar()
        {
            return YesNoAnswer ? CurrentGrammar.IsEmptyLanguage : !CurrentGrammar.IsEmptyLanguage;
        }

        public override string GetAnswer()
        {
            return YesNoAnswer ? "yes" : "no";
        }

        public override string GetTaskDescription()
        {
            return "Допускает ли КА хотя бы одну цепочку?";
        }

        public override void SetCurrentChain(RgTask templateTask)
        {
        }
    }

    //2)указать последовательность состояний КА при анализе заданной цепочки
    public class FsmProblem2 : FsmProblemTemplate
    {
        public override bool ConditionUntilForGrammar()
        {
            return CurrentGrammar.IsProper == false;
        }

        public override string GetAnswer()
        {
            return CurrentGrammar
                .RulesForChainRepresentable(CurrentChain.Chain)
                .Select(x => CurrentGrammar.GetStateSequenceForResult(x)).ToList()
                .SerializeJsonListOfStrings();
        }

        public override string GetTaskDescription()
        {
            return $"Выпишите последовательность состояний КА (через пробел) при анализе данной цепочки: <strong>{CurrentChain.Chain}</strong>";
        }

        public override void SetCurrentChain(RgTask templateTask)
        {
            var allChains = CurrentGrammar.GetAllChains(templateTask.ChainMinLength);
            CurrentChain = allChains[new Random().Next(allChains.Count)];
        }
    }

    //3)допускает ли КА бесконечный язык?
    public class FsmProblem3 : FsmProblemTemplate
    {

        public override bool ConditionUntilForGrammar()
        {
            return YesNoAnswer ? !CurrentGrammar.IsInfinityLanguage : CurrentGrammar.IsInfinityLanguage;
        }

        public override string GetAnswer()
        {
            return YesNoAnswer ? "yes" : "no";
        }

        public override string GetTaskDescription()
        {
            return "Порождает ли конечный автомат бесконечный язык?";
        }

        public override void SetCurrentChain(RgTask templateTask)
        {
        }
    }

    //4)является ли КА детерминированным?
    public class FsmProblem4 : FsmProblemTemplate
    {

        public override bool ConditionUntilForGrammar()
        {
            return YesNoAnswer ? !CurrentGrammar.IsDeterministic : CurrentGrammar.IsDeterministic;
        }

        public override string GetAnswer()
        {
            return YesNoAnswer ? "yes" : "no";
        }

        public override string GetTaskDescription()
        {
            return "Является ли данный автомат детерминированным?";
        }

        public override void SetCurrentChain(RgTask templateTask)
        {
        }
    }

    //5)допустима ли цепочка? (цепочка генерируется)
    public class FsmProblem5 : FsmProblemTemplate
    {
        public override bool ConditionUntilForGrammar()
        {
            return !CurrentGrammar.IsProper;
        }

        public override string GetAnswer()
        {
            return YesNoAnswer ? "yes" : "no";
        }

        public override string GetTaskDescription()
        {
            return $"Допустима ли цепочка <strong>{CurrentChain.Chain}</strong> данным КА?";
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
}
