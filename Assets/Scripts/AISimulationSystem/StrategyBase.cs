// StrategyBase.cs

using UnityEngine;

namespace AISimulationSystem
{
    public abstract class StrategyBase : IAIMovementStrategy
    {
        protected AIAgent agent;
        protected MapManager mapManager;

        public virtual void Initialize(AIAgent agent)
        {
            this.agent = agent;
            this.mapManager = MapManager.Instance;
        }

        public abstract Vector2Int DecideNextMove(Vector2Int currentPosition, AIAgent agent);
        public virtual void OnTileLanded(Vector2Int tilePosition, AIAgent agent) {  }
        public abstract string GetStrategyName();
    }
    public interface IAIMovementStrategy
    {
        Vector2Int DecideNextMove(Vector2Int currentPosition, AIAgent agent);
        void OnTileLanded(Vector2Int tilePosition, AIAgent agent); 
        void Initialize(AIAgent agent); 
        string GetStrategyName();
    }
}