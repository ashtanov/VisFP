using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models.DBModels
{
    public class RgTask
    {
        [Key]
        public int TaskNumber { get; set; }
        public string TaskTitle { get; set; }
        public string TaskText { get; set; }
        public int AlphabetTerminalsCount { get; set; }
        public int AlphabetNonTerminalsCount { get; set; }
        public int TerminalRuleCount { get; set; }
        public int NonTerminalRuleCount { get; set; }
        public ICollection<RgTaskProblem> Problems { get; set; }

    }
}
