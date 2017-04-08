using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Data.DBModels
{
    public class RgTaskProblem : DbTaskProblem
    {
        public Guid GrammarId { get; set; }
        public RGrammar CurrentGrammar { get; set; }

        public Guid TaskId { get; set; }
        public RgTask Task { get; set; }
    }
}
