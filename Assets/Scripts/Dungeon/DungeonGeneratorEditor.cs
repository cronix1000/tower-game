using UnityEditor;
using UnityEngine;

namespace Dungeon
{
    [CustomEditor(typeof(RoomFirstDungeonGenerator))]
    public class RoomFirstDungeonGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            RoomFirstDungeonGenerator generator = (RoomFirstDungeonGenerator)target;

            if (GUILayout.Button("Generate Dungeon", GUILayout.Height(30)))
            {
                generator.GenerateDungeon();
            }
        }
    }
}