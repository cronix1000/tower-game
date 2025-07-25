using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Utils;

namespace AISimulationSystem
{
    // Data structure to hold information about each tile during exploration
    [Serializable]
    public class TileData
    {
        public Vector2Int position;
        public bool isExplored = false;
        public bool isVisited = false;
        public float distanceFromStart = float.MaxValue;

        public TileData(Vector2Int pos)
        {
            position = pos;
        }
    }

    // Structure to hold exploration statistics
    [Serializable]
    public struct ExplorationStats
    {
        public int totalFloorTiles;
        public int exploredTiles;
        public float explorationPercentage;
        public float explorationRadius;
        public int visitedTilesCount;

        public ExplorationStats(int total, int explored, float percentage, float radius, int visited)
        {
            totalFloorTiles = total;
            exploredTiles = explored;
            explorationPercentage = percentage;
            explorationRadius = radius;
            visitedTilesCount = visited;
        }
    }

    public class AIExplorationManager : MonoBehaviourSingletonPersistent<AIExplorationManager>
    {
        [Header("Exploration Settings")]
        public float visionRange = 5f;
        public Vector2Int goalPosition;

        [Header("References")]
        public Tilemap floorTilemap;
        public Tilemap wallTilemap; 

        // Core data structures
        public HashSet<Vector2Int> floorPositionsSet = new HashSet<Vector2Int>();
        private Dictionary<Vector2Int, TileData> tileDataMap = new Dictionary<Vector2Int, TileData>();
        private Vector2Int startPosition;

        public List<Vector2Int> wallPositionsSet = new List<Vector2Int>();
        private List<Vector2Int> floorTileCache = new List<Vector2Int>();
        private float maxDistanceFromStart = 0f;

 public IEnumerator SetTilesForCaching(int width, int height, Tilemap floorTilemap, Tilemap wallTilemap)
        {
            BoundsInt bounds = new BoundsInt(Vector3Int.zero, new Vector3Int(width, height, 1));
            TileBase[] floorPositions = floorTilemap.GetTilesBlock(bounds);
            floorPositionsSet.Clear();
            floorTileCache.Clear();

            const int batchSize = 1000;
            int processedCount = 0;

            for (int i = 0; i < floorPositions.Length; i++)
            {
                if (floorPositions[i])
                {
                    int x = i % width;
                    int y = i / width;
                    Vector2Int pos2D = new Vector2Int(x, y);
                    floorPositionsSet.Add(pos2D);
                    floorTileCache.Add(pos2D);
                }

                processedCount++;
                if (processedCount % batchSize == 0)
                {
                    yield return null;
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

                processedCount++;
                if (processedCount % batchSize == 0)
                {
                    yield return null;
                }
            }
        }
 
        public void SetStartPosition(Vector2Int pos )
        {
            startPosition = pos;
            // Initialize start tile data
            if (tileDataMap.TryGetValue(startPosition, out TileData startTile))
            {
                startTile.distanceFromStart = 0f;
            }
        }
        
        public void UpdateVision(Vector2Int agentPosition)
        {
            // Mark tiles within vision range as explored
            foreach (Vector2Int floorPos in floorPositionsSet)
            {
                float distance = Vector2.Distance(agentPosition, floorPos);
                if (distance <= visionRange)
                {
                    if (tileDataMap.TryGetValue(floorPos, out TileData data))
                    {
                        data.isExplored = true;
                    }
                }
            }

            // Mark the agent's current position as visited
            if (tileDataMap.TryGetValue(agentPosition, out TileData currentTileData))
            {
                currentTileData.isVisited = true;
            }
        }

        public void ClearExplorationData()
        {
            foreach (var kvp in tileDataMap)
            {
                kvp.Value.isExplored = false;
                kvp.Value.isVisited = false;
                kvp.Value.distanceFromStart = float.MaxValue;
            }
            // Re-initialize start tile distance
            if (tileDataMap.TryGetValue(startPosition, out TileData startTile))
            {
                startTile.distanceFromStart = 0f;
            }
            Debug.Log("Exploration data cleared.");
        }

        // --- Data Queries for AI Strategies ---

        public bool IsWalkable(Vector2Int position)
        {
            return floorPositionsSet.Contains(position);
        }

        public TileData GetTileData(Vector2Int position)
        {
            if (tileDataMap.TryGetValue(position, out TileData data))
            {
                return data;
            }
            return null; // Or return a default TileData if preferred
        }

        public List<Vector2Int> GetFrontierTiles()
        {
            List<Vector2Int> frontier = new List<Vector2Int>();
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int tilePos in floorPositionsSet)
            {
                TileData data = GetTileData(tilePos);
                if (data != null && data.isExplored && !data.isVisited)
                {
                    // Check if this explored tile has at least one unexplored neighbor
                    foreach (Vector2Int dir in directions)
                    {
                        Vector2Int neighborPos = tilePos + dir;
                        if (floorPositionsSet.Contains(neighborPos))
                        {
                            TileData neighborData = GetTileData(neighborPos);
                            if (neighborData != null && !neighborData.isExplored)
                            {
                                frontier.Add(tilePos);
                                break; // Found one unexplored neighbor, add to frontier and move on
                            }
                        }
                    }
                }
            }
            return frontier;
        }

        public ExplorationStats GetExplorationMetrics()
        {
            int totalFloorTiles = floorPositionsSet.Count;
            int exploredTiles = tileDataMap.Values.Count(data => data.isExplored);
            int visitedTiles = tileDataMap.Values.Count(data => data.isVisited);
            float percentage = totalFloorTiles > 0 ? (float)exploredTiles / totalFloorTiles : 0f;

            // Calculate exploration radius (max distance from start to any explored tile)
            float maxDistance = 0f;
            foreach (var kvp in tileDataMap)
            {
                if (kvp.Value.isExplored)
                {
                    float distance = Vector2Int.Distance(startPosition, kvp.Key);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                    }
                }
            }

            return new ExplorationStats(totalFloorTiles, exploredTiles, percentage, maxDistance, visitedTiles);
        }

        public Vector2Int GetNearestFloorTile(Vector3 worldPosition)
        {
            Vector3Int cellPosition = floorTilemap.WorldToCell(worldPosition);
            Vector2Int gridPos = new Vector2Int(cellPosition.x, cellPosition.y);

            if (floorPositionsSet.Contains(gridPos))
            {
                return gridPos;
            }

            // If the direct cell isn't a floor, find the closest floor tile
            Vector2Int closestPos = Vector2Int.zero;
            float closestDistance = float.MaxValue;

            foreach (Vector2Int floorPos in floorPositionsSet)
            {
                float distance = Vector2.Distance(worldPosition, new Vector3(floorPos.x, floorPos.y, worldPosition.z));
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPos = floorPos;
                }
            }

            return closestPos;
        }

        public Vector2Int SetGoalPosition(Vector2Int vector2Int)
        {
            // Find the furthest unexplored tile from the start position
            Vector2Int furthestTile = Vector2Int.zero;
            float maxDistance = 0f;

            foreach (Vector2Int pos in floorPositionsSet)
            {
                TileData data = GetTileData(pos);
                if (data != null && !data.isExplored)
                {
                    float distance = Vector2Int.Distance(vector2Int, pos);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        furthestTile = pos;
                    }
                }
            }

            goalPosition = furthestTile;
            return goalPosition;
        }
    }
}