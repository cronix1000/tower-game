using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

namespace Dungeon
{
    public class Floor
    {
        public Dictionary<Vector2Int, Room> Rooms;
        public int DungeonWidth { get; private set; }
        public int DungeonHeight { get; private set; }
        public int RoomCount = 5;
        public HashSet<Vector2Int> FloorTiles;
    }
}