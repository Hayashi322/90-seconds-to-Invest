using System;
using System.Collections.Generic;
using UnityEngine;

namespace Graph
{
    // Represents a node in the graph
    public class GraphNode
    {
        public GameObject GameObjectNode { get; private set; }
        public Vector3 Position => GameObjectNode.transform.position; // Use GameObject's position
        public List<Edge> Edges { get; private set; }

        public GraphNode(GameObject gameObject)
        {
            GameObjectNode = gameObject;
            Edges = new List<Edge>();
        }

        // Adds an edge to this node
        public void AddEdge(GraphNode targetNode, float weight)
        {
            Edges.Add(new Edge(targetNode, weight));
        }
    }

    // Represents a single edge in the graph
    public class Edge
    {
        public GraphNode TargetNode { get; private set; }
        public float Weight { get; private set; }

        public Edge(GraphNode targetNode, float weight)
        {
            TargetNode = targetNode;
            Weight = weight;
        }
    }

    // Represents a weighted graph
    public class WeightedGraph
    {
        private Dictionary<GameObject, GraphNode> nodes;

        public WeightedGraph()
        {
            nodes = new Dictionary<GameObject, GraphNode>();
        }

        // Adds a GameObject as a node in the graph
        public GraphNode AddNode(GameObject gameObject)
        {
            if (!nodes.ContainsKey(gameObject))
            {
                var node = new GraphNode(gameObject);
                nodes[gameObject] = node;
                return node;
            }

            return nodes[gameObject];
        }

        // Adds a weighted edge between two GameObjects
        public void AddEdge(GameObject from, GameObject to, float weight)
        {
            if (nodes.ContainsKey(from) && nodes.ContainsKey(to))
            {
                nodes[from].AddEdge(nodes[to], weight);
            }
            else
            {
                Debug.LogError("One or both of the GameObjects are not present in the graph.");
            }
        }

        // Gets the node corresponding to a GameObject
        public GraphNode GetNode(GameObject gameObject)
        {
            nodes.TryGetValue(gameObject, out GraphNode node);
            return node;
        }

        // Prints the graph structure for debugging
        public void PrintGraph()
        {
            foreach (var nodePair in nodes)
            {
                Debug.Log($"Node: {nodePair.Key.name}");
                foreach (var edge in nodePair.Value.Edges)
                {
                    Debug.Log($"  -> {edge.TargetNode.GameObjectNode.name} (Weight: {edge.Weight})");
                }
            }
        }

        public List<GraphNode> GetAllNodes()
        {
            return new List<GraphNode>(nodes.Values);
        }
    }
}
