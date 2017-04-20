using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Data.DBModels
{
    public class DbTaskProblem
    {
        public Guid ProblemId { get; set; }
        public string RightAnswer { get; set; }
        public TaskAnswerType AnswerType { get; set; }
        public int MaxAttempts { get; set; }
        public DateTime CreateDate { get; set; }
        public string TaskQuestion { get; set; }
        public int Generation { get; set; }
        public string TaskTitle { get; set; }
        public int TaskNumber { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public Guid? VariantId { get; set; }
        public DbControlVariant Variant { get; set; }

        public Guid TaskId { get; set; }
        public DbTask Task { get; set; }

        public Guid ExternalProblemId { get; set; } //id связанной проблемы в модуле

        public ICollection<DbAttempt> Attempts { get; set; }
    }
}
