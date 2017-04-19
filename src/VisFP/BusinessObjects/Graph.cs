using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.BusinessObjects
{
    public interface IGraph
    {
    }

    [JsonObject]
    public class Node
    {
        public string label { get; set; }
        public int id { get; set; }
    }

    [JsonObject]
    public class Edge
    {
        public string id { get; set; }
        public int from { get; set; } 
        public int to { get; set; }
        public string label { get; set; }
        public string arrows { get; set; } = "to";
    }

    [JsonObject]
    public class Graph<TNode,TEdge> : IGraph
        where TNode: Node, new() 
        where TEdge: Edge, new()
    {
        public List<TNode> nodes { get; protected set; }
        public List<TEdge> edges { get; protected set; }

        public Graph()
        {
            nodes = new List<TNode>();
            edges = new List<TEdge>();
        }
    }
}
