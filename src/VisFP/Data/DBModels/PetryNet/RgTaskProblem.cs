using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using VisFP.Data.DBModels;

namespace VisFP.Data.DBModels
{
    public class PnTaskProblem
    {
        public Guid Id { get; set; }
        public string Question { get; set; }
        public string PetryNetJson { get; set; }
        public string Answers { get; set; }
    }
}
