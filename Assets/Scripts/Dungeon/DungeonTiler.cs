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
                        wallTilemap.SetTile(position, selectedTile);
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

            // Straight walls
            if (n && !s && !e && !w) return GetTile("bottom");
            if (!n && s && !e && !w) return GetTile("top");
            if (!n && !s && e && !w) return GetTile("left");
            if (!n && !s && !e && w) return GetTile("right");

            // Corners
            if (!n && !w && e && s) return GetTile("topLeft");
            if (!n && !e && w && s) return GetTile("topRight");
            if (!s && !w && e && n) return GetTile("bottomLeft");
            if (!s && !e && w && n) return GetTile("bottomRight");

            // Complex corners with diagonals
            if (!nw && n && w) return GetTile("topRight");
            if (!ne && n && e) return GetTile("topLeft");
            if (!sw && s && w) return GetTile("bottomRight");
            if (!se && s && e) return GetTile("bottomLeft");

            // Fallback
            return GetTile("full");
        }

        TileBase GetTile(string name)
        {
            foreach (var tile in wallTiles)
            {
                if (tile.name.ToLower() == name.ToLower())
                    return tile.tile;
            }
            return null;
        }
    }

    public enum Directions
    {
        N,
        NW,
        NE,
        S,
        SW,
        SE,
        W,
        E
    }
}