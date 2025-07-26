using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace AISimulationSystem
{
    public class ActorMover : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Range(1f, 10f)]
        public float moveSpeed = 2f;

        [Header("Visual Settings")]
        public float jumpPower = 1.2f;
        public int jumpCount = 1;

        // Movement state
        private bool isMoving = false;
        private Vector2Int currentPosition;
        private Vector2Int targetPosition;

        // Events
        public event Action<Vector2Int> OnMovementComplete;
        public event Action OnMovementStarted;

        public bool IsMoving => isMoving;
        public Vector2Int CurrentPosition => currentPosition;

        /// <summary>
        /// Initialize the mover with a starting position
        /// </summary>
        public void Initialize(Vector2Int startPosition)
        {
            currentPosition = startPosition;
            transform.position = new Vector3(startPosition.x, startPosition.y, transform.position.z);
        }

        /// <summary>
        /// Set the current position without animation (useful for teleporting)
        /// </summary>
        public void SetPosition(Vector2Int position)
        {
            currentPosition = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        /// <summary>
        /// Move to a target position with animation
        /// </summary>
        public void MoveTo(Vector2Int target, Action<Vector2Int> onComplete = null)
        {
            if (isMoving)
            {
                Debug.LogWarning("Already moving! Ignoring move request.");
                return;
            }

            targetPosition = target;
            StartCoroutine(MoveCoroutine(onComplete));
        }

        /// <summary>
        /// Stop any current movement
        /// </summary>
        public void StopMovement()
        {
            StopAllCoroutines();
            isMoving = false;
        }

        private IEnumerator MoveCoroutine(Action<Vector2Int> onComplete = null)
        {
            isMoving = true;
            OnMovementStarted?.Invoke();

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
            
            // Update current position
            currentPosition = targetPosition;
            isMoving = false;
            
            // Trigger events
            OnMovementComplete?.Invoke(currentPosition);
            onComplete?.Invoke(currentPosition);
        }

        /// <summary>
        /// Get the distance to a target position
        /// </summary>
        public float GetDistanceTo(Vector2Int target)
        {
            return Vector2Int.Distance(currentPosition, target);
        }

        /// <summary>
        /// Check if the mover can move to a target position (basic validation)
        /// </summary>
        public bool CanMoveTo(Vector2Int target)
        {
            // Basic check - could be extended with pathfinding or obstacle detection
            return !isMoving && target != currentPosition;
        }
    }
}
