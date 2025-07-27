// BasicExplorationStrategy.cs
using UnityEngine;
using AISimulationSystem;
using System.Collections.Generic;

public class BasicExplorationStrategy : StrategyBase
{
    private Vector2Int lastDirection = Vector2Int.zero;
    private Vector2Int preferredDirection = Vector2Int.zero;
    
    public override Vector2Int DecideNextMove(Vector2Int currentPosition, AIAgent agent)
    {
        // Try to continue in the same direction if possible (natural movement)
        if (preferredDirection != Vector2Int.zero)
        {
            Vector2Int continueStraight = currentPosition + preferredDirection;
            if (mapManager.IsWalkable(continueStraight) && !mapManager.GetTileData(continueStraight).isVisited)
            {
                return continueStraight;
            }
        }

        // Look for unvisited adjacent tiles (immediate exploration)
        List<Vector2Int> validMoves = GetValidAdjacentMoves(currentPosition);
        if (validMoves.Count > 0)
        {
            // Pick a random valid move for more natural exploration
            Vector2Int chosenMove = validMoves[UnityEngine.Random.Range(0, validMoves.Count)];
            preferredDirection = chosenMove - currentPosition; // Remember this direction
            return chosenMove;
        }

        // If no immediate moves, go to closest frontier tile
        List<Vector2Int> frontier = agent.GetFrontierTiles();
        if (frontier.Count > 0)
        {
            Vector2Int closestFrontier = FindClosestFrontierTile(currentPosition, frontier);
            Vector2Int directionToFrontier = GetDirectionToward(currentPosition, closestFrontier);
            Vector2Int nextStep = currentPosition + directionToFrontier;
            
            if (mapManager.IsWalkable(nextStep))
            {
                preferredDirection = directionToFrontier; // Update preferred direction
                return nextStep;
            }
        }

        // No moves available, reset preferred direction
        preferredDirection = Vector2Int.zero;
        return currentPosition;
    }

    protected List<Vector2Int> GetValidAdjacentMoves(Vector2Int pos)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = pos + dir;
            if (mapManager.IsWalkable(newPos) && !mapManager.GetTileData(newPos).isVisited)
            {
                moves.Add(newPos);
            }
        }
        return moves;
    }

    private Vector2Int FindClosestFrontierTile(Vector2Int currentPosition, List<Vector2Int> frontierTiles)
    {
        Vector2Int closest = frontierTiles[0];
        float minDistance = Vector2Int.Distance(currentPosition, closest);

        foreach (Vector2Int frontier in frontierTiles)
        {
            float distance = Vector2Int.Distance(currentPosition, frontier);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = frontier;
            }
        }
        return closest;
    }

    private Vector2Int GetDirectionToward(Vector2Int from, Vector2Int to)
    {
        Vector2Int difference = to - from;
        
        // Prefer moving in the axis with the larger difference
        if (Mathf.Abs(difference.x) > Mathf.Abs(difference.y))
        {
            return new Vector2Int(difference.x > 0 ? 1 : -1, 0);
        }
        else if (difference.y != 0)
        {
            return new Vector2Int(0, difference.y > 0 ? 1 : -1);
        }
        
        return Vector2Int.zero;
    }

    public override string GetStrategyName()
    {
        return "Basic";
    }
}