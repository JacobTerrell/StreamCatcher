using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace StreamCatcher
{
    public class BalancingInfo
    {
        [JsonProperty("edges")]
        public List<Edge> Edges { get; set; }

        [JsonProperty("preferedEdge")]
        public string PreferredEdge { get; set; }

        public class Edge
        {
            [JsonProperty("id")]
            public string ID { get; set; }
            [JsonProperty("ep")]
            public string Endpoint { get; set; }
        }
    }
}
