using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XNode;

namespace KitchenECSExplorer.Utils
{
    internal static class GraphUtils
    {
        static FieldInfo f_ports = typeof(Node).GetField("ports", BindingFlags.NonPublic | BindingFlags.Instance);

        public static Dictionary<NodePort, List<NodePort>> GetConnections(Node node)
        {
            object obj = f_ports.GetValue(node);

            Dictionary<NodePort, List<NodePort>> connections = new Dictionary<NodePort, List<NodePort>>();
            if (obj == null || !(obj is Dictionary<string, NodePort> nodeDictionary))
                return connections;
            foreach (NodePort nodePort in nodeDictionary.Values)
            {
                if (nodePort == null)
                    continue;
                connections[nodePort] = new List<NodePort>();
                foreach (NodePort connectedPort in nodePort.GetConnections().OrderByDescending(nodePort => nodePort.direction))
                {
                    connections[nodePort].Add(connectedPort);
                }
            }
            return connections;
        }
    }
}
