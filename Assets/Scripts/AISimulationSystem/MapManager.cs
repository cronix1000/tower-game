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
        [Header("Tilemap References")]
        public Tilemap floorTilemap;
        public Tilemap wallTilemap;
        
        [Header("Map Data")]
        public HashSet<Vector2Int> floorTiles;
        public List<GameObject> entities;
        public List<Room> rooms;

        [Header("Strategic Dungeon Data")]
        public Dictionary<Room, List<Room>> roomConnections;
        public Room startRoom;
        public Room exitRoom;
        
        [Header("AI Management")]
        public List<AIAgent> aiAgents = new List<AIAgent>();
        public AIAgent aiAgentPrefab; // Reference to AI prefab
        
        [Header("Game State")]
        public GamePhase currentPhase = GamePhase.Design;
        public int currentFloorNumber = 1;
        
        // Exploration data
        public Dictionary<Vector2Int, TileData> tileDataMap;
        public List<Vector2Int> wallPositionsSet;
        public float visionRange = 5f;

        // Legacy support - keeping for backward compatibility
        Vector2Int startPosition;
        Vector2Int goalPosition;

        public enum GamePhase
        {
            Design,     // Player is filling rooms with challenges
            Simulation, // AIs are moving through dungeon
            Results     // Showing outcomes, collecting rewards
        }

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
            return default;
        }

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
            roomConnections = new Dictionary<Room, List<Room>>();
            
            if (tileDataMap == null)
                tileDataMap = new Dictionary<Vector2Int, TileData>();
            if (wallPositionsSet == null)
                wallPositionsSet = new List<Vector2Int>();
            
            tileDataMap.Clear();
            wallPositionsSet.Clear();
            
            // Clear AI agents
            ClearAIAgents();
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
            
            // Set legacy positions for backward compatibility
            SetLegacyPositions();
            
            // Initialize rooms for strategic gameplay
            InitializeRoomsForDesign();
            
            // Start in design phase
            currentPhase = GamePhase.Design;
            
            Debug.Log($"Dungeon created with {rooms.Count} rooms. Design phase started.");
        }

        private void SetLegacyPositions()
        {
            if (startRoom != null)
            {
                startPosition = startRoom.center;
            }
            else
            {
                SetStartPosition(); // Fallback to old method
            }
            
            if (exitRoom != null)
            {
                goalPosition = exitRoom.center;
            }
            else
            {
                SetGoalPosition(); // Fallback to old method
            }
        }

        private void InitializeRoomsForDesign()
        {
            foreach (var room in rooms)
            {
                // Mark all rooms as empty and ready for player design
                room.isEmpty = true;
                room.assignedChallenge = null;
                
                // Initialize room visual state (could trigger UI updates)
                OnRoomStateChanged(room);
            }
        }

        // Strategic gameplay methods
        public void AssignChallengeToRoom(Room room, ChallengeCard challenge)
        {
            if (currentPhase != GamePhase.Design)
            {
                Debug.LogWarning("Can only assign challenges during design phase!");
                return;
            }
            
            if (room == null || challenge == null)
            {
                Debug.LogError("Invalid room or challenge assignment!");
                return;
            }
            
            room.assignedChallenge = challenge;
            room.isEmpty = false;
            
            OnRoomStateChanged(room);
            Debug.Log($"Assigned {challenge.challengeName} to room at {room.center}");
        }

        public void RemoveChallengeFromRoom(Room room)
        {
            if (currentPhase != GamePhase.Design)
            {
                Debug.LogWarning("Can only remove challenges during design phase!");
                return;
            }
            
            if (room != null)
            {
                room.assignedChallenge = null;
                room.isEmpty = true;
                OnRoomStateChanged(room);
            }
        }

        public bool CanStartSimulation()
        {
            // Check if at least the critical path has challenges
            if (startRoom == null || exitRoom == null)
                return false;
                
            // Ensure there's at least one path from start to exit with some challenges
            return HasValidPathToExit();
        }

        private bool HasValidPathToExit()
        {
            // Simple path validation - you might want more sophisticated logic
            if (roomConnections == null || !roomConnections.ContainsKey(startRoom))
                return false;
                
            // BFS to check if exit is reachable
            Queue<Room> toVisit = new Queue<Room>();
            HashSet<Room> visited = new HashSet<Room>();
            
            toVisit.Enqueue(startRoom);
            visited.Add(startRoom);
            
            while (toVisit.Count > 0)
            {
                Room current = toVisit.Dequeue();
                
                if (current == exitRoom)
                    return true;
                    
                if (roomConnections.ContainsKey(current))
                {
                    foreach (Room connected in roomConnections[current])
                    {
                        if (!visited.Contains(connected))
                        {
                            visited.Add(connected);
                            toVisit.Enqueue(connected);
                        }
                    }
                }
            }
            
            return false;
        }

        public void StartSimulation()
        {
            if (!CanStartSimulation())
            {
                Debug.LogError("Cannot start simulation - invalid dungeon setup!");
                return;
            }
            
            currentPhase = GamePhase.Simulation;
            SpawnAIAgents();
            Debug.Log("Simulation phase started!");
        }

        private void SpawnAIAgents()
        {
            if (aiAgentPrefab == null || startRoom == null)
            {
                Debug.LogError("Cannot spawn AI agents - missing prefab or start room!");
                return;
            }
            
            ClearAIAgents();
            
            // Spawn multiple AI agents in the start room
            int agentCount = 5; // You can make this configurable
            
            for (int i = 0; i < agentCount; i++)
            {
                Vector3 spawnPosition = new Vector3(startRoom.center.x, startRoom.center.y, 0f);
                
                // Add slight random offset so they don't all spawn on same tile
                spawnPosition += new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
                
                GameObject agentObj = Instantiate(aiAgentPrefab.gameObject, spawnPosition, Quaternion.identity);
                AIAgent agent = agentObj.GetComponent<AIAgent>();
                
                if (agent != null)
                {
                    agent.Initialize();
                    agent.currentRoom = startRoom;
                    aiAgents.Add(agent);
                    entities.Add(agentObj);
                }
            }
            
            Debug.Log($"Spawned {aiAgents.Count} AI agents in start room");
        }

        private void ClearAIAgents()
        {
            foreach (var agent in aiAgents)
            {
                if (agent != null && agent.gameObject != null)
                {
                    entities.Remove(agent.gameObject);
                    DestroyImmediate(agent.gameObject);
                }
            }
            aiAgents.Clear();
        }

        public void EndSimulation()
        {
            currentPhase = GamePhase.Results;
            
            // Analyze results
            AnalyzeSimulationResults();
            
            Debug.Log("Simulation ended - showing results");
        }

        private void AnalyzeSimulationResults()
        {
            int survivingAgents = aiAgents.Count(agent => agent != null && !agent.isDead);
            int agentsAtExit = aiAgents.Count(agent => agent != null && agent.currentRoom == exitRoom);
            
            Debug.Log($"Simulation Results: {survivingAgents} agents survived, {agentsAtExit} reached the exit");
            
            // Here you could trigger UI updates, calculate rewards, etc.
        }

        public void PrepareNextFloor()
        {
            currentFloorNumber++;
            currentPhase = GamePhase.Design;
            
            // Clear current floor data
            ClearAIAgents();
            
            // You might want to regenerate the dungeon here
            // or load a new floor layout
            
            Debug.Log($"Preparing floor {currentFloorNumber}");
        }

        // Room utility methods
        public Room GetRoomAtPosition(Vector2Int position)
        {
            return rooms?.FirstOrDefault(room => room.floorPositions?.Contains(position) == true);
        }

        public List<Room> GetConnectedRooms(Room room)
        {
            if (roomConnections != null && roomConnections.ContainsKey(room))
            {
                return roomConnections[room];
            }
            return new List<Room>();
        }

        public List<Room> GetEmptyRooms()
        {
            return rooms?.Where(room => room.isEmpty).ToList() ?? new List<Room>();
        }

        public List<Room> GetFilledRooms()
        {
            return rooms?.Where(room => !room.isEmpty).ToList() ?? new List<Room>();
        }

        // Event system for UI updates
        public System.Action<Room> OnRoomStateChanged;
        public System.Action<GamePhase> OnPhaseChanged;

        // Legacy methods - keeping for backward compatibility
        public void SetStartPosition()
        {
            if (floorTiles.Count == 0 || wallPositionsSet.Count == 0)
            {
                Debug.LogError("No floor tiles available to set start position.");
                return;
            }
            Vector2Int wallPosition = wallPositionsSet[Random.Range(0, wallPositionsSet.Count)];
            foreach (var direction in Direction2D.cardinalDirectionsList)
            {
                Vector2Int adjacentPosition = wallPosition + direction;
                if (floorTiles.Contains(adjacentPosition))
                {
                    startPosition = adjacentPosition;
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

        public void ClearExplorationData()
        {
            if (tileDataMap == null)
                tileDataMap = new Dictionary<Vector2Int, TileData>();
            
            tileDataMap.Clear();
            
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
            if (floorTilemap != null)
            {
                BoundsInt bounds = floorTilemap.cellBounds;
                return position.x >= bounds.xMin - 10 && position.x <= bounds.xMax + 10 &&
                       position.y >= bounds.yMin - 10 && position.y <= bounds.yMax + 10;
            }
            
            return position.x >= -50 && position.x <= 50 && position.y >= -50 && position.y <= 50;
        }

        public bool BlocksLight(Vector2Int position)
        {
            if (!IsValidPosition(position))
                return true;
                
            if (wallPositionsSet != null && wallPositionsSet.Contains(position))
                return true;
                
            if (floorTiles != null && !floorTiles.Contains(position))
                return true;
                
            return false;
        }
    }

    [System.Serializable]
    public class ChallengeCard
    {
        public string challengeName;
        public string description;
        public ChallengeType type;
        // Add other challenge properties
    }

    public enum ChallengeType
    {
        Combat,
        Trap,
        Puzzle,
        Social,
        Environmental
    }
}