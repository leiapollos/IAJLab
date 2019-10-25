using RAIN.Navigation.Graph;
using UnityEngine;
using Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures.HPStructures;

namespace Assets.Scripts.IAJ.Unity.Pathfinding.Heuristics
{
    public class GatewayHeuristic : IHeuristic
    {
        private ClusterGraph graph { get; set; }

        public GatewayHeuristic(ClusterGraph clusterGraph)
        {
            this.graph = clusterGraph;
        }

        public float H(NavigationGraphNode node, NavigationGraphNode goalNode)
        {
            Cluster start = graph.Quantize(node);
            Cluster goal = graph.Quantize(goalNode);

            if (start == null || goal == null || Equals(start, goal))
                return EuclideanDistance(node.LocalPosition, goalNode.LocalPosition);

            float min = float.MaxValue;

            for (int i = 0; i < start.gateways.Count; i++)
            {
                int rowId = start.gateways[i].id;

                for (int j = 0; j < goal.gateways.Count; j++)
                {
                    int entryId = goal.gateways[j].id;
                    float precomputed = graph.gatewayDistanceTable[rowId].entries[entryId].shortestDistance;

                    float h = EuclideanDistance(node.LocalPosition, start.gateways[i].Localize()) + precomputed + EuclideanDistance(goal.gateways[j].Localize(), goalNode.LocalPosition);

                    if (h < min)
                        min = h;
                }
            }

            return min;
        }


        public float EuclideanDistance(Vector3 nodePos, Vector3 goalPos)
        {
            return (goalPos - nodePos).magnitude;
        }
    }
}
