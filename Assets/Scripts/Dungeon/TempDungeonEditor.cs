using UnityEngine;

namespace Dungeon
{
    public class TempDungeonEditor : MonoBehaviour
    {
        public DungeonGenerator dungeonGenerator; // Reference to the DungeonGenerator script
        public DungeonTiler dungeonTiler; // Reference to the DungeonTiler script

        private void Start()
        {
            if (dungeonGenerator == null || dungeonTiler == null)
            {
                Debug.LogError("DungeonGenerator or DungeonTiler reference is missing.");
                return;
            }
        }
        
    }
}