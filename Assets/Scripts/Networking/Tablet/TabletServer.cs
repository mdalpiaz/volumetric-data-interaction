﻿#nullable enable

using Extensions;
using Helper;
using Model;
using Networking.screens;
using Slicing;
using Selection;
using Snapshots;
using Unity.Netcode;
using UnityEngine;

namespace Networking.Tablet
{
    public class TabletServer : MonoBehaviour
    {
        public static TabletServer Instance { get; private set; } = null!;
        
        [SerializeField]
        private InterfaceController ui = null!;
        
        [SerializeField]
        private GameObject ray = null!;
        
        [SerializeField]
        private Slicer slicer = null!;
        
        [SerializeField]
        private GameObject tracker = null!;

        [SerializeField]
        private GameObject tablet = null!;
        
        [SerializeField]
        private NetworkManager netMan = null!;

        [SerializeField]
        private ScreenServer screenServer = null!;

        private Player? _player;
        private MenuMode _menuMode;
        
        private Selectable? _selected;
        private Selectable? _highlighted;

        private Selectable? Selected
        {
            get => _selected;
            set
            {
                Unselect();
                _selected = value;
            }
        }

        public Selectable? Highlighted
        {
            get => _highlighted;
            set
            {
                if (_highlighted != null)
                {
                    _highlighted.IsSelected = false;
                }

                _highlighted = value;
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(this);
            }
        }

        private void OnEnable()
        {
            PlayerConnectedNotifier.OnPlayerConnected += HandlePlayerConnected;
        }

        private void Start()
        {
            netMan.StartHost();
            ray.SetActive(false);

            Selected = ModelManager.Instance.CurrentModel.Selectable;
        }

        private void OnDisable()
        {
            PlayerConnectedNotifier.OnPlayerConnected -= HandlePlayerConnected;
            if (_player != null)
            {
                DeregisterPlayerCallbacks(_player);
            }
        }

        private void HandlePlayerConnected(Player p)
        {
            // don't register itself
            if (p.IsLocalPlayer)
            {
                return;
            }

            if (_player != null)
            {
                Debug.LogWarning("Another player tried to register itself! There should only be one further player!");
                return;
            }
            Debug.Log("New player connected");
            RegisterPlayerCallbacks(p);
            _player = p;
        }

        private void RegisterPlayerCallbacks(Player p)
        {
            p.ModeChanged += HandleModeChange;
            p.ShakeCompleted += HandleShakes;
            p.Tilted += HandleTilt;
            p.Tapped += HandleTap;
            p.Swiped += HandleSwipe;
            p.Scaled += HandleScaling;
            p.Rotated += HandleRotation;
            p.TextReceived += HandleText;
        }

        private void DeregisterPlayerCallbacks(Player p)
        {
            p.ModeChanged -= HandleModeChange;
            p.ShakeCompleted -= HandleShakes;
            p.Tilted -= HandleTilt;
            p.Tapped -= HandleTap;
            p.Swiped -= HandleSwipe;
            p.Scaled -= HandleScaling;
            p.Rotated -= HandleRotation;
            p.TextReceived -= HandleText;
        }
        
        #region Player Callbacks
        
        private void HandleModeChange(MenuMode mode)
        {
            Debug.Log($"Changing Menu Mode to \"{mode}\"");
            if (_menuMode == mode)
            {
                return;
            }

            var isSnapshotSelected = false;
            switch (mode)
            {
                case MenuMode.None:
                    if (_menuMode == MenuMode.Analysis)
                    {
                        slicer.SetTemporaryCuttingPlaneActive(false);
                        SnapshotManager.Instance.DeactivateAllSnapshots();
                    }
                    else
                    {
                        ray.SetActive(false);

                        Unselect();
                        SnapshotManager.Instance.DeactivateAllSnapshots();
                    }
                    break;
                case MenuMode.Selection:
                    ray.SetActive(true);
                    break;
                case MenuMode.Selected:
                    if (Selected == null)
                    {
                        isSnapshotSelected = false;
                        break;
                    }
                    isSnapshotSelected = Selected.gameObject.IsSnapshot();
                    break;
                case MenuMode.Analysis:
                    slicer.SetTemporaryCuttingPlaneActive(true);
                    break;
                case MenuMode.Mapping:
                default:
                    Debug.Log($"{nameof(HandleModeChange)}() received unhandled mode: {mode}");
                    break;
            }

            ui.SetMode(mode, isSnapshotSelected);
            _menuMode = mode;
        }
        
        private void HandleShakes(int shakeCount)
        {
            if (shakeCount <= 1) // one shake can happen unintentionally
            {
                return;
            }

            if (Selected != null && Selected.TryGetComponent(out Snapshot snapshot))
            {
                SnapshotManager.Instance.DeleteSnapshot(snapshot);
            }
            else
            {
                var result = SnapshotManager.Instance.DeleteAllSnapshots();
                if (!result)
                {
                    ModelManager.Instance.CurrentModel.ResetMesh();
                    ModelManager.Instance.CurrentModel.RemoveCuts();
                }
            }

            HandleModeChange(MenuMode.None);
            if (_player != null) _player.MenuModeClientRpc(MenuMode.None);
        }

        private void HandleTilt(bool isLeft)
        {
            if (_menuMode != MenuMode.Selected)
            {
                return;
            }
            
            if (Selected != null && Selected.TryGetComponent(out Snapshot snapshot))
            {
                var direction = isLeft ? NeighbourDirection.Left : NeighbourDirection.Right;
                SnapshotManager.Instance.Move(snapshot, direction);
            }
        }

        private void HandleTap(TapType type, float x, float y)
        {
            switch(type)
            {
                case TapType.Single:
                    Debug.Log($"Single Tap received at ({x},{y})");
                    break;
                case TapType.Double:
                    Debug.Log($"Double Tap received at ({x},{y})");
                    if (_menuMode == MenuMode.Selection && Highlighted != null)
                    {
                        Selected = Highlighted;
                        Selected.IsSelected = true;

                        ray.SetActive(false);
                        Highlighted = null;

                        if (_player != null) _player.MenuModeClientRpc(MenuMode.Selected);
                        HandleModeChange(MenuMode.Selected);
                    }
                    else if (_menuMode == MenuMode.Analysis)
                    {
                        slicer.Slice();
                    }
                    break;
                case TapType.HoldStart:
                    Debug.Log($"Tap Hold Start received at: ({x},{y})");
                    break;
                case TapType.HoldEnd:
                    Debug.Log($"Tap Hold End received at: ({x},{y})");
                    break;
                default:
                    Debug.Log($"{nameof(HandleTap)}() received unhandled tap type: {type}");
                    break;
            }
        }

        private async void HandleSwipe(bool isSwipeInward, float endX, float endY, float angle)
        {
            // ignore inward swiped, outward swipes are used to create snapshots
            if (isSwipeInward)
            {
                return;
            }

            if (_menuMode == MenuMode.Selected
                && Direction.Up == DirectionMethods.GetDirectionDegree(angle)
                && Selected != null && Selected.TryGetComponent(out Snapshot snapshot))
            {
                await screenServer.Send(tablet.transform, snapshot.SnapshotTexture);
            }
            else if (_menuMode == MenuMode.Analysis)
            {
                await SnapshotManager.Instance.CreateSnapshot(angle);
            }
        }

        /// <summary>
        /// Depending on mode, the scale input is used for resizing object or recognised as a grab gesture
        /// </summary>
        private void HandleScaling(float scaleMultiplier)
        {
            if(_menuMode == MenuMode.Selected
               && Selected != null)
            {
                Selected.transform.localScale *= scaleMultiplier;
            }
            else if (Selected == null)
            {
                // TODO check scaleMultiplier to identify attach and detach commands
                SnapshotManager.Instance.ToggleSnapshotsAttached();
            }
        }

        private void HandleRotation(float rotationRadDelta)
        {
            if (Selected == null)
            {
                return;
            }

            var trackerTransform = tracker.transform;
            const float threshold = 20.0f;
            const float downAngle = 90.0f;

            if (trackerTransform.eulerAngles.x is >= downAngle - threshold and <= downAngle + threshold)
            {
                Selected.transform.Rotate(0.0f, rotationRadDelta * Mathf.Rad2Deg, 0.0f);
                return;
            }

            if (trackerTransform.rotation.x is >= 0f and <= 30f or >= 140f and <= 160f)
            {
                Selected.transform.Rotate(Vector3.up, -rotationRadDelta * Mathf.Rad2Deg);
            }
            else
            {
                Selected.transform.Rotate(Vector3.forward, rotationRadDelta * Mathf.Rad2Deg);
            }
        }

        private static void HandleText(string text) => Debug.Log($"Text received: {text}");
        
        #endregion

        private void Unselect()
        {
            if (Highlighted != null)
            {
                Highlighted.IsSelected = false;
            }
            else if (Selected != null)
            {
                Selected.IsSelected = false;
            }

            // manually set to null, as "IsSelected = null" can cause stack overflows through the constant calls to Unselect()
            _selected = null;
            Highlighted = null;
            ui.SetMode(MenuMode.None);
        }
    }
}