using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Data.DBModels
{
    public class DbAttempt
    {
        public Guid AttemptId { get; set; }
        public DateTime Date { get; set; }
        public string Answer { get; set; }
        public bool IsCorrect { get; set; }

        public Guid ProblemId { get; set; }
        public DbTaskProblem Problem { get; set; }
    }
}
