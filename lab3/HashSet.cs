using RAIN.Navigation.Graph;
using System;
using System.Collections.Generic;

namespace Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures
{
    public class HashSet : IClosedSet
    {
        private IDictionary<NavigationGraphNode, NodeRecord> NodeRecords { get; set; }

        public HashSet()
        {
            this.NodeRecords = new Dictionary<NavigationGraphNode, NodeRecord>();
        }
        public void Initialize()
        {
            this.NodeRecords.Clear();
        }

        public void AddToClosed(NodeRecord nodeRecord)
        {
            this.NodeRecords.Add(nodeRecord.node, nodeRecord);
        }

        public void RemoveFromClosed(NodeRecord nodeRecord)
        {
            this.NodeRecords.Remove(nodeRecord.node);
        }

        public NodeRecord SearchInClosed(NodeRecord nodeRecord)
        {
            if (this.NodeRecords.ContainsKey(nodeRecord.node))
                return this.NodeRecords[nodeRecord.node];
            return null;
        }

        public ICollection<NodeRecord> All()
        {
            return new List<NodeRecord>(this.NodeRecords.Values);
        }
    }
}
