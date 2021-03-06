﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Data.DBModels
{
    public class RGrammar
    {
        public Guid GrammarId { get; set; }
        public string GrammarJson { get; set; }
        public ICollection<RgTaskProblem> Problems { get; set; }
        public ICollection<RgTask> Tasks { get; set; }
    }
}
