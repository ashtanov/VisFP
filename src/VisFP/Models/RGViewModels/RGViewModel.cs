using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models.RGViewModels
{
    public class RGViewModel
    {
        public RegularGrammar Grammar { get; set; }
        public GrammarGraph Graph { get; private set; } 

        public RGViewModel(RegularGrammar grammar)
        {
            Grammar = grammar;
            Graph = new GrammarGraph(grammar);
        }
    }
}
