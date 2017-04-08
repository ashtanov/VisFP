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
    /// <summary>
    /// Один инстанс для генерации одной задачи
    /// </summary>
    public class FSMProblemBuilder : RGProblemBuilder
    {

        public FSMProblemBuilder(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        protected override Alphabet GetAlphabet(RgTask templateTask)
        {
            return Alphabet.GenerateRandomFsm(templateTask.AlphabetNonTerminalsCount, templateTask.AlphabetTerminalsCount);
        }

        protected override void GenerateGrammar(RgTask templateTask, Alphabet alphabet)
        {
            _currentGrammar = Generator.Instance.GenerateFSM(templateTask.NonTerminalRuleCount, templateTask.TerminalRuleCount, alphabet);
        }

    }
}
