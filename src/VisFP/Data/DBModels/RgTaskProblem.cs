using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Data.DBModels
{
    public class RgTaskProblem
    {
        [Key]
        public Guid ProblemId { get; set; }
        public string RightAnswer { get; set; }
        public TaskAnswerType AnswerType { get; set; }
        public int MaxAttempts { get; set; }

        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }

        public Guid GrammarId { get; set; }
        [ForeignKey(nameof(GrammarId))]
        public RGrammar CurrentGrammar { get; set; }

        public int TaskNumber { get; set; }
        [ForeignKey(nameof(TaskNumber))]
        public RgTask Task { get; set; }

        public ICollection<RgAttempt> Attempts { get; set; }
    }
}
