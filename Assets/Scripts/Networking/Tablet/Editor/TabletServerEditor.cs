using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

        private MethodInfo onSelection;
        private MethodInfo onSlicing;
        private MethodInfo onSelect;
        private MethodInfo onDeselect;
        private MethodInfo onSlice;
        private MethodInfo onRemoveSnapshot;
        private MethodInfo onToggleAttached;
        private MethodInfo onHoldBegin;
        private MethodInfo onHoldEnd;
        private MethodInfo onSendToScreen;

        private void Awake()
        {
            _modeMethod = typeof(TabletServer).GetMethod("HandleModeChange", BindingFlags.NonPublic | BindingFlags.Instance);
            _tapMethod = typeof(TabletServer).GetMethod("HandleTap", BindingFlags.NonPublic | BindingFlags.Instance);
            _swipeMethod = typeof(TabletServer).GetMethod("HandleSwipe", BindingFlags.NonPublic | BindingFlags.Instance);
            _tiltMethod = typeof(TabletServer).GetMethod("HandleTilt", BindingFlags.NonPublic | BindingFlags.Instance);
            _shakeMethod = typeof(TabletServer).GetMethod("HandleShakes", BindingFlags.NonPublic | BindingFlags.Instance);

            // new commands
            onSelection = typeof(TabletServer).GetMethod("OnSelectionMode", BindingFlags.NonPublic | BindingFlags.Instance);
            onSlicing = typeof(TabletServer).GetMethod("OnSlicingMode", BindingFlags.NonPublic | BindingFlags.Instance);
            onSelect = typeof(TabletServer).GetMethod("OnSelect", BindingFlags.NonPublic | BindingFlags.Instance);
            onDeselect = typeof(TabletServer).GetMethod("OnDeselect", BindingFlags.NonPublic | BindingFlags.Instance);
            onSlice = typeof(TabletServer).GetMethod("OnSlice", BindingFlags.NonPublic | BindingFlags.Instance);
            onRemoveSnapshot = typeof(TabletServer).GetMethod("OnRemoveSnapshot", BindingFlags.NonPublic | BindingFlags.Instance);
            onToggleAttached = typeof(TabletServer).GetMethod("OnToggleAttached", BindingFlags.NonPublic | BindingFlags.Instance);
            onHoldBegin = typeof(TabletServer).GetMethod("OnHoldBegin", BindingFlags.NonPublic | BindingFlags.Instance);
            onHoldEnd = typeof(TabletServer).GetMethod("OnHoldEnd", BindingFlags.NonPublic | BindingFlags.Instance);
            onSendToScreen = typeof(TabletServer).GetMethod("OnSendToScreen", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public override async void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawDefaultInspector();
            
            var host = (TabletServer)serializedObject.targetObject;

            GUILayout.Label("Modes");
            if (GUILayout.Button("None"))
            {
                _modeMethod.Invoke(host, new object[] { MenuMode.None });
            }
            
            if (GUILayout.Button("Analysis Mode"))
            {
                _modeMethod.Invoke(host, new object[] { MenuMode.None });
                _modeMethod.Invoke(host, new object[] { MenuMode.Analysis });
            }

            if (GUILayout.Button("Selection Mode"))
            {
                _modeMethod.Invoke(host, new object[] { MenuMode.None });
                _modeMethod.Invoke(host, new object[] { MenuMode.Selection });
            }

            GUILayout.Label("Tap");
            if (GUILayout.Button("Double Tap"))
            {
                _tapMethod.Invoke(host, new object[] { TapType.Double, 250, 250 });
            }

            if (GUILayout.Button("Hold Start"))
            {
                _tapMethod.Invoke(host, new object[] { TapType.HoldBegin, 250, 250 });
            }
            
            if (GUILayout.Button("Hold End"))
            {
                _tapMethod.Invoke(host, new object[] { TapType.HoldEnd, 250, 250 });
            }

            GUILayout.Label("Swipe");
            if (GUILayout.Button("Left"))
            {
                _swipeMethod.Invoke(host, new object[] { false, 0, 150, 180 });
            }

            if (GUILayout.Button("Right"))
            {
                _swipeMethod.Invoke(host, new object[] { false, 500, 150, 0});
            }
            
            if (GUILayout.Button("Up"))
            {
                _swipeMethod.Invoke(host, new object[] { false, 250, 300, 90});
            }

            GUILayout.Label("Tilt");
            if (GUILayout.Button("Left"))
            {
                _tiltMethod.Invoke(host, new object[] { true });
            }

            if (GUILayout.Button("Right"))
            {
                _tiltMethod.Invoke(host, new object[] { false });
            }

            GUILayout.Label("Shake");
            if (GUILayout.Button("2"))
            {
                _shakeMethod.Invoke(host, new object[] { 2 });
            }

            GUILayout.Label("New Commands");

            if (GUILayout.Button("OnSelectionMode"))
            {
                onSelection.Invoke(host, null);
            }
            if (GUILayout.Button("OnSlicingMode"))
            {
                onSlicing.Invoke(host, null);
            }
            if (GUILayout.Button("OnSelect"))
            {
                onSelect.Invoke(host, null);
            }
            if (GUILayout.Button("OnDeselect"))
            {
                onDeselect.Invoke(host, null);
            }
            if (GUILayout.Button("OnSlice"))
            {
                onSlice.Invoke(host, null);
            }
            if (GUILayout.Button("OnRemoveSnapshot"))
            {
                onRemoveSnapshot.Invoke(host, null);
            }
            if (GUILayout.Button("OnToggleAttached"))
            {
                onToggleAttached.Invoke(host, null);
            }
            if (GUILayout.Button("OnHoldBegin"))
            {
                onHoldBegin.Invoke(host, null);
            }
            if (GUILayout.Button("OnHoldEnd"))
            {
                onHoldEnd.Invoke(host, null);
            }
            if (GUILayout.Button("OnSendToScreen"))
            {
                await (Task)onSendToScreen.Invoke(host, null);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}