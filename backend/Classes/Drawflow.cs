using Newtonsoft.Json;
using ZigbeeHome.Enums;

namespace ZigbeeHome.Classes
{
    public class Drawflow
    {
        public List<Module> modules { get; set; }

        /// <summary>
        /// Get all the nodes in the drawflow
        /// </summary>
        public List<Node> GetNodes()
        {
            var nodes = new List<Node>();

            if (modules != null) 
            { 
                foreach (var module in modules)
                {
                    if (module.nodes != null)
                        nodes.AddRange(module.nodes);
                }
            }

            return nodes;
        }
    }

    public class Module
    {
        public string name { get; set; }
        public List<Node> nodes { get; set; }
    }

    public class Node
    {
        public int id { get; set; }
        public NodeTypeEnum name { get; set; }
        public Dictionary<string, string> data { get; set; }
        public string _class { get; set; }
        public string html { get; set; }
        public string typenode { get; set; }
        [JsonIgnore]
        public List<Input> inputs { get; set; }
        [JsonIgnore]
        public List<Output> outputs { get; set; }
        public int pos_x { get; set; }
        public int pos_y { get; set; }
    }

    public class Input
    {
        public string name { get; set; }
        public Connection[] connections { get; set; }
    }

    public class Output
    {
        public string name { get; set; }
        public Connection[] connections { get; set; }
    }

    public class Connection
    {
        /// <summary>
        /// Id of the target node
        /// </summary>
        public int node { get; set; }
        //public string input { get; set; }
        //public string output { get; set; }
        public Node target { get; set; }
    }
}
