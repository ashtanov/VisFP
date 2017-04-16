using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.BusinessObjects
{
    [JsonObject]
    public class PetryNode : Node
    {
        public string shape { get; set; }
    }

    [JsonObject]
    public class PetryNodeImage : PetryNode
    {
        public string image { get; set; }
    }

    /// <summary>
    /// P = {p 1 , p 2 , p 3 , p 4 , p 5 }; T = {t 1 , t 2 , t 3 , t 4 }; F = { (p 1 , t 1 ), (p 2 , t 2 ), (p 3 , t 2 ), (p 3 , t 3 ), (p 4 , t 4 ), (p 5 , t 2 ), (t 1 , p 2 ), (t 1 , p 3 ), (t 1 , p 5 ), (t 2 , p 5 ), (t 3 , p 4 ), (t 4 , p 2 ), (t 4 , p 3 ) 
    /// </summary>
    [JsonObject]
    public class PetryNetGraph : Graph<PetryNode, Edge>
    {
        public string options
        {
            get { return 
@"{
    nodes: { borderWidth: 2 },
    layout: {
        hierarchical: {
            direction: 'LR'
        },
    },
    physics: {
        enabled: false
    }
}"; }
        }
        public PetryNetGraph(PetryNet net)
        {
            Dictionary<string, int> supp = new Dictionary<string, int>();
            foreach(var node in net.P)
            {
                supp.Add(node,supp.Count);
                nodes.Add(
                    new PetryNode
                    {
                        id = supp[node],
                        label = node,
                        shape = "dot"
                    });
            }
            foreach (var node in net.T)
            {
                supp.Add(node, supp.Count);
                nodes.Add(
                    new PetryNodeImage
                    {
                        id = supp[node],
                        label = node,
                        image = "svgLineImage",
                        shape = "image"
                    });
            }
            foreach (var edge in net.F)
            {
                edges.Add(new Edge
                {
                    label = "",
                    from = supp[edge.from],
                    to = supp[edge.to],
                });
            }
        }

    }
}
