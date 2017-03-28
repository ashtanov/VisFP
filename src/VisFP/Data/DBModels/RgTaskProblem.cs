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
        public Guid ProblemId { get; set; }
        public string RightAnswer { get; set; }
        public TaskAnswerType AnswerType { get; set; }
        public int MaxAttempts { get; set; }
        public string Chain { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public Guid GrammarId { get; set; }
        public RGrammar CurrentGrammar { get; set; }

        public Guid TaskId { get; set; }
        public RgTask Task { get; set; }

        public ICollection<RgAttempt> Attempts { get; set; }
    }
}
