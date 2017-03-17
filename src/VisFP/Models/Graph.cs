using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models
{
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
    }

    [JsonObject]
    public class Graph
    {
        public List<Node> nodes { get; protected set; }
        public List<Edge> edges { get; protected set; }

        public Graph()
        {
            nodes = new List<Node>();
            edges = new List<Edge>();
        }
    }
}
