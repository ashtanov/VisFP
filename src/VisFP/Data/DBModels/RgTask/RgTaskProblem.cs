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
        public Guid Id { get; set; }
        public Guid GrammarId { get; set; }
        public RGrammar CurrentGrammar { get; set; }
    }
}
