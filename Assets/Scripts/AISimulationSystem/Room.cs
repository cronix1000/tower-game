using System.Collections.Generic;
using AISimulationSystem;
using UnityEngine;

public class Room
{
    public int width;
    public int height;
    public Vector2Int center;
    public HashSet<Vector2Int> floorPositions;
        
    public bool isEmpty = true;
    public bool isStartRoom = false;
    public bool isExitRoom = false;
    public ChallengeCard assignedChallenge = null;
        
    public Room(int width, int height, Vector2Int center)
    {
        this.width = width;
        this.height = height;
        this.center = center;
        this.floorPositions = new HashSet<Vector2Int>();
        this.isEmpty = true;
        this.isStartRoom = false;
        this.isExitRoom = false;
        this.assignedChallenge = null;
    }
}