using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models
{
    public class GraphJS : Graph
    {
        [JsonIgnore]
        public virtual string SerializedGraph
        {
            get
            {
                return JsonConvert.SerializeObject(this);
            }
        }
        public GraphJS(Graph graph)
        {
            this.edges = graph.edges;
            this.nodes = graph.nodes;
        }

        
    }
}
