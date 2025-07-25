// BasicExplorationStrategy.cs
using UnityEngine;
using AISimulationSystem;
using System.Collections.Generic;

public class BasicExplorationStrategy : StrategyBase
{
    public override Vector2Int DecideNextMove(Vector2Int currentPosition, AIAgent agent)
    {
        // Use the manager's frontier selection as default
        List<Vector2Int> frontier = explorationManager.GetFrontierTiles();
        if (frontier.Count > 0)
        {
            // Simple: Move to the first frontier tile
            // You could implement pathfinding (A*) here for better results
            return frontier[0];
        }

        // If no frontier, just move to an adjacent unvisited tile
        List<Vector2Int> validMoves = GetValidAdjacentMoves(currentPosition);
        if (validMoves.Count > 0)
        {
            return validMoves[0]; // Simplest choice
        }

        // No valid moves, stay put
        return currentPosition;
    }

    protected List<Vector2Int> GetValidAdjacentMoves(Vector2Int pos)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = pos + dir;
            if (explorationManager.IsWalkable(newPos) && !explorationManager.GetTileData(newPos).isVisited)
            {
                moves.Add(newPos);
            }
        }
        return moves;
    }

    public override string GetStrategyName()
    {
        return "Basic";
    }
}