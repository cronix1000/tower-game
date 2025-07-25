using System;
using System.Collections.Generic;
using System.Linq;
using Dungeon;
using UnityEngine;
using UnityEngine.Tilemaps;
using Utils;
using Random = UnityEngine.Random;

namespace AISimulationSystem
{
    public class MapManager : MonoBehaviourSingleton<MapManager>
    {
        public Tilemap floorTilemap;
        public Tilemap wallTilemap;
        
        public HashSet<Vector2Int> floorTiles = new HashSet<Vector2Int>();
        
        public List<GameObject> entities = new List<GameObject>();
        public List<Room> rooms = new List<Room>();
        
        Vector2Int startPosition;
        Vector2Int goalPosition;
        
        public void ClearMap()
        {
            floorTiles.Clear();
            rooms.Clear();
            entities.Clear();
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
            StartCoroutine(AIExplorationManager.Instance.SetTilesForCaching(
                floorTilemap.size.x, 
                floorTilemap.size.y, 
                floorTilemap, wallTilemap));
        }
        
        public void SetStartPosition()
        {
            Vector2Int wallPosition = AIExplorationManager.Instance.wallPositionsSet[Random.Range(0, AIExplorationManager.Instance.wallPositionsSet.Count)];
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
            goalPosition = AIExplorationManager.Instance.SetGoalPosition(startPosition);
        }


        public Vector2Int GetStartPostion()
        {
            return startPosition;
        }

        public Vector2Int GetGoalPosition()
        {
            return goalPosition;
        }
    }
}