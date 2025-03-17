using System.Collections.Generic;
using UnityEngine;
using Graph;

namespace Astar
{
    public class AStar
    {
        // Finds the shortest path using A* algorithm
        public static List<GameObject> FindPath(WeightedGraph graph, GameObject start, GameObject goal)
        {
            var startNode = graph.GetNode(start);
            var goalNode = graph.GetNode(goal);

            if (startNode == null || goalNode == null)
            {
                Debug.LogError("Start or Goal node not found in the graph.");
                return null;
            }

            // Cost dictionaries
            Dictionary<GraphNode, float> gCost = new Dictionary<GraphNode, float>();
            Dictionary<GraphNode, float> fCost = new Dictionary<GraphNode, float>();

            // Parent dictionary for reconstructing the path
            Dictionary<GraphNode, GraphNode> cameFrom = new Dictionary<GraphNode, GraphNode>();

            // Priority Queue using SortedSet for A* (open set)
            var openSet = new SortedSet<(float fCost, GraphNode node)>(new GraphNodeComparer());

            // Initialize costs
            foreach (var node in graph.GetAllNodes())
            {
                gCost[node] = float.MaxValue;
                fCost[node] = float.MaxValue;
            }

            gCost[startNode] = 0;
            fCost[startNode] = Heuristic(startNode, goalNode);

            openSet.Add((fCost[startNode], startNode));

            while (openSet.Count > 0)
            {
                // Get the node with the lowest fCost
                var current = openSet.Min.node;
                openSet.Remove(openSet.Min);

                // If we reach the goal, reconstruct the path
                if (current == goalNode)
                {
                    return ReconstructPath(cameFrom, current);
                }

                // Explore neighbors
                foreach (var edge in current.Edges)
                {
                    var neighbor = edge.TargetNode;
                    float tentativeGCost = gCost[current] + edge.Weight;

                    if (tentativeGCost < gCost[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gCost[neighbor] = tentativeGCost;
                        fCost[neighbor] = gCost[neighbor] + Heuristic(neighbor, goalNode);

                        // Add to the open set if not already present
                        if (!openSet.Contains((fCost[neighbor], neighbor)))
                        {
                            openSet.Add((fCost[neighbor], neighbor));
                        }
                    }
                }
            }

            // If we exit the loop without finding a path
            Debug.LogError("Path not found.");
            return null;
        }

        // Reconstructs the path from the goal node to the start node
        private static List<GameObject> ReconstructPath(Dictionary<GraphNode, GraphNode> cameFrom, GraphNode current)
        {
            List<GameObject> path = new List<GameObject>();
            while (cameFrom.ContainsKey(current))
            {
                path.Add(current.GameObjectNode);
                current = cameFrom[current];
            }
            path.Add(current.GameObjectNode); // Add the start node
            path.Reverse();
            return path;
        }

        // Heuristic function: Straight-line distance (Euclidean distance)
        private static float Heuristic(GraphNode a, GraphNode b)
        {
            return Vector3.Distance(a.Position, b.Position);
        }

        // Defines a comparer to create a sorted set
        // that is sorted by the file extensions.
        public class GraphNodeComparer : IComparer<(float fCost, GraphNode node)>
        {
            public int Compare((float fCost, GraphNode node) a, (float fCost, GraphNode node) b)
            {
                if (a.fCost != b.fCost) 
                    return a.fCost.CompareTo(b.fCost);
                else
                    return a.node.GameObjectNode.GetInstanceID().CompareTo(b.node.GameObjectNode.GetInstanceID());
            }
        }
    }
}
