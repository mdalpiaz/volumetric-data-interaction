using System.Reflection;
using Networking.Tablet;
using UnityEditor;
using UnityEngine;

namespace Networking.Editor
{
    [CustomEditor(typeof(TabletServer))]
    public class TabletServerEditor : UnityEditor.Editor
    {
        private MethodInfo _modeMethod;
        private MethodInfo _tapMethod;
        private MethodInfo _swipeMethod;
        private MethodInfo _tiltMethod;
        private MethodInfo _shakeMethod;

        private void Awake()
        {
            _modeMethod = typeof(TabletServer).GetMethod("HandleModeChange", BindingFlags.NonPublic | BindingFlags.Instance);
            _tapMethod = typeof(TabletServer).GetMethod("HandleTap", BindingFlags.NonPublic | BindingFlags.Instance);
            _swipeMethod = typeof(TabletServer).GetMethod("HandleSwipe", BindingFlags.NonPublic | BindingFlags.Instance);
            _tiltMethod = typeof(TabletServer).GetMethod("HandleTilt", BindingFlags.NonPublic | BindingFlags.Instance);
            _shakeMethod = typeof(TabletServer).GetMethod("HandleShakes", BindingFlags.NonPublic | BindingFlags.Instance);
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

            GUILayout.Label("Interaction");
            if (GUILayout.Button("Double Tap"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _tapMethod.Invoke(host, new object[] { TapType.Double, 250, 250 });
            }

            if (GUILayout.Button("Swipe Left"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _swipeMethod.Invoke(host, new object[] { false, 0, 150, 180 });
            }

            if (GUILayout.Button("Swipe Right"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _swipeMethod.Invoke(host, new object[] { false, 500, 150, 0});
            }
            
            if (GUILayout.Button("Swipe Up"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _swipeMethod.Invoke(host, new object[] { false, 250, 300, 90});
            }

            if (GUILayout.Button("Tilt left"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _tiltMethod.Invoke(host, new object[] { true });
            }

            if (GUILayout.Button("Tilt right"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _tiltMethod.Invoke(host, new object[] { false });
            }

            if (GUILayout.Button("Shake"))
            {
                var host = (TabletServer)serializedObject.targetObject;
                _shakeMethod.Invoke(host, new object[] { 2 });
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}