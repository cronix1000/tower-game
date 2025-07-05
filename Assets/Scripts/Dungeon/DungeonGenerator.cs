using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon
{
    public class DungeonGenerator : MonoBehaviour
    { 
        // Get rooms from json
        public Room[] Rooms { get; private set; }
        public int DungeonWidth { get; private set; }
        public int DungeonHeight { get; private set; }
        public int RoomCount = 5; // Try placing up to this many rooms
        public int MaxAttempts = 100; // Prevent infinite loops when placing rooms
        
        public Tile wallTile; // Tile to use for walls
        public Tilemap wallTilemap; // Tilemap to place walls on
        public Tile floorTile; // Tile to use for floors
        public Tilemap floorTilemap; // Tilemap to place floors on
        
        
        private void Awake()
        {
            DungeonWidth = 20; // Set your dungeon width
            DungeonHeight = 20; // Set your dungeon height
        }

        private void Start()
        {
            string jsonPath = Path.Combine(Application.dataPath, "RoomLayouts.json");
            string jsonData = File.ReadAllText(jsonPath);
            RoomListWrapper wrapper = JsonUtility.FromJson<RoomListWrapper>(jsonData);
            Rooms = wrapper.rooms;
        
        }

        [Serializable]
        private class RoomListWrapper
        {
            public Room[] rooms;
        }

        public void PlaceRooms()
        {
            ClearHallways();
            
            if (Rooms == null || Rooms.Length == 0)
            {
                Debug.LogError("No rooms available to place.");
                return;
            }
            
            HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();
            
            for (int i = 0; i < RoomCount; i++)
            {
                if (i >= Rooms.Length) break; // Prevent out of bounds
                
                Room room = Rooms[i];
                Vector2Int position = new Vector2Int(UnityEngine.Random.Range(0, DungeonWidth - room.Width), 
                                                      UnityEngine.Random.Range(0, DungeonHeight - room.Height));
                
                if (CanPlaceRoom(room, position, occupiedPositions))
                {
                    PlaceRoom(room, position, occupiedPositions);
                    Debug.Log($"Placed room {room} at {position}");
                }
                else
                {
                    Debug.LogWarning($"Could not place room {room} at {position}");
                }
            }
            
        }

        private void PlaceRoom(Room room, Vector2Int position, HashSet<Vector2Int> occupiedPositions)
        {
            for (int x = 0; x < room.Width; x++)
            {
                for (int y = 0; y < room.Height; y++)
                {
                    Vector2Int tilePosition = position + new Vector2Int(x, y);
                    occupiedPositions.Add(tilePosition);
                }
            }
            

            PlaceRoomTiles(room, position);
        }
        private void PlaceRoomTiles(Room room, Vector2Int position)
        {
            // This method should handle the actual placement of room tiles in your tilemap or grid system
            // For example, you could instantiate prefabs or set tiles in a Tilemap
            Debug.Log($"Placing room {room} at position {position}");
        }

        private bool CanPlaceRoom(Room room, Vector2Int position, HashSet<Vector2Int> occupiedPositions)
        {
            for (int x = 0; x < room.Width; x++)
            {
                for (int y = 0; y < room.Height; y++)
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

        private void ClearHallways()
        {
            
        }

        public void PlaceHallways()
        {
        }

    }
}