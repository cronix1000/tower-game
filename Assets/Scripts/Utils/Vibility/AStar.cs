using System.Collections.Generic;
using UnityEngine;

namespace Utils.Vibility
{
    public class AStar : MonoBehaviour
    {
        private class Node
        {
            public Vector2Int position;
            public Node parent;
            public int g; // Cost from start
            public int h; // Heuristic to goal
            public int f; // Total cost

            public Node(Vector2Int pos)
            {
                position = pos;
            }
        }

        private Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();

        public Vector2Int[] FindPath(Vector2Int start, Vector2Int goal, List<Vector2Int> walkablePositions)
        {
            // Clear nodes from previous pathfinding
            nodes.Clear();

            // Check if start and goal are walkable
            if (!walkablePositions.Contains(start) || !walkablePositions.Contains(goal))
            {
                return null;
            }

            Node startNode = GetNode(start);
            Node goalNode = GetNode(goal);

            List<Node> openList = new List<Node>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

            openList.Add(startNode);

            while (openList.Count > 0)
            {
                // Find node with lowest f cost
                Node current = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].f < current.f || (openList[i].f == current.f && openList[i].h < current.h))
                    {
                        current = openList[i];
                    }
                }

                openList.Remove(current);
                closedSet.Add(current.position);

                // Path found
                if (current.position == goal)
                {
                    return RetracePath(startNode, goalNode);
                }

                // Check neighbors
                foreach (Vector2Int neighborPos in GetNeighbors(current.position, walkablePositions))
                {
                    if (closedSet.Contains(neighborPos)) continue;

                    int newG = current.g + GetDistance(current.position, neighborPos);
                    Node neighbor = GetNode(neighborPos);

                    if (newG < neighbor.g || !openList.Contains(neighbor))
                    {
                        neighbor.g = newG;
                        neighbor.h = GetDistance(neighbor.position, goal);
                        neighbor.parent = current;

                        if (!openList.Contains(neighbor))
                        {
                            openList.Add(neighbor);
                        }
                    }
                }
            }

            return null; // No path found
        }

        private Vector2Int[] RetracePath(Node start, Node end)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Node currentNode = end;

            while (currentNode != start)
            {
                path.Add(currentNode.position);
                currentNode = currentNode.parent;
            }

            path.Reverse();
            return path.ToArray();
        }

        private List<Vector2Int> GetNeighbors(Vector2Int position, List<Vector2Int> walkablePositions)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    Vector2Int neighborPos = new Vector2Int(position.x + x, position.y + y);

                    // Only add if it's a walkable position
                    if (walkablePositions.Contains(neighborPos))
                    {
                        neighbors.Add(neighborPos);
                    }
                }
            }

            return neighbors;
        }

        private int GetDistance(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);

            // Diagonal distance heuristic
            if (dx > dy)
                return 14 * dy + 10 * (dx - dy);
            else
                return 14 * dx + 10 * (dy - dx);
        }

        private Node GetNode(Vector2Int position)
        {
            if (!nodes.ContainsKey(position))
            {
                nodes[position] = new Node(position);
            }

            return nodes[position];
        }
    }
}