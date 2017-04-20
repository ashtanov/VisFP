using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;
using VisFP.Models.TaskProblemViewModels;

namespace VisFP.BusinessObjects
{
    public class ProblemResult
    {
        public ComponentRepository ProblemComponents { get; set; }
        public Guid ExternalProblemId { get; set; }
        public string Answer { get; set; }
    }

    public interface ITaskModule
    {
        Task<ProblemResult> CreateNewProblemAsync(DbTask taskTemplate);
        Task<ComponentRepository> GetExistingProblemAsync(DbTaskProblem problem);
    }
}
