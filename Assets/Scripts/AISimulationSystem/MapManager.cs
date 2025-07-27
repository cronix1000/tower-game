using System;
using System.Collections.Generic;
using System.Linq;
using Dungeon;
using UnityEngine;
using UnityEngine.Tilemaps;
using Utils;
using Random = UnityEngine.Random;
using IEnumerator = System.Collections.IEnumerator;

namespace AISimulationSystem
{
    public class MapManager : MonoBehaviourSingleton<MapManager>
    {
        public Tilemap floorTilemap;
        public Tilemap wallTilemap;
        public HashSet<Vector2Int> floorTiles;
        public List<GameObject> entities;
        public List<Room> rooms;

        // Exploration data (moved from AIExplorationManager)
        public Dictionary<Vector2Int, TileData> tileDataMap;
        public List<Vector2Int> wallPositionsSet;
        public float visionRange = 5f;

        public AIAgent aIAgent;

        public bool IsWalkable(Vector2Int position)
        {
            if (floorTiles.Contains(position) && !wallPositionsSet.Contains(position))
            {
                return true;
            }
            return false;
        }

        public TileData GetTileData(Vector2Int position)
        {
            if (tileDataMap.TryGetValue(position, out TileData data))
            {
                return data;
            }
            return default; // Return a default TileData
        }

        Vector2Int startPosition;
        Vector2Int goalPosition;

        public void SetTilesForCaching(int width, int height, Tilemap floorTilemap, Tilemap wallTilemap)
        {
            BoundsInt bounds = new BoundsInt(Vector3Int.zero, new Vector3Int(width, height, 1));
            TileBase[] floorPositions = floorTilemap.GetTilesBlock(bounds);
            
            if (floorTiles == null)
                floorTiles = new HashSet<Vector2Int>();
            if (wallPositionsSet == null)
                wallPositionsSet = new List<Vector2Int>();
            if (tileDataMap == null)
                tileDataMap = new Dictionary<Vector2Int, TileData>();
                
            floorTiles.Clear();

            for (int i = 0; i < floorPositions.Length; i++)
            {
                if (floorPositions[i])
                {
                    int x = i % width;
                    int y = i / width;
                    Vector2Int pos2D = new Vector2Int(x, y);
                    floorTiles.Add(pos2D);
                    tileDataMap[pos2D] = new TileData(pos2D);
                }
            }

            // Process wall tiles
            wallPositionsSet.Clear();
            BoundsInt wallBounds = wallTilemap.cellBounds;
            TileBase[] wallTiles = wallTilemap.GetTilesBlock(wallBounds);
            for (int i = 0; i < wallTiles.Length; i++)
            {
                if (wallTiles[i])
                {
                    int x = i % wallBounds.size.x;
                    int y = i / wallBounds.size.x;
                    Vector2Int pos2D = new Vector2Int(x, y);
                    wallPositionsSet.Add(pos2D);
                }
            }
        }

        public void ClearMap()
        {
            floorTiles = new HashSet<Vector2Int>();
            rooms = new List<Room>();
            entities = new List<GameObject>();
            if (tileDataMap == null)
                tileDataMap = new Dictionary<Vector2Int, TileData>();
            if (wallPositionsSet == null)
                wallPositionsSet = new List<Vector2Int>();
            
            tileDataMap.Clear();
            wallPositionsSet.Clear();
        }

        private void Start()
        {
            if (floorTilemap == null || wallTilemap == null)
            {
                Debug.LogError("Tilemaps are not assigned in MapManager.");
                return;
            }
        }

        public void AddFloorTile(Vector2Int position)
        {
            floorTiles.Add(position);
        }



        public GameObject GetGameObjectOnTile(Vector2Int position)
        {
            Collider2D results = Physics2D.OverlapBox((position - new Vector2(0.5f, 0.5f)), position + new Vector2(0.5f, 0.5f), LayerMask.GetMask("Entities"));

            if (results != null)
            {
                return results.gameObject;
            }
            return null;
        }


        public void DungeonCreated()
        {
            SetTilesForCaching(
                 floorTilemap.size.x,
                 floorTilemap.size.y,
                 floorTilemap, wallTilemap);
            SetStartPosition();
            SetGoalPosition();

            aIAgent.Initialize();
        }

        public void SetStartPosition()
        {
            if (floorTiles.Count == 0 || wallPositionsSet.Count == 0)
            {
                Debug.LogError("No floor tiles available to set start position.");
                return;
            }
            Vector2Int wallPosition = wallPositionsSet[Random.Range(0, wallPositionsSet.Count)];
            foreach (var VARIABLE in Direction2D.cardinalDirectionsList)
            {
                Vector2Int adjacentPosition = wallPosition + VARIABLE;
                if (floorTiles.Contains(adjacentPosition))
                {
                    Instance.startPosition = adjacentPosition;
                    return;
                }
            }
        }

        public void SetGoalPosition()
        {
            float maxDistance = 0f;
            Vector2Int farthestPosition = startPosition;
            foreach (var position in floorTiles)
            {
                float distance = Vector2Int.Distance(startPosition, position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthestPosition = position;
                }
            }
            goalPosition = farthestPosition;
        }


        public Vector2Int GetStartPostion()
        {
            return startPosition;
        }

        public Vector2Int GetGoalPosition()
        {
            return goalPosition;
        }

        internal void ClearExplorationData()
        {
            if (tileDataMap == null)
                tileDataMap = new Dictionary<Vector2Int, TileData>();
            
            tileDataMap.Clear();
            
            // Reinitialize tile data for all floor tiles
            foreach (var position in floorTiles)
            {
                tileDataMap[position] = new TileData(position);
            }
        }

        public bool IsOnGoalTile(Vector2Int position)
        {
            return position == goalPosition;
        }

        public void MarkTileAsVisited(Vector2Int position)
        {
            if (tileDataMap.ContainsKey(position))
            {
                tileDataMap[position].isVisited = true;
            }
        }

        public void MarkTileAsExplored(Vector2Int position)
        {
            if (tileDataMap.ContainsKey(position))
            {
                tileDataMap[position].isExplored = true;
            }
        }

        public bool IsValidPosition(Vector2Int position)
        {
            // Check if position is within reasonable bounds based on tilemap bounds if available
            if (floorTilemap != null)
            {
                BoundsInt bounds = floorTilemap.cellBounds;
                return position.x >= bounds.xMin - 10 && position.x <= bounds.xMax + 10 &&
                       position.y >= bounds.yMin - 10 && position.y <= bounds.yMax + 10;
            }
            
            // Fallback to reasonable default bounds
            return position.x >= -50 && position.x <= 50 && position.y >= -50 && position.y <= 50;
        }

        public bool BlocksLight(Vector2Int position)
        {
            // Out of bounds blocks light
            if (!IsValidPosition(position))
                return true;
                
            // Walls block light
            if (wallPositionsSet != null && wallPositionsSet.Contains(position))
                return true;
                
            // Areas without floor tiles also block light
            if (floorTiles != null && !floorTiles.Contains(position))
                return true;
                
            return false;
        }
    }
}