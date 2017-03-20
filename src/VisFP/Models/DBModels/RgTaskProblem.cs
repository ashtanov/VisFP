using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models.DBModels
{
    public class RgTaskProblem
    {
        [Key]
        public Guid ProblemId { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public int TaskNumber { get; set; }
        [ForeignKey("TaskId")]
        public RgTask Task { get; set; }

        public string ProblemGrammar { get; set; }
        public string RightAnswer { get; set; }
        public TaskAnswerType AnswerType { get; set; }
        public int MaxAttempts { get; set; }
        public ICollection<RgAttempt> Attempts { get; set; }
    }
}
