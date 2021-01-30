using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(IslandGenerator))]
public class IslandGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Generate Tilemaps"))
        {
            IslandGenerator islandGenerator = (IslandGenerator)target;
            islandGenerator.Generated = false;
            islandGenerator.Generate();
        }
    }
}