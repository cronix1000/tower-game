using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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

        // AI-specific events
        public event Action OnGoalReached;

        protected override void Start()
        {
            base.Start(); // Initialize Actor components
            InitializeAI();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // Any AI-specific cleanup can go here
        }

        private void InitializeAI()
        {
            if (AIExplorationManager.Instance == null)
            {
                Debug.LogError("AIExplorationManager instance not found!");
                return;
            }

            Vector2Int goalPosition = MapManager.Instance.GetGoalPosition();
            AIExplorationManager.Instance.SetStartPosition(startPosition);
            AIExplorationManager.Instance.goalPosition = goalPosition;

            Debug.Log($"AI Agent initialized at {GetCurrentPosition()}, goal at {AIExplorationManager.Instance.goalPosition}");

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

        protected override void OnActorMovementComplete(Vector2Int position)
        {
            base.OnActorMovementComplete(position); // Handle base actor behavior
            
            // AI-specific movement completion logic
            visitedThisRun.Add(position);
            pathToDraw.Add(position);
            
            // Notify strategy
            movementStrategy?.OnTileLanded(position, this);

            // Check if reached goal
            if (position == AIExplorationManager.Instance.goalPosition)
            {
                hasReachedGoal = true;
                OnGoalReached?.Invoke();
                Debug.Log($"AI Agent ({movementStrategy?.GetStrategyName()}) reached the goal! Visited {visitedThisRun.Count} tiles during exploration.");
            }

            // Update stats
            var stats = AIExplorationManager.Instance.GetExplorationMetrics();
            OnStatsUpdated?.Invoke(stats);
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
            while (!hasReachedGoal && AIExplorationManager.Instance != null)
            {
                yield return new WaitForSeconds(0.5f / actorMover.moveSpeed);
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

            // Update vision - this will mark tiles as explored
            AIExplorationManager.Instance.UpdateVision(currentPosition);

            // Delegate decision to the strategy
            Vector2Int nextTarget = currentPosition;
            if (movementStrategy != null)
            {
                nextTarget = movementStrategy.DecideNextMove(currentPosition, this);
            }
            else
            {
                // Fallback logic if no strategy
                var frontier = AIExplorationManager.Instance.GetFrontierTiles();
                if (frontier.Count > 0)
                {
                    nextTarget = frontier[0];
                }
            }

            if (nextTarget != currentPosition)
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
            AIExplorationManager.Instance.ClearExplorationData();
            
            // Reset position to start
            SetPosition(startPosition);
            
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
        public ExplorationStats GetCurrentStats() => AIExplorationManager.Instance.GetExplorationMetrics();
        public float GetExplorationPercentage()
        {
            var stats = GetCurrentStats();
            return stats.explorationPercentage * 100f;
        }
        public int GetExploredTileCount()
        {
            var stats = GetCurrentStats();
            return stats.exploredTiles;
        }
        public List<Vector2Int> GetFrontierTiles()
        {
            return AIExplorationManager.Instance?.GetFrontierTiles() ?? new List<Vector2Int>();
        }
        public float GetCurrentExplorationRadius()
        {
            var stats = GetCurrentStats();
            return stats.explorationRadius;
        }

        #region AI-Specific Gizmo Visualization
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos(); // Draw base actor gizmos

            if (!showGizmos || !Application.isPlaying) return;
            if (AIExplorationManager.Instance == null) return;

            Vector2Int currentPos = GetCurrentPosition();

            // Draw explored tiles
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Transparent cyan
            foreach (Vector2Int tile in AIExplorationManager.Instance.floorPositionsSet)
            {
                var tileData = AIExplorationManager.Instance.GetTileData(tile);
                if (tileData != null && tileData.isExplored)
                {
                    Gizmos.DrawCube(new Vector3(tile.x, tile.y, 0), Vector3.one * 0.7f);
                }
            }

            // Draw visited tiles this run (darker)
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f); // Orange
            foreach (Vector2Int tile in AIExplorationManager.Instance.floorPositionsSet)
            {
                var tileData = AIExplorationManager.Instance.GetTileData(tile);
                if (tileData != null && tileData.isVisited)
                {
                    Gizmos.DrawCube(new Vector3(tile.x, tile.y, 0), Vector3.one * 0.5f);
                }
            }

            // Draw vision range
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f); // Transparent yellow
            Gizmos.DrawSphere(transform.position, AIExplorationManager.Instance.visionRange);

            // Draw path taken this run
            if (pathToDraw != null && pathToDraw.Count > 1)
            {
                Gizmos.color = Color.magenta;
                for (int i = 0; i < pathToDraw.Count - 1; i++)
                {
                    Vector3 start = new Vector3(pathToDraw[i].x, pathToDraw[i].y, 0);
                    Vector3 end = new Vector3(pathToDraw[i + 1].x, pathToDraw[i + 1].y, 0);
                    Gizmos.DrawLine(start, end);
                }
            }

            // Draw exploration radius circle
            var stats = GetCurrentStats();
            if (stats.explorationRadius > 0)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
                Gizmos.DrawWireSphere(new Vector3(startPosition.x, startPosition.y, 0), stats.explorationRadius);
            }

            // Draw frontier tiles
            Gizmos.color = Color.cyan;
            var frontierTiles = GetFrontierTiles();
            foreach (Vector2Int tile in frontierTiles)
            {
                Gizmos.DrawWireCube(new Vector3(tile.x, tile.y, 0), Vector3.one * 0.9f);
            }
        }
        #endregion
    }
}