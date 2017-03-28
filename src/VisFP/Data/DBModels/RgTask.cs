using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Data.DBModels
{
    public class RgTask
    {
        public Guid TaskId { get; set; }
        [Display(Name = "Название задачи")]
        public string TaskTitle { get; set; }
        [Display(Name = "Номер задачи")]
        public int TaskNumber { get; set; }
        [Display(Name = "Количество терминалов в алфавите")]
        public int AlphabetTerminalsCount { get; set; }
        [Display(Name = "Количество нетерминалов в алфавите")]
        public int AlphabetNonTerminalsCount { get; set; }
        [Display(Name = "Количество терминальных правил")]
        public int TerminalRuleCount { get; set; }
        [Display(Name = "Количество нетерминальных правил")]
        public int NonTerminalRuleCount { get; set; }
        [Display(Name = "Количество попыток")]
        public int MaxAttempts { get; set; }
        [Display(Name = "Минимальная длина цепочки")]
        public int ChainMinLength { get; set; }
        [Display(Name = "Грамматика генерируется?")]
        public bool IsGrammarGenerated { get; set; }

        public Guid FixedGrammarId { get; set; }
        public RGrammar FixedGrammar { get; set; }

        public Guid GroupId { get; set; }
        public UserGroup UserGroup { get; set; }

        public ICollection<RgTaskProblem> Problems { get; set; }

    }
}
