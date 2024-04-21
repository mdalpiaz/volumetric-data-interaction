#nullable enable

using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Extensions;
using Model;
using Networking.Screens;
using Slicing;
using Selection;
using Snapshots;
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
        private MappingAnchor mappingAnchor = null!;
        
        [SerializeField]
        private int port = Ports.TabletPort;
        
        private TcpListener _server = null!;
        private TcpClient? _tabletClient;
        private NetworkStream? _tabletStream;

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
                _server = new TcpListener(IPAddress.Loopback, port);
            }
            else
            {
                Destroy(this);
            }
        }

        private async void OnEnable()
        {
            Debug.Log($"TabletServer started on port {port}");
            _server.Start();
            try
            {
                _tabletClient = await _server.AcceptTcpClientAsync();
            }
            catch
            {
                Debug.Log("Tablet never connected.");
                return;
            }
            Debug.Log("Client connected");
            _tabletStream = _tabletClient.GetStream();

            var commandIdentifier = new byte[1];
            while (true)
            {
                try
                {
                    await _tabletStream.ReadAllAsync(commandIdentifier, 0, 1);
                    Debug.Log($"Received command {commandIdentifier[0]}");
                    switch (commandIdentifier[0])
                    {
                        case Categories.MenuMode:
                            {
                                var buffer = new byte[MenuModeCommand.Size];
                                buffer[0] = commandIdentifier[0];
                                await _tabletStream.ReadAllAsync(buffer, 1, buffer.Length - 1);
                                var cmd = MenuModeCommand.FromByteArray(buffer);
                                HandleModeChange(cmd.Mode);
                                break;
                            }
                        case Categories.Swipe:
                            {
                                var buffer = new byte[SwipeCommand.Size];
                                buffer[0] = commandIdentifier[0];
                                await _tabletStream.ReadAllAsync(buffer, 1, buffer.Length - 1);
                                var cmd = SwipeCommand.FromByteArray(buffer);
                                HandleSwipe(cmd.Inward, cmd.EndPointX, cmd.EndPointY, cmd.Angle);
                                break;
                            }
                        case Categories.Scale:
                            {
                                var buffer = new byte[ScaleCommand.Size];
                                buffer[0] = commandIdentifier[0];
                                await _tabletStream.ReadAllAsync(buffer, 1, buffer.Length - 1);
                                var cmd = ScaleCommand.FromByteArray(buffer);
                                HandleScaling(cmd.Scale);
                                break;
                            }
                        case Categories.Rotate:
                            {
                                var buffer = new byte[RotateCommand.Size];
                                buffer[0] = commandIdentifier[0];
                                await _tabletStream.ReadAllAsync(buffer, 1, buffer.Length - 1);
                                var cmd = RotateCommand.FromByteArray(buffer);
                                HandleRotation(cmd.Rotation);
                                break;
                            }
                        case Categories.Tilt:
                            {
                                var buffer = new byte[TiltCommand.Size];
                                buffer[0] = commandIdentifier[0];
                                await _tabletStream.ReadAllAsync(buffer, 1, buffer.Length - 1);
                                var cmd = TiltCommand.FromByteArray(buffer);
                                HandleTilt(cmd.IsLeft);
                                break;
                            }
                        case Categories.Shake:
                            {
                                var buffer = new byte[ShakeCommand.Size];
                                buffer[0] = commandIdentifier[0];
                                await _tabletStream.ReadAllAsync(buffer, 1, buffer.Length - 1);
                                var cmd = ShakeCommand.FromByteArray(buffer);
                                HandleShakes(cmd.Count);
                                break;
                            }
                        case Categories.Tap:
                            {
                                var buffer = new byte[TapCommand.Size];
                                buffer[0] = commandIdentifier[0];
                                await _tabletStream.ReadAllAsync(buffer, 1, buffer.Length - 1);
                                var cmd = TapCommand.FromByteArray(buffer);
                                HandleTap(cmd.Type, cmd.X, cmd.Y);
                                break;
                            }
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        private void Start()
        {
            ray.SetActive(false);
            Selected = ModelManager.Instance.CurrentModel.Selectable;
        }

        private void OnDisable()
        {
            _tabletClient?.Close();
            _server.Stop();
        }
        
        private void HandleModeChange(MenuMode mode)
        {
            Debug.Log($"Menu Mode change requested: \"{mode}\"");
            if (_menuMode == mode)
            {
                return;
            }
            Debug.Log($"Changing Menu Mode to \"{mode}\"");

            var isSnapshotSelected = false;
            switch (mode)
            {
                case MenuMode.None:
                    if (_menuMode == MenuMode.Analysis)
                    {
                        slicer.SetCuttingActive(false);
                    }
                    else
                    {
                        ray.SetActive(false);
                        Unselect();
                    }
                    mappingAnchor.StopMapping();
                    SnapshotManager.Instance.DeactivateAllSnapshots();
                    break;
                case MenuMode.Selection:
                    ray.SetActive(true);
                    mappingAnchor.StopMapping();
                    break;
                case MenuMode.Selected:
                    isSnapshotSelected = Selected != null && Selected.gameObject.IsSnapshot();
                    break;
                case MenuMode.Analysis:
                    slicer.SetCuttingActive(true);
                    mappingAnchor.StopMapping();
                    break;
                default:
                    Debug.Log($"{nameof(HandleModeChange)}() received unhandled mode: {mode}");
                    break;
            }

            // stop mapping if the menu is changed
            ui.SetMode(mode, isSnapshotSelected);
            _menuMode = mode;
        }
        
        private async void HandleShakes(int shakeCount)
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
            await SendMenuModeToClient(MenuMode.None);
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

        private async void HandleTap(TapType type, float x, float y)
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

                        HandleModeChange(MenuMode.Selected);
                        await SendMenuModeToClient(MenuMode.Selected);
                    }
                    else if (_menuMode == MenuMode.Analysis)
                    {
                        slicer.Slice();
                    }
                    break;
                case TapType.HoldStart:
                    Debug.Log($"Tap Hold Start received at: ({x},{y})");
                    if (_menuMode == MenuMode.Selected &&
                        Selected != null &&
                        ModelManager.Instance.CurrentModel.gameObject == Selected.gameObject)
                    {
                        mappingAnchor.StartMapping(Selected.transform);
                    }
                    break;
                case TapType.HoldEnd:
                    Debug.Log($"Tap Hold End received at: ({x},{y})");
                    mappingAnchor.StopMapping();
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
                await ScreenServer.Instance.Send(tablet.transform, snapshot.SnapshotTexture);
            }
            else if (_menuMode == MenuMode.Analysis)
            {
                await SnapshotManager.Instance.CreateSnapshot(angle);
            }
        }

        /// <summary>
        /// Depending on mode, the scale input is used for resizing object or recognised as a grab gesture.
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

        private async Task SendMenuModeToClient(MenuMode mode)
        {
            if (_tabletStream == null)
            {
                return;
            }
            
            var buffer = new byte[2];
            buffer[0] = Categories.MenuMode;
            buffer[1] = (byte)mode;
            await _tabletStream.WriteAsync(buffer);
        }
    }
}
