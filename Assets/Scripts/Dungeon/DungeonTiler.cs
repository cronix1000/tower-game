using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon
{
    public class DungeonTiler : MonoBehaviour
    {
        [System.Serializable]
        public class TileType
        {
            public string name;
            public Directions direction;
            public TileBase tile;
        }

        public Tilemap wallTilemap;
        public TileType[] wallTiles;

        public void GenerateWalls(bool[,] wallGrid, int width, int height)
        {
            wallTilemap.ClearAllTiles();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (wallGrid[x, y])
                    {
                        Vector3Int position = new Vector3Int(x, y, 0);
                        TileBase selectedTile = GetWallTile(wallGrid, x, y, width, height);
                        if (selectedTile != null)
                        {
                            wallTilemap.SetTile(position, selectedTile);
                        }
                    }
                }
            }
        }

        TileBase GetWallTile(bool[,] wallGrid, int x, int y, int width, int height)
        {
            bool GetWall(int dx, int dy)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) return false;
                return wallGrid[nx, ny];
            }

            bool n = GetWall(0, 1);
            bool s = GetWall(0, -1);
            bool e = GetWall(1, 0);
            bool w = GetWall(-1, 0);

            bool ne = GetWall(1, 1);
            bool nw = GetWall(-1, 1);
            bool se = GetWall(1, -1);
            bool sw = GetWall(-1, -1);

            // Check for straight walls first
            if (n && !s && !e && !w) return GetTile(Directions.North);
            if (!n && s && !e && !w) return GetTile(Directions.South);
            if (!n && !s && e && !w) return GetTile(Directions.East);
            if (!n && !s && !e && w) return GetTile(Directions.West);

            // Check for corners
            if (n && w && !s && !e) return GetTile(Directions.NorthWest);
            if (n && e && !s && !w) return GetTile(Directions.NorthEast);
            if (s && w && !n && !e) return GetTile(Directions.SouthWest);
            if (s && e && !n && !w) return GetTile(Directions.SouthEast);

            // Check for T-junctions and crosses
            if (n && s && !e && !w) return GetTile(Directions.North); // Vertical wall
            if (!n && !s && e && w) return GetTile(Directions.East);  // Horizontal wall

            // Default case (single wall tile or base tile)
            return GetTile(Directions.Base);
        }

        TileBase GetTile(Directions direction)
        {
            foreach (var tile in wallTiles)
            {
                if (tile.direction == direction)
                    return tile.tile;
            }
            Debug.LogWarning($"No tile found for direction: {direction}");
            return null;
        }
    }

    public enum Directions
    {
        North,
        South,
        East,
        West,
        NorthWest,
        NorthEast,
        SouthWest,
        SouthEast,
        Base
    }
}