using UnityEditor;
using UnityEngine;

namespace Dungeon
{
    [CustomEditor(typeof(DungeonGenerator))]
    public class DungeonGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DungeonGenerator generator = (DungeonGenerator)target;

            if (GUILayout.Button("Generate Dungeon", GUILayout.Height(30)))
            {
                generator.PlaceRooms();
            }
            
            if(GUILayout.Button("Generate Hallways", GUILayout.Height(30)))
            {
                generator.PlaceHallways();
            }

            if (GUILayout.Button("Clear Dungeon", GUILayout.Height(30)))
            {
                generator.ClearHallways();
            }
        }
    }
}