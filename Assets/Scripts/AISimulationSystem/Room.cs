using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Dungeon
{
    [Serializable]
    public class Room
    {
        public int width;
        public int height;
        public HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();

        public Vector2Int Position;

        public Room(int width, int height, Vector2Int position)
        {
            this.width = width;
            this.height = height;
            Position = position;
        }

        public bool Contains(Vector2Int point)
        {
            return point.x >= Position.x && point.x < Position.x + width &&
                   point.y >= Position.y && point.y < Position.y + height;
        }
        
        public bool Contains(Room other)
        {
            return other.Position.x >= Position.x && 
                   other.Position.x + other.width <= Position.x + width &&
                   other.Position.y >= Position.y && 
                   other.Position.y + other.height <= Position.y + height;
        }
        
        
    
        
    
        
    }
}