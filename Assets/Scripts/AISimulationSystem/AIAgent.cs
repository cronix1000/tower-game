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
        [Header("Components")]
        [SerializeField] private ActorMover actorMover;

        [Header("AI Strategy")]
        [SerializeField] private IAIMovementStrategy movementStrategy;
        
        [Header("Debug")]
        public bool showGizmos = true;

        // Instance data for this specific AI agent
        private Vector2Int startPosition;
        private bool hasReachedGoal = false;
        private List<Vector2Int> visitedThisRun = new List<Vector2Int>();
        private List<Vector2Int> pathToDraw = new List<Vector2Int>();

        // Events
        public event Action<Vector2Int> OnTileLanded;
        public event Action OnGoalReached;
        public event Action<ExplorationStats> OnStatsUpdated;

        private void Start()
        {
            // Get or add ActorMover component
            if (actorMover == null)
            {
                actorMover = GetComponent<ActorMover>();
                if (actorMover == null)
                {
                    actorMover = gameObject.AddComponent<ActorMover>();
                }
            }

            // Subscribe to movement events
            actorMover.OnMovementComplete += OnMovementComplete;

            InitializeAgent();
        }

        private void OnDestroy()
        {
            if (actorMover != null)
            {
                actorMover.OnMovementComplete -= OnMovementComplete;
            }
        }

        private void PlayerLandedOnTile(Vector2Int position)
        {
            OnTileLanded?.Invoke(position);

            // check map manager for the tile type
            GameObject tile = MapManager.Instance.GetGameObjectOnTile(position);

            visitedThisRun.Add(position);
            pathToDraw.Add(position);
        }
    }
}