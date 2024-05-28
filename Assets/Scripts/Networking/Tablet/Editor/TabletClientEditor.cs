using UnityEditor;
using UnityEngine;

namespace Networking.Tablet.Editor
{
    [CustomEditor(typeof(TabletClient))]
    public class TabletClientEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var client = (TabletClient)target;

            DrawDefaultInspector();

            if (GUILayout.Button("Connect"))
            {
                _ = client.Connect();
            }
            
            if (GUILayout.Button("Send Menu: Analysis"))
            {
                _ = client.SendMenuChangedMessage(MenuMode.Analysis);
            }

            if (GUILayout.Button("Send Menu: Selection"))
            {
                _ = client.SendMenuChangedMessage(MenuMode.Selection);
            }

            if (GUILayout.Button("Send Swipe"))
            {
                _ = client.SendSwipeMessage(true, 250, 250, 0);
            }

            if (GUILayout.Button("Send Shake"))
            {
                _ = client.SendShakeMessage(3);
            }

            if (GUILayout.Button("Send Double Tap"))
            {
                _ = client.SendTapMessage(TapType.Double, 250, 250);
            }
        }
    }
}