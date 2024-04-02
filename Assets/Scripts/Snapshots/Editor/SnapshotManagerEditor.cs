using UnityEditor;
using UnityEngine;

namespace Snapshots.Editor
{
    [CustomEditor(typeof(SnapshotManager))]
    public class SnapshotManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawDefaultInspector();

            if (GUILayout.Button("Toggle Snapshots Attached"))
            {
                var sm = (SnapshotManager)serializedObject.targetObject;
                sm.ToggleSnapshotsAttached();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}