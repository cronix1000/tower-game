using System;
using System.Collections.Generic;
using System.Linq;
using AISimulationSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dungeon
{
    public class RoomFirstDungeonGenerator : SimpleRandomWalkDungeonGenerator
    {
        [SerializeField]
        private int minRoomWidth = 4, minRoomHeight = 4;
        [SerializeField]
        private int dungeonWidth = 20, dungeonHeight = 20;
        [SerializeField]
        [Range(0,10)]
        private int offset = 1;
        [SerializeField]
        private bool randomWalkRooms = false;
        
        // New fields for strategic dungeon design
        [SerializeField] private int minRoomsPerFloor = 3;
        [SerializeField] private int maxRoomsPerFloor = 8;
        
        List<Room> rooms = new List<Room>();
        Dictionary<Room, List<Room>> roomConnections = new Dictionary<Room, List<Room>>();
        Room startRoom;
        Room exitRoom;
        
        [SerializeField] bool createDungeonOnAwake = true;

        protected override void RunProceduralGeneration()
        {
            CreateStrategicDungeon();
        }

        private void Awake()
        {
            if (createDungeonOnAwake)
                GenerateDungeon();
            else
            {
                CreateStartAndGoal();
            }
        }

        private void CreateStartAndGoal()
        {
            MapManager.Instance.DungeonCreated();
        }

        private void CreateStrategicDungeon()
        {
            MapManager.Instance.ClearMap();
            
            // Generate room layout
            var roomsList = ProceduralGenerationAlgorithms.BinarySpacePartitioning(
                new BoundsInt((Vector3Int)startPosition, new Vector3Int(dungeonWidth, dungeonHeight, 0)), 
                minRoomWidth, minRoomHeight);

            // Limit rooms for strategic gameplay
            roomsList = roomsList.Take(Random.Range(minRoomsPerFloor, maxRoomsPerFloor + 1)).ToList();

            HashSet<Vector2Int> allFloorTiles = new HashSet<Vector2Int>();
            rooms.Clear();
            roomConnections.Clear();

            // Create individual rooms with separate floor positions
            foreach (var roomBounds in roomsList)
            {
                Room room = CreateIndividualRoom(roomBounds);
                rooms.Add(room);
                allFloorTiles.UnionWith(room.floorPositions);
            }

            // Create strategic connections (not just nearest neighbor)
            CreateStrategicConnections();
            
            // Generate corridors based on connections
            HashSet<Vector2Int> corridors = CreateCorridorsFromConnections();
            allFloorTiles.UnionWith(corridors);

            // Designate start and exit rooms
            DesignateStartAndExit();

            // Pass data to MapManager
            MapManager.Instance.rooms = rooms;
            MapManager.Instance.roomConnections = roomConnections; // New field needed in MapManager
            MapManager.Instance.floorTiles = allFloorTiles;
            MapManager.Instance.startRoom = startRoom;
            MapManager.Instance.exitRoom = exitRoom;

            // Visualize - paint both rooms and corridors
            tilemapVisualizer.PaintFloorTiles(allFloorTiles);
            WallGenerator.CreateWalls(allFloorTiles, tilemapVisualizer);
            
            CreateStartAndGoal();
        }

        private Room CreateIndividualRoom(BoundsInt roomBounds)
        {
            var roomCenter = new Vector2Int(Mathf.RoundToInt(roomBounds.center.x), Mathf.RoundToInt(roomBounds.center.y));
            Room room = new Room(roomBounds.xMax - roomBounds.xMin, roomBounds.yMax - roomBounds.yMin, roomCenter);
            
            
            // Each room gets its own floor positions HashSet
            HashSet<Vector2Int> roomFloor = new HashSet<Vector2Int>();

            if (randomWalkRooms)
            {
                var walkFloor = RunRandomWalk(randomWalkParameters, roomCenter);
                foreach (var position in walkFloor)
                {
                    if(position.x >= (roomBounds.xMin + offset) && position.x <= (roomBounds.xMax - offset) && 
                       position.y >= (roomBounds.yMin + offset) && position.y <= (roomBounds.yMax - offset))
                    {
                        roomFloor.Add(position);
                    }
                }
            }
            else
            {
                for (int col = offset; col < roomBounds.size.x - offset; col++)
                {
                    for (int row = offset; row < roomBounds.size.y - offset; row++)
                    {
                        Vector2Int position = (Vector2Int)roomBounds.min + new Vector2Int(col, row);
                        roomFloor.Add(position);
                    }
                }
            }

            room.floorPositions = roomFloor;
            room.isEmpty = true; // New field - room starts empty, player fills it
            return room;
        }

        private void CreateStrategicConnections()
        {
            if (rooms.Count == 0) return;

            // Initialize connections dictionary
            foreach (var room in rooms)
            {
                roomConnections[room] = new List<Room>();
            }

            // Create strategic path layouts
            CreatePathLayout();
            
            // Debug: Log connections created
            int totalConnections = roomConnections.Values.Sum(list => list.Count);
            Debug.Log($"Created {totalConnections} room connections between {rooms.Count} rooms");
        }

        private void CreatePathLayout()
        {
            // Sort rooms by position to create interesting paths
            var sortedRooms = rooms.OrderBy(r => r.center.x + r.center.y * 0.1f).ToList();
            
            // Choose path type based on room count
            if (rooms.Count <= 4)
            {
                CreateLinearPath(sortedRooms);
            }
            else if (rooms.Count <= 6)
            {
                CreateBranchingPath(sortedRooms);
            }
            else
            {
                CreateComplexPath(sortedRooms);
            }
        }

        private void CreateLinearPath(List<Room> sortedRooms)
        {
            for (int i = 0; i < sortedRooms.Count - 1; i++)
            {
                ConnectRooms(sortedRooms[i], sortedRooms[i + 1]);
            }
        }

        private void CreateBranchingPath(List<Room> sortedRooms)
        {
            // Main path
            int mainPathLength = sortedRooms.Count - 2;
            for (int i = 0; i < mainPathLength; i++)
            {
                ConnectRooms(sortedRooms[i], sortedRooms[i + 1]);
            }
            
            // Branch from middle room to last rooms
            int branchPoint = mainPathLength / 2;
            for (int i = mainPathLength; i < sortedRooms.Count; i++)
            {
                ConnectRooms(sortedRooms[branchPoint], sortedRooms[i]);
            }
        }

        private void CreateComplexPath(List<Room> sortedRooms)
        {
            // Create multiple paths with choices
            CreateLinearPath(sortedRooms.Take(4).ToList());
            
            // Add optional branches
            for (int i = 4; i < sortedRooms.Count; i++)
            {
                int connectionPoint = Random.Range(1, 3); // Connect to early rooms
                ConnectRooms(sortedRooms[connectionPoint], sortedRooms[i]);
            }
        }

        private void ConnectRooms(Room from, Room to)
        {
            if (!roomConnections[from].Contains(to))
            {
                roomConnections[from].Add(to);
            }
        }

        private HashSet<Vector2Int> CreateCorridorsFromConnections()
        {
            HashSet<Vector2Int> corridors = new HashSet<Vector2Int>();
            
            foreach (var kvp in roomConnections)
            {
                Room fromRoom = kvp.Key;
                foreach (Room toRoom in kvp.Value)
                {
                    var corridor = CreateCorridor(fromRoom.center, toRoom.center);
                    corridors.UnionWith(corridor);
                }
            }
            
            return corridors;
        }

        private void DesignateStartAndExit()
        {
            if (rooms.Count < 2) return;

            // Find rooms with specific connection patterns
            startRoom = rooms.Where(r => roomConnections[r].Count > 0).OrderBy(r => r.center.x).First();
            exitRoom = rooms.Where(r => roomConnections.Values.Any(connections => connections.Contains(r)))
                           .OrderBy(r => -r.center.x).First();
            
            startRoom.isStartRoom = true;
            exitRoom.isExitRoom = true;
        }

        // Keep existing corridor creation methods
        private HashSet<Vector2Int> CreateCorridor(Vector2Int currentRoomCenter, Vector2Int destination)
        {
            HashSet<Vector2Int> corridor = new HashSet<Vector2Int>();
            var position = currentRoomCenter;
            corridor.Add(position);
            
            
            while (position.y != destination.y)
            {
                if(destination.y > position.y)
                {
                    position += Vector2Int.up;
                }
                else if(destination.y < position.y)
                {
                    position += Vector2Int.down;
                }
                corridor.Add(position);
            }
            
            while (position.x != destination.x)
            {
                if (destination.x > position.x)
                {
                    position += Vector2Int.right;
                }
                else if(destination.x < position.x)
                {
                    position += Vector2Int.left;
                }
                corridor.Add(position);
            }
            
            return corridor;
        }
    }
}