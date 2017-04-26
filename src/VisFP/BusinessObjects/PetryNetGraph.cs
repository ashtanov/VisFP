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
    public class PetryEdge : Edge
    {
        public string value { get; set; }
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
            get
            {
                return
//@"{
//    nodes: { borderWidth: 2 },
//    layout: {
//        hierarchical: {
//            direction: 'LR'
//        },
//    },
//    physics: {
//        enabled: false
//    }
//}";
@"{
    nodes: { borderWidth: 2 }
}";
            }
        }
        public PetryNetGraph(PetryNet net)
        {
            Dictionary<string, int> supp = new Dictionary<string, int>();

            for (int i = 0; i < net.P.Length; ++i)
            {
                supp.Add(net.P[i], supp.Count);
                if (net.Markup != null && !string.IsNullOrEmpty(net.Markup[i]) && net.Markup[i] != "0")
                {
                    nodes.Add(
                        new PetryNodeImage
                        {
                            id = supp[net.P[i]],
                            label = net.P[i],
                            image = $"getCircleWithText({net.Markup[i]})",
                            shape = "image"
                        });
                }
                else
                {
                    nodes.Add(
                        new PetryNode
                        {
                            id = supp[net.P[i]],
                            label = net.P[i],
                            shape = "dot"
                        });
                }
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
                if (!string.IsNullOrEmpty(edge.w))
                {
                    edges.Add(new PetryEdge
                    {
                        label = edge.w,
                        value = "10",
                        from = supp[edge.from],
                        to = supp[edge.to],
                    });
                }
                else
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
}
