using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Networking.Tablet.Editor
{
    [CustomEditor(typeof(TabletServer))]
    public class TabletServerEditor : UnityEditor.Editor
    {
        private MethodInfo _modeMethod;
        private MethodInfo _tapMethod;
        private MethodInfo _swipeMethod;
        private MethodInfo _tiltMethod;
        private MethodInfo _shakeMethod;

        private MethodInfo onSelectionMethod;
        private MethodInfo onSlicingMethod;
        private MethodInfo onSelect;
        private MethodInfo onDeselect;

        private void Awake()
        {
            _modeMethod = typeof(TabletServer).GetMethod("HandleModeChange", BindingFlags.NonPublic | BindingFlags.Instance);
            _tapMethod = typeof(TabletServer).GetMethod("HandleTap", BindingFlags.NonPublic | BindingFlags.Instance);
            _swipeMethod = typeof(TabletServer).GetMethod("HandleSwipe", BindingFlags.NonPublic | BindingFlags.Instance);
            _tiltMethod = typeof(TabletServer).GetMethod("HandleTilt", BindingFlags.NonPublic | BindingFlags.Instance);
            _shakeMethod = typeof(TabletServer).GetMethod("HandleShakes", BindingFlags.NonPublic | BindingFlags.Instance);

            // new commands

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawDefaultInspector();

            GUILayout.Label("Modes");
            if (GUILayout.Button("None"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _modeMethod.Invoke(host, new object[] { MenuMode.None });
            }
            
            if (GUILayout.Button("Analysis Mode"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _modeMethod.Invoke(host, new object[] { MenuMode.None });
                _modeMethod.Invoke(host, new object[] { MenuMode.Analysis });
            }

            if (GUILayout.Button("Selection Mode"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _modeMethod.Invoke(host, new object[] { MenuMode.None });
                _modeMethod.Invoke(host, new object[] { MenuMode.Selection });
            }

            GUILayout.Label("Tap");
            if (GUILayout.Button("Double Tap"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _tapMethod.Invoke(host, new object[] { TapType.Double, 250, 250 });
            }

            if (GUILayout.Button("Hold Start"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _tapMethod.Invoke(host, new object[] { TapType.HoldBegin, 250, 250 });
            }
            
            if (GUILayout.Button("Hold End"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _tapMethod.Invoke(host, new object[] { TapType.HoldEnd, 250, 250 });
            }

            GUILayout.Label("Swipe");
            if (GUILayout.Button("Left"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _swipeMethod.Invoke(host, new object[] { false, 0, 150, 180 });
            }

            if (GUILayout.Button("Right"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _swipeMethod.Invoke(host, new object[] { false, 500, 150, 0});
            }
            
            if (GUILayout.Button("Up"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _swipeMethod.Invoke(host, new object[] { false, 250, 300, 90});
            }

            GUILayout.Label("Tilt");
            if (GUILayout.Button("Left"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _tiltMethod.Invoke(host, new object[] { true });
            }

            if (GUILayout.Button("Right"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _tiltMethod.Invoke(host, new object[] { false });
            }

            GUILayout.Label("Shake");
            if (GUILayout.Button("2"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _shakeMethod.Invoke(host, new object[] { 2 });
            }

            GUILayout.Label("New Commands");


            serializedObject.ApplyModifiedProperties();
        }
    }
}