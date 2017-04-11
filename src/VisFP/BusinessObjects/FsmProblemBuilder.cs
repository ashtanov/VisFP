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

namespace VisFP.BusinessObjects
{
    public class FSMProblemBuilder : RgProblemBuilder2
    {
        public FSMProblemBuilder(ApplicationDbContext dbContext) : base(dbContext)
        {
        }
        protected override RgProblemTemplate GetProblemTemplate(int taskNumber)
        {
            switch (taskNumber)
            {
                case 1:
                    return new FsmProblem1();
                case 2:
                    return new FsmProblem2();
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


    //3)является ли КА детерминированным?
    //4)допускает ли КА бесконечный язык?
    //5)допустима ли цепочка? (цепочка генерируется)

    //1)допускает ли КА хотя бы одну цепочку?
    public class FsmProblem1 : FsmProblemTemplate
    {
        public override TaskAnswerType AnswerType
        {
            get
            {
                return TaskAnswerType.YesNoAnswer;
            }
        }

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
            return "Порождает ли конечный автомат непустой язык?";
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
            return CurrentGrammar.RulesForChainRepresentable(CurrentChain.Chain).SerializeJsonListOfStrings();
        }

        public override TaskAnswerType AnswerType
        {
            get { return TaskAnswerType.TextMulty; }
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
}
