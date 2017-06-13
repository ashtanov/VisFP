using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data;
using VisFP.Data.DBModels;
using VisFP.Models.TaskProblemViewModels;

namespace VisFP.BusinessObjects
{
    public class FsmTaskModule : RgTaskModule
    {
        /// <summary>
        /// Создание модуля
        /// </summary>
        public FsmTaskModule(IServiceScopeFactory scopeFactory) : base(scopeFactory)
        {
            _problemBuilder = new FSMProblemBuilder();
        }

        public override string GetModuleName()
        {
            return Constants.FsmType;

        }
        public override string GetModuleNameToView()
        {
            return "Конечные автоматы";
        }

        protected override TaskInfoTopComponent GetTopComponent(RegularGrammar grammar)
        {
            return new TaskInfoTopComponent
            {
                Header = "Описание",
                Fields = new Dictionary<string, string>
                    {
                        { "Входные символы", string.Join(", ", grammar.Alph.Terminals) },
                        { "Внутренние состояния", string.Join(", ", grammar.Alph.NonTerminals) },
                        { "Начальное состояние", grammar.Alph.InitState.ToString() },
                        { "Конечное состояние", grammar.Alph.FiniteState.ToString() }
                    }
            };
        }

        protected override TaskInfoListComponent GetListComponent(RegularGrammar grammar)
        {
            return new TaskInfoListComponent
            {
                Header = "Функция переходов",
                Items = grammar.GetTransitionFunc(),
                IsOrdered = false
            };
        }

        protected override List<RgTask> GetInitModuleTasks()
        {
            var result = new List<RgTask>();
            result.Add(new RgTask
            {
                TaskTitle = "Непустые языки",
                Type = GetModuleName(),
                NonTerminalRuleCount = 7,
                IsGrammarGenerated = true,
                TerminalRuleCount = 3,
                AlphabetNonTerminalsCount = 5,
                AlphabetTerminalsCount = 3,
                TaskNumber = 1,
                AnswerType = TaskAnswerType.YesNoAnswer,
                IsSeed = true
            });

            result.Add(new RgTask
            {
                TaskTitle = "Анализ цепочки",
                Type = GetModuleName(),
                NonTerminalRuleCount = 5,
                IsGrammarGenerated = true,
                TerminalRuleCount = 2,
                AlphabetNonTerminalsCount = 4,
                AlphabetTerminalsCount = 2,
                ChainMinLength = 6,
                TaskNumber = 2,
                AnswerType = TaskAnswerType.TextMulty,
                IsSeed = true
            });

            result.Add(new RgTask
            {
                TaskTitle = "Бесконечные языки",
                Type = GetModuleName(),
                NonTerminalRuleCount = 5,
                IsGrammarGenerated = true,
                TerminalRuleCount = 3,
                AlphabetNonTerminalsCount = 5,
                AlphabetTerminalsCount = 3,
                TaskNumber = 3,
                AnswerType = TaskAnswerType.YesNoAnswer,
                IsSeed = true
            });

            result.Add(new RgTask
            {
                TaskTitle = "Детерминированность автомата",
                Type = GetModuleName(),
                NonTerminalRuleCount = 7,
                IsGrammarGenerated = true,
                TerminalRuleCount = 3,
                AlphabetNonTerminalsCount = 5,
                AlphabetTerminalsCount = 3,
                TaskNumber = 4,
                AnswerType = TaskAnswerType.YesNoAnswer,
                IsSeed = true
            });

            result.Add(new RgTask
            {
                TaskTitle = "Допустимость цепочки",
                Type = GetModuleName(),
                NonTerminalRuleCount = 6,
                IsGrammarGenerated = true,
                TerminalRuleCount = 3,
                AlphabetNonTerminalsCount = 4,
                AlphabetTerminalsCount = 2,
                ChainMinLength = 6,
                TaskNumber = 5,
                AnswerType = TaskAnswerType.YesNoAnswer,
                IsSeed = true
            });
            return result;
        }
    }
}
