using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using Newtonsoft.Json;

namespace Dungeon
{
    public class DungeonGenerator : MonoBehaviour
    { 
        // Get rooms from json
        public Room[] Rooms;
        public int DungeonWidth;
        public int DungeonHeight;
        public int roomCount = 5; 
        public int maxAttempts = 100; 
        
        public Tile wallTile; 
        public Tilemap wallTilemap; 
        public Tile floorTile; 
        public Tilemap floorTilemap; 
        private List<Room> placedRooms = new List<Room>();
        private DungeonTiler dungeonTiler;
        
        private void Awake()
        {
            DungeonWidth = 20; 
            DungeonHeight = 20; 
        }

        private void Start()
        {
            string jsonPath = Path.Combine(Application.dataPath, "RoomLayouts.json");
            string jsonData = File.ReadAllText(jsonPath);
            Rooms = JsonConvert.DeserializeObject<Room[]>(jsonData);
            dungeonTiler = GetComponent<DungeonTiler>();

            PlaceRooms();
            PlaceHallways(); 
        }

        [Serializable]
        private class RoomListWrapper
        {
            public Room[] rooms;
        }

        public void PlaceRooms()
        {
            ClearHallways();
            
            string jsonPath = Path.Combine(Application.dataPath, "RoomLayouts.json");
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"RoomLayouts.json not found at {jsonPath}");
                return;
            }

            string jsonData = File.ReadAllText(jsonPath);
            dungeonTiler = GetComponent<DungeonTiler>();
            Rooms = JsonConvert.DeserializeObject<Room[]>(jsonData);
            
            if (Rooms == null || Rooms.Length == 0)
            {
                Debug.LogError("No rooms available to place.");
                return;
            }
            
            HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();
            
            for (int i = 0; i < roomCount; i++)
            {
                if (i >= Rooms.Length) break; // Prevent out of bounds
                
                Room room = Rooms[i];
                Vector2Int position = new Vector2Int(UnityEngine.Random.Range(0, DungeonWidth - room.width), 
                                                      UnityEngine.Random.Range(0, DungeonHeight - room.height));
                
                if (CanPlaceRoom(room, position, occupiedPositions))
                {
                    PlaceRoom(room, position, occupiedPositions);
                    room.Position = position; 
                    placedRooms.Add(room);
                    Debug.Log($"Placed room {room} at {position}");
                }
                else
                {
                    Debug.LogWarning($"Could not place room {room} at {position}");
                }
            }
            
            GenerateWallsFromFloor();
        }

        private void PlaceRoom(Room room, Vector2Int position, HashSet<Vector2Int> occupiedPositions)
        {
            for (int x = 0; x < room.width; x++)
            {
                for (int y = 0; y < room.height; y++)
                {
                    Vector2Int tilePosition = position + new Vector2Int(x, y);
                    occupiedPositions.Add(tilePosition);
                }
            }
            
            PlaceRoomTiles(room, position);
        }

        private void PlaceRoomTiles(Room room, Vector2Int position)
        {
            // Place floor tiles for the entire room area
            for (int x = 0; x < room.width; x++)
            {
                for (int y = 0; y < room.height; y++)
                {
                    Vector3Int tilePosition = new Vector3Int(position.x + x, position.y + y, 0);
                    floorTilemap.SetTile(tilePosition, floorTile);
                }
            }
        }

        private bool CanPlaceRoom(Room room, Vector2Int position, HashSet<Vector2Int> occupiedPositions)
        {
            for (int x = 0; x < room.width; x++)
            {
                for (int y = 0; y < room.height; y++)
                {
                    Vector2Int tilePosition = position + new Vector2Int(x, y);
                    if (occupiedPositions.Contains(tilePosition))
                    {
                        return false; // Position is already occupied
                    }
                }
            }
            return true;
        }
        
        private void GenerateWallsFromFloor()
        {
            bool[,] wallGrid = new bool[DungeonWidth, DungeonHeight];
            bool[,] floorGrid = new bool[DungeonWidth, DungeonHeight];

            // Mark all floor positions
            foreach (var room in placedRooms)
            {
                for (int x = room.Position.x; x < room.Position.x + room.width; x++)
                {
                    for (int y = room.Position.y; y < room.Position.y + room.height; y++)
                    {
                        if (x >= 0 && y >= 0 && x < DungeonWidth && y < DungeonHeight)
                        {
                            floorGrid[x, y] = true;
                        }
                    }
                }
            }

            // Also mark hallway positions as floors
            BoundsInt bounds = floorTilemap.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (floorTilemap.HasTile(pos))
                {
                    if (pos.x >= 0 && pos.y >= 0 && pos.x < DungeonWidth && pos.y < DungeonHeight)
                    {
                        floorGrid[pos.x, pos.y] = true;
                    }
                }
            }

            // Generate walls around floors
            for (int x = 0; x < DungeonWidth; x++)
            {
                for (int y = 0; y < DungeonHeight; y++)
                {
                    if (!floorGrid[x, y])
                    {
                        // Check if adjacent to floor
                        bool adjacentToFloor = IsAdjacentToFloor(floorGrid, x, y);
                        if (adjacentToFloor)
                        {
                            wallGrid[x, y] = true;
                        }
                    }
                }
            }

            dungeonTiler.GenerateWalls(wallGrid, DungeonWidth, DungeonHeight);
        }

        private bool IsAdjacentToFloor(bool[,] floorGrid, int x, int y)
        {
            // Check all 8 directions for more complete wall coverage
            for (int nx = x - 1; nx <= x + 1; nx++)
            {
                for (int ny = y - 1; ny <= y + 1; ny++)
                {
                    if (nx == x && ny == y) continue; // Skip the center tile
                    
                    if (nx >= 0 && ny >= 0 && nx < DungeonWidth && ny < DungeonHeight)
                    {
                        if (floorGrid[nx, ny])
                            return true;
                    }
                }
            }
            return false;
        }

        public void ClearHallways()
        {
            wallTilemap.ClearAllTiles();
            floorTilemap.ClearAllTiles();
            placedRooms.Clear();
        }

        public void PlaceHallways()
        {
            if (placedRooms.Count < 2)
            {
                Debug.LogWarning("Not enough rooms to connect.");
                return;
            }

            for (int i = 0; i < placedRooms.Count - 1; i++)
            {
                Room currentRoom = placedRooms[i];
                Room nextRoom = placedRooms[i + 1];

                Vector2Int start = GetRandomEdgeTile(currentRoom);
                Vector2Int end = GetRandomEdgeTile(nextRoom);

                DrawStraightHallway(start, end);
            }
        }
        
        private void DrawStraightHallway(Vector2Int start, Vector2Int end)
        {
            int deltaX = end.x - start.x;
            int deltaY = end.y - start.y;

            if (deltaX != 0 && deltaY != 0)
            {
                bool horizontalFirst = UnityEngine.Random.value > 0.5f;

                if (horizontalFirst)
                {
                    DrawHorizontalLine(start, end.x);
                    DrawVerticalLine(new Vector2Int(end.x, start.y), end.y);
                }
                else
                {
                    DrawVerticalLine(start, end.y);
                    DrawHorizontalLine(new Vector2Int(start.x, end.y), end.x);
                }
            }
            else
            {
                if (deltaX != 0) DrawHorizontalLine(start, end.x);
                if (deltaY != 0) DrawVerticalLine(start, end.y);
            }
        }

        private void DrawHorizontalLine(Vector2Int start, int endX)
        {
            int step = (int)Mathf.Sign(endX - start.x);
            for (int x = start.x; x != endX + step; x += step)
            {
                floorTilemap.SetTile(new Vector3Int(x, start.y, 0), floorTile);
            }
        }

        private void DrawVerticalLine(Vector2Int start, int endY)
        {
            int step = (int)Mathf.Sign(endY - start.y);
            for (int y = start.y; y != endY + step; y += step)
            {
                floorTilemap.SetTile(new Vector3Int(start.x, y, 0), floorTile);
            }
        }
        
        private Vector2Int GetRandomEdgeTile(Room room)
        {
            int edgeSide = UnityEngine.Random.Range(0, 4); // 0: top, 1: right, 2: bottom, 3: left

            int x = room.Position.x;
            int y = room.Position.y;
            int w = room.width;
            int h = room.height;

            switch (edgeSide)
            {
                case 0: // Top edge
                    return new Vector2Int(x + UnityEngine.Random.Range(0, w), y + h - 1);
                case 1: // Right edge
                    return new Vector2Int(x + w - 1, y + UnityEngine.Random.Range(0, h));
                case 2: // Bottom edge
                    return new Vector2Int(x + UnityEngine.Random.Range(0, w), y);
                case 3: // Left edge
                    return new Vector2Int(x, y + UnityEngine.Random.Range(0, h));
                default:
                    return room.Position;
            }
        }
    }
}