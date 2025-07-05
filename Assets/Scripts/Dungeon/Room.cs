using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dungeon
{
    [Serializable]
    public class Room
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Vector2Int Position { get; private set; }
        public int[,] Layout { get; private set; }

        public Room(int width, int height, Vector2Int position)
        {
            Width = width;
            Height = height;
            Position = position;
        }

        public bool Contains(Vector2Int point)
        {
            return point.x >= Position.x && point.x < Position.x + Width &&
                   point.y >= Position.y && point.y < Position.y + Height;
        }
        
        public bool Contains(Room other)
        {
            return other.Position.x >= Position.x && 
                   other.Position.x + other.Width <= Position.x + Width &&
                   other.Position.y >= Position.y && 
                   other.Position.y + other.Height <= Position.y + Height;
        }
    
        
    
        
    }
}