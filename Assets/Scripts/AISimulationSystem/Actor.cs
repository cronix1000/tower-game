using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.Vibility;

namespace AISimulationSystem
{
    public class Actor : MonoBehaviour
    {
        [Header("Components")]
        public ActorMover actorMover;
        public Visibility visibilityCalculator;

        [Header("Debug")]
        public bool showGizmos = true;

        protected Vector2Int startPosition;
        protected List<Vector2Int> visitedTiles = new List<Vector2Int>();
        protected List<Vector2Int> visibleTiles = new List<Vector2Int>();

        // Events
        public event Action<Vector2Int> OnTileLanded;

        protected virtual void Start()
        {

            InitializeComponents();
            InitializeActor();
        }

        protected virtual void OnDestroy()
        {
            if (actorMover != null)
            {
                actorMover.OnMovementComplete -= OnActorMovementComplete;
            }
        }

        protected virtual void InitializeComponents()
        {
            if (actorMover == null)
            {
                actorMover = GetComponent<ActorMover>();
                if (actorMover == null)
                {
                    actorMover = gameObject.AddComponent<ActorMover>();
                }
            }

            if (visibilityCalculator == null)
            {
                visibilityCalculator = new AdamMilVisibility();
            }

            // Subscribe to movement events
            actorMover.OnMovementComplete += OnActorMovementComplete;
        }

        protected virtual void InitializeActor()
        {
            UpdateVisibility();
        }

        protected virtual void OnActorMovementComplete(Vector2Int position)
        {
            visitedTiles.Add(position);
            UpdateVisibility();
            OnTileLanded?.Invoke(position);
            
            HandleTileInteraction(position);
        }

        public virtual void HandleTileInteraction(Vector2Int position)
        {
            GameObject tile = MapManager.Instance?.GetGameObjectOnTile(position);
            Interactable interactable = tile?.GetComponent<Interactable>();
            if(interactable != null)
            {
                interactable.Interact(this);
            }
        }

        protected virtual void UpdateVisibility()
        {
            if (visibilityCalculator != null && actorMover != null)
            {
                visibleTiles.Clear();
                Vector3Int origin = new Vector3Int(actorMover.CurrentPosition.x, actorMover.CurrentPosition.y, 0);
                List<Vector3Int> visibleTiles3D = new List<Vector3Int>();
                
                // Use a reasonable vision range (e.g., 10 tiles) instead of unlimited
                int visionRange = MapManager.Instance != null ? (int)MapManager.Instance.visionRange : 10;
                visibilityCalculator.Compute(origin, visionRange, visibleTiles3D);
                
                // Convert Vector3Int to Vector2Int
                foreach (Vector3Int tile3D in visibleTiles3D)
                {
                    visibleTiles.Add(new Vector2Int(tile3D.x, tile3D.y));
                }
            }
        }

        // Public getters for accessing actor state
        public Vector2Int GetCurrentPosition() => actorMover?.CurrentPosition ?? Vector2Int.zero;
        public Vector2Int GetStartPosition() => startPosition;
        public bool IsMoving() => actorMover?.IsMoving ?? false;
        public List<Vector2Int> GetVisitedTiles() => new List<Vector2Int>(visitedTiles);
        public List<Vector2Int> GetVisibleTiles() => new List<Vector2Int>(visibleTiles);

        // Movement methods
        public virtual void MoveTo(Vector2Int target)
        {
            if (actorMover != null && actorMover.CanMoveTo(target))
            {
                actorMover.MoveTo(target);
            }
        }

        public virtual void SetPosition(Vector2Int position)
        {
            startPosition = position;
            if (actorMover != null)
            {
                actorMover.SetPosition(position);
            }
            UpdateVisibility();
        }

        public virtual void StopMovement()
        {
            actorMover?.StopMovement();
        }
    }
}
