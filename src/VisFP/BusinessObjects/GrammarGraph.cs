using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.BusinessObjects
{
    [JsonObject]
    public class GNode : Node
    {
        public string color { get; set; }
    }

    [JsonObject]
    public class GrammarGraph : Graph<GNode, Edge>
    {
        public GrammarGraph(RegularGrammar gram)
        {
            Dictionary<char, int> suppDict = new Dictionary<char, int>();
            int currId = 0;
            foreach (var v in gram.Alph.NonTerminals)
            {
                string color = (v == gram.Alph.InitState ?
                                        "rgba(0,255,0,0.7)" :
                                                v == gram.Alph.FiniteState ?
                                                "rgba(255,0,0,0.7)" :
                                                "rgba(90,90,90,0.7)");
                nodes.Add(new GNode
                {
                    id = currId,
                    color = color,
                    label = v.ToString()
                });
                suppDict.Add(v, currId);
                currId++;
            }
            if (!suppDict.ContainsKey(gram.Alph.FiniteState))
            {
                nodes.Add(new GNode
                {
                    id = currId,
                    color = "rgba(255,0,0,0.7)",
                    label = gram.Alph.FiniteState.ToString()
                });
            }
            foreach (var r in gram.Rules)
            {
                edges.Add(new Edge
                {
                    from = suppDict[r.Lnt],
                    to = r.IsFinite ? currId : suppDict[r.Rnt],
                    label = r.Rt.ToString()
                });
            }
        }
    }
}
