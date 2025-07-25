using UnityEngine;

namespace AISimulationSystem
{
    public class AIVisual : MonoBehaviour
    {
        [SerializeField] private Animator Animator;
        [SerializeField] private SpriteRenderer SpriteRenderer;

        public enum AIVisualActions
        {
            Move,
            Attack,
            Hurt,
            Trap,
            Loot,
            Finish
        }
        
        public void SetAction(AIVisualActions action)
        {
            switch (action)
            {
                case AIVisualActions.Move:
                    Animator.SetTrigger("Move");
                    break;
                case AIVisualActions.Attack:
                    Animator.SetTrigger("Attack");
                    break;
                case AIVisualActions.Hurt:
                    Animator.SetTrigger("Hurt");
                    break;
                case AIVisualActions.Trap:
                    Animator.SetTrigger("Trap");
                    break;
                case AIVisualActions.Loot:
                    Animator.SetTrigger("Loot");
                    break;
                case AIVisualActions.Finish:
                    Animator.SetTrigger("Finish");
                    break;
            }
        }
        
        
        
    }
}