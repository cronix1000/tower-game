using UnityEngine;

namespace AISimulationSystem
{
    public class Node
    {
        public Vector2Int coords;
        public bool isWalkable;
        public bool explored = false;
        public bool path = false;
        public Node parent;
        
        public Node(Vector2Int coords, bool isWalkable = true)
        {
            this.coords = coords;
            this.isWalkable = isWalkable;
        }
    }
}