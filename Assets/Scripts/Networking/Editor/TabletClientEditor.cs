using Networking.Tablet;
using UnityEditor;
using UnityEngine;

namespace Networking.Editor
{
    [CustomEditor(typeof(TabletClient))]
    public class TabletClientEditor : UnityEditor.Editor
    {
        public override async void OnInspectorGUI()
        {
            var client = (TabletClient)target;

            DrawDefaultInspector();

            if (GUILayout.Button("Send Menu: Analysis"))
            {
                await client.SendMenuChangedMessage(MenuMode.Analysis);
            }

            if (GUILayout.Button("Send Menu: Selection"))
            {
                await client.SendMenuChangedMessage(MenuMode.Selection);
            }

            if (GUILayout.Button("Send Menu: Mapping"))
            {
                await client.SendMenuChangedMessage(MenuMode.Mapping);
            }

            if (GUILayout.Button("Send Swipe"))
            {
                await client.SendSwipeMessage(true, 250, 250, 0);
            }

            if (GUILayout.Button("Send Shake"))
            {
                await client.SendShakeMessage(3);
            }

            if (GUILayout.Button("Send Double Tap"))
            {
                await client.SendTapMessage(TapType.Double, 250, 250);
            }
        }
    }
}