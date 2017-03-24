using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Models
{
    public interface IProblem
    {
        string TaskTitle { get; }
        string TaskDescription { get; }
        string Answer { get; }
        TaskAnswerType AnswerType { get; }
    }
}
