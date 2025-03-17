using System.Collections.Generic;
using UnityEngine;
using Graph;
using Astar;

public class AStarExample : MonoBehaviour
{
    private void Start()
    {
        WeightedGraph graph = new WeightedGraph();

        // Create some GameObjects with positions in the scene
        GameObject nodeA = CreateNode("NodeA", new Vector3(0, 0, 0));
        GameObject nodeB = CreateNode("NodeB", new Vector3(2, 0, 0));
        GameObject nodeC = CreateNode("NodeC", new Vector3(1, 1, 0));
        GameObject nodeD = CreateNode("NodeD", new Vector3(3, 1, 0));

        // Add nodes to the graph
        graph.AddNode(nodeA);
        graph.AddNode(nodeB);
        graph.AddNode(nodeC);
        graph.AddNode(nodeD);

        // Add weighted edges
        graph.AddEdge(nodeA, nodeB, 1.5f);
        graph.AddEdge(nodeA, nodeC, 2.0f);
        graph.AddEdge(nodeB, nodeD, 2.0f);
        graph.AddEdge(nodeC, nodeD, 1.0f);

        // Find the shortest path using A*
        List<GameObject> path = AStar.FindPath(graph, nodeA, nodeD);

        if (path != null)
        {
            Debug.Log("Path found:");
            foreach (var gameObject in path)
            {
                Debug.Log(gameObject.name);
            }
        }
    }

    // Helper method to create GameObjects at specific positions
    private GameObject CreateNode(string name, Vector3 position)
    {
        GameObject node = new GameObject(name);
        node.transform.position = position;
        return node;
    }
}