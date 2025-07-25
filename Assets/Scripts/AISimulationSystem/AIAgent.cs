using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AISimulationSystem
{
    public class AIAgent : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Range(1f, 10f)]
        public float moveSpeed = 2f;

        [Header("Visual Settings")]
        public float jumpPower = 1.2f;
        public int jumpCount = 1;

        [Header("AI Strategy")]
        [SerializeField] private IAIMovementStrategy movementStrategy;
        
        [Header("Debug")]
        public bool showGizmos = true;

        // Instance data for this specific AI agent
        private Vector2Int currentPosition;
        private Vector2Int targetPosition;
        private Vector2Int startPosition;
        private bool isMoving = false;
        private bool hasReachedGoal = false;
        private List<Vector2Int> visitedThisRun = new List<Vector2Int>();
        private List<Vector2Int> pathToDraw = new List<Vector2Int>();

        // Events
        public event Action<Vector2Int> OnTileLanded;
        public event Action OnGoalReached;
        public event Action<ExplorationStats> OnStatsUpdated;

        private void Start()
        {
            InitializeAgent();
        }

        private void InitializeAgent()
        {
            if (AIExplorationManager.Instance == null)
            {
                Debug.LogError("AIExplorationManager instance not found!");
                return;
            }

            startPosition = MapManager.Instance.GetStartPostion();
            Vector2Int goalPosition = MapManager.Instance.GetGoalPosition();

            AIExplorationManager.Instance.SetStartPosition(startPosition);
            AIExplorationManager.Instance.goalPosition = goalPosition;

            currentPosition = AIExplorationManager.Instance.GetNearestFloorTile(transform.position);
            transform.position = new Vector3(currentPosition.x, currentPosition.y, transform.position.z);

            Debug.Log($"AI Agent initialized at {currentPosition}, goal at {AIExplorationManager.Instance.goalPosition}");

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
                yield return new WaitForSeconds(0.5f / moveSpeed); // Adjust wait based on speed
                if (!isMoving)
                {
                    UpdateExploration();
                }
            }
        }

        private void UpdateExploration()
        {
            if (isMoving || hasReachedGoal) return;

            // Update vision - this will mark tiles as explored
            AIExplorationManager.Instance.UpdateVision(currentPosition);

            // --- Delegate decision to the strategy ---
            Vector2Int nextTarget = currentPosition; // Default to current if strategy fails
            if (movementStrategy != null)
            {
                nextTarget = movementStrategy.DecideNextMove(currentPosition, this);
            }
            else
            {
                // Fallback logic if no strategy (should ideally not happen)
                var frontier = AIExplorationManager.Instance.GetFrontierTiles();
                if (frontier.Count > 0)
                {
                     nextTarget = frontier[0]; // Simple fallback
                }
            }

            if (nextTarget != currentPosition)
            {
                MoveTo(nextTarget);
                pathToDraw.Add(nextTarget); // Add to path for visualization
            }
        }

        private void MoveTo(Vector2Int target)
        {
            targetPosition = target;
            isMoving = true;
            // Add to visited list for this run
            visitedThisRun.Add(target);
            StartCoroutine(MoveCoroutine());
        }

        private IEnumerator MoveCoroutine()
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
            // Determine movement type
            bool isHorizontalMove = Mathf.Abs(targetPosition.x - currentPosition.x) > 0 &&
                                    targetPosition.y == currentPosition.y;
            Tween moveTween;
            if (isHorizontalMove)
            {
                // Use jump for horizontal movement
                moveTween = transform.DOJump(endPos, jumpPower, jumpCount, 1f / moveSpeed);
            }
            else
            {
                // Regular smooth movement
                moveTween = transform.DOMove(endPos, 1f / moveSpeed);
            }
            moveTween.SetEase(Ease.InOutSine);
            // Wait for movement to complete
            yield return moveTween.WaitForCompletion();
            currentPosition = targetPosition;
            isMoving = false;
            // Trigger events
            OnTileLanded?.Invoke(currentPosition);
            // --- Notify strategy ---
            movementStrategy?.OnTileLanded(currentPosition, this);
            OnPlayerLandedOnTile(currentPosition);

            // Check if reached goal
            if (currentPosition == AIExplorationManager.Instance.goalPosition)
            {
                hasReachedGoal = true;
                OnGoalReached?.Invoke();
                Debug.Log($"AI Agent ({movementStrategy?.GetStrategyName()}) reached the goal! Visited {visitedThisRun.Count} tiles during exploration.");
            }
            // Update stats
            var stats = AIExplorationManager.Instance.GetExplorationMetrics();
            OnStatsUpdated?.Invoke(stats);
        }

        private void OnPlayerLandedOnTile(Vector2Int tilePosition)
        {
            Debug.Log($"AI Agent ({movementStrategy?.GetStrategyName()}) landed on tile: {tilePosition}");
            // Add custom behavior here if needed
        }

        public void ResetForNewRun()
        {
            hasReachedGoal = false;
            isMoving = false;
            visitedThisRun.Clear();
            pathToDraw.Clear();
            // Clear exploration data in the manager
            AIExplorationManager.Instance.ClearExplorationData();
            // Reset position to start
            currentPosition = startPosition;
            transform.position = new Vector3(currentPosition.x, currentPosition.y, transform.position.z);
            Debug.Log("AI Agent reset for new exploration run");
        }

        public void SetStartPosition(Vector2Int newStartPos)
        {
            startPosition = newStartPos;
            currentPosition = newStartPos;
            transform.position = new Vector3(newStartPos.x, newStartPos.y, transform.position.z);
            // Update the exploration manager with new start position
            if (AIExplorationManager.Instance != null)
            {
                AIExplorationManager.Instance.SetStartPosition(newStartPos);
            }
        }

        public void StopExploration()
        {
            StopAllCoroutines();
            isMoving = false;
        }

        // Public getters for external systems and strategies
        public Vector2Int GetCurrentPosition() => currentPosition;
        public Vector2Int GetStartPosition() => startPosition;
        public bool IsMoving() => isMoving;
        public bool HasReachedGoal() => hasReachedGoal;
        public List<Vector2Int> GetVisitedTiles() => new List<Vector2Int>(visitedThisRun);
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

        #region Gizmo Visualization
        private void OnDrawGizmos()
        {
            if (!showGizmos || !Application.isPlaying) return;
            if (AIExplorationManager.Instance == null) return;
            // Draw start position
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(new Vector3(startPosition.x, startPosition.y, 0), 0.8f);
            // Draw current position
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.6f);
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
            // Draw visited tiles (darker)
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