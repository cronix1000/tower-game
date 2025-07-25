// StrategyBase.cs

using UnityEngine;

namespace AISimulationSystem
{
    // Assuming this is the namespace for AIAgent/Manager

    public abstract class StrategyBase : IAIMovementStrategy
    {
        protected AIAgent agent;
        protected AIExplorationManager explorationManager;
        protected MapManager mapManager;

        public virtual void Initialize(AIAgent agent)
        {
            this.agent = agent;
            this.explorationManager = AIExplorationManager.Instance;
            this.mapManager = MapManager.Instance;
        }

        public abstract Vector2Int DecideNextMove(Vector2Int currentPosition, AIAgent agent);
        public virtual void OnTileLanded(Vector2Int tilePosition, AIAgent agent) { /* Default empty */ }
        public abstract string GetStrategyName();
    }
    public interface IAIMovementStrategy
    {
        Vector2Int DecideNextMove(Vector2Int currentPosition, AIAgent agent);
        void OnTileLanded(Vector2Int tilePosition, AIAgent agent); // Optional callback
        void Initialize(AIAgent agent); // Optional initialization
        string GetStrategyName(); // For debugging/stats
    }
}