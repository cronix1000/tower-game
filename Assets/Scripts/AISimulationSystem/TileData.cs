using UnityEngine;

namespace AISimulationSystem
{
    /// <summary>
    /// Represents data for each tile in the AI simulation system.
    /// </summary>
    public class TileData
    {
        public bool isVisited;
        public bool isExplored;
        public Vector2Int position;

        public TileData(Vector2Int pos)
        {
            position = pos;
            isVisited = false;
            isExplored = false;
        }
    }
}