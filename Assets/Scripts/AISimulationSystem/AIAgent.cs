using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Dungeon;
using UnityEngine;
using UnityEngine.Tilemaps;
using Utils.Vibility;

namespace AISimulationSystem
{
    public class AIAgent : Actor
    {
        [Header("AI Strategy")]
        [SerializeField] private IAIMovementStrategy movementStrategy;

        // AI-specific state
        private bool hasReachedGoal = false;
        private List<Vector2Int> visitedThisRun = new List<Vector2Int>();
        private List<Vector2Int> pathToDraw = new List<Vector2Int>();
        private List<Vector2Int> frontierTiles = new List<Vector2Int>();
        public bool isDead;
        public Room currentRoom;

        // AI-specific events
        public event Action OnGoalReached;

        protected override void Start()
        {
            base.Start(); 
            Initialize();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

public void Initialize()       {
            if (MapManager.Instance == null)
            {
                Debug.LogError("MapManager instance not found!");
                return;
            }
            // Set the starting position
            Vector2Int startPosition = MapManager.Instance.GetStartPostion();
            SetPosition(startPosition);
            Vector2Int goalPosition = MapManager.Instance.GetGoalPosition();
            Debug.Log($"AI Agent initialized at {GetCurrentPosition()}, goal at {goalPosition}");

            // Mark starting position as visited
            MapManager.Instance.MarkTileAsVisited(GetCurrentPosition());
            
            // Initialize frontier tiles
            SetFrontierTiles();

            // Initialize movement strategy
            if (movementStrategy != null)
            {
                movementStrategy.Initialize(this);
            }
            else
            {
                movementStrategy = new BasicExplorationStrategy();
                movementStrategy.Initialize(this);
                Debug.LogWarning("No AI Movement Strategy assigned. Using BasicExplorationStrategy.");
            }

            StartExploration();
        }
        
        public void SetFrontierTiles()
        {
            frontierTiles.Clear();
            
            // Get all adjacent tiles to visited tiles that are walkable but not yet visited
            HashSet<Vector2Int> allVisited = new HashSet<Vector2Int>(visitedTiles);
            allVisited.UnionWith(visitedThisRun);
            
            foreach (var visitedTile in allVisited)
            {
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                
                foreach (Vector2Int direction in directions)
                {
                    Vector2Int adjacentTile = visitedTile + direction;
                    
                    if (MapManager.Instance.IsWalkable(adjacentTile) && 
                        !allVisited.Contains(adjacentTile) && 
                        !frontierTiles.Contains(adjacentTile))
                    {
                        frontierTiles.Add(adjacentTile);
                    }
                }
            }
        }

        public List<Vector2Int> GetFrontierTiles()
        {
            return new List<Vector2Int>(frontierTiles);
        }

        protected override void OnActorMovementComplete(Vector2Int position)
        {
            base.OnActorMovementComplete(position); 

            visitedThisRun.Add(position);
            pathToDraw.Add(position);

            // Mark tile as visited in MapManager
            MapManager.Instance.MarkTileAsVisited(position);
            
            // Update frontier tiles based on current visibility
            SetFrontierTiles();

            movementStrategy?.OnTileLanded(position, this);

            if (MapManager.Instance.IsOnGoalTile(position))
            {
                hasReachedGoal = true;
                OnGoalReached?.Invoke();
                Debug.Log($"AI Agent ({movementStrategy?.GetStrategyName()}) reached the goal! Visited {visitedThisRun.Count} tiles during exploration.");
            }

            // // Update stats
            // var stats = AIExplorationManager.Instance.GetExplorationMetrics();
            // OnStatsUpdated?.Invoke(stats);
        }

        public void SetMovementStrategy(IAIMovementStrategy strategy)
        {
            this.movementStrategy = strategy;
            if (strategy != null)
            {
                strategy.Initialize(this);
            }
        }

        public void StartExploration()
        {
            if (hasReachedGoal) return;
            ResetForNewRun();
            StartCoroutine(ExplorationLoop());
        }

        private IEnumerator ExplorationLoop()
        {
            while (!hasReachedGoal && MapManager.Instance)
            {
                yield return new WaitForSeconds(0.1f / actorMover.moveSpeed);
                if (!IsMoving())
                {
                    UpdateExploration();
                }
            }
        }

        private void UpdateExploration()
        {
            if (IsMoving() || hasReachedGoal) return;

            Vector2Int currentPosition = GetCurrentPosition();
            
            // Delegate decision to the strategy
            Vector2Int nextTarget = currentPosition;
            if (movementStrategy != null)
            {
                nextTarget = movementStrategy.DecideNextMove(currentPosition, this);
            }
            else
            {
                // Fallback logic if no strategy - try random valid moves
                var directions = Dungeon.Direction2D.cardinalDirectionsList;
                List<Vector2Int> validDirections = new List<Vector2Int>();
                
                foreach (Vector2Int direction in directions)
                {
                    Vector2Int testTarget = currentPosition + direction;
                    if (MapManager.Instance.IsWalkable(testTarget))
                    {
                        validDirections.Add(testTarget);
                    }
                }
                
                if (validDirections.Count > 0)
                {
                    nextTarget = validDirections[UnityEngine.Random.Range(0, validDirections.Count)];
                }
            }

            // Only move if the target is different and walkable
            if (nextTarget != currentPosition && MapManager.Instance.IsWalkable(nextTarget))
            {
                MoveTo(nextTarget);
            }
        }

        public void ResetForNewRun()
        {
            hasReachedGoal = false;
            visitedThisRun.Clear();
            pathToDraw.Clear();
            
            // Clear exploration data in the manager
            MapManager.Instance.ClearExplorationData();
            
            // Reset position to start
            Vector2Int startPos = MapManager.Instance.GetStartPostion();
            SetPosition(startPos);
            
            // Mark starting position as visited
            MapManager.Instance.MarkTileAsVisited(startPos);
            
            // Update frontier tiles from the starting position
            SetFrontierTiles();
            
            Debug.Log("AI Agent reset for new exploration run");
        }

        public void StopExploration()
        {
            StopAllCoroutines();
            StopMovement();
        }

        // AI-specific getters
        public bool HasReachedGoal() => hasReachedGoal;
        public List<Vector2Int> GetVisitedThisRun() => new List<Vector2Int>(visitedThisRun);

    }
}