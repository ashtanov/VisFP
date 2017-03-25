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
        [Key]
        public int TaskNumber { get; set; }
        public string TaskTitle { get; set; }
        public int AlphabetTerminalsCount { get; set; }
        public int AlphabetNonTerminalsCount { get; set; }
        public int TerminalRuleCount { get; set; }
        public int NonTerminalRuleCount { get; set; }
        public int MaxAttempts { get; set; }
        public int ChainMinLength { get; set; }
        public bool IsGrammarGenerated { get; set; }

        public Guid FixedGrammarId { get; set; }
        [ForeignKey(nameof(FixedGrammarId))]
        public RGrammar FixedGrammar { get; set; }

        public ICollection<RgTaskProblem> Problems { get; set; }

    }
}
