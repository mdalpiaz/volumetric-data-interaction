#nullable enable

using System;
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
using Networking.openIA;

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
        private GameObject tablet = null!;

        [SerializeField]
        private InterfaceController interfaceController = null!;

        [SerializeField]
        private MappingAnchor mappingAnchor = null!;
        
        [SerializeField]
        private int port = Ports.TabletPort;
        
        private TcpListener server = null!;
        private TcpClient? tabletClient;
        private NetworkStream? tabletStream;
        private Task? receivingTask;

        private Selectable? selected;
        private Selectable? highlighted;

        private Selectable? Selected
        {
            get => selected;
            set
            {
                Unselect();
                selected = value;
            }
        }

        public Selectable? Highlighted
        {
            get => highlighted;
            set
            {
                if (highlighted != null)
                {
                    highlighted.IsSelected = false;
                }

                highlighted = value;
            }
        }

        public event Action<Selectable>? MappingStarted;

        public event Action? MappingStopped;

        public event Action<Transform>? Sliced;

        public event Action<Snapshot>? SnapshotRemoved;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
                server = new TcpListener(IPAddress.Loopback, port);
            }
            else
            {
                Destroy(this);
            }
        }

        private async void OnEnable()
        {
            Debug.Log($"TabletServer started on port {port}");
            server.Start();
            try
            {
                tabletClient = await server.AcceptTcpClientAsync();
            }
            catch
            {
                Debug.Log("Tablet never connected.");
                return;
            }
            Debug.Log("Client connected");
            tabletStream = tabletClient.GetStream();

            receivingTask = Run();
        }

        private void Start()
        {
            ray.SetActive(false);
            Selected = ModelManager.Instance.CurrentModel.Selectable;
        }

        private async void OnDisable()
        {
            tabletClient?.Close();
            server.Stop();
            if (receivingTask != null)
            {
                await receivingTask;
            }
        }

        private async Task Run()
        {
            var commandIdentifier = new byte[1];
            while (true)
            {
                try
                {
                    if (tabletStream == null)
                    {
                        return;
                    }
                    await tabletStream.ReadAllAsync(commandIdentifier, 0, 1);
                    Debug.Log($"Received command {commandIdentifier[0]}");
                    switch (commandIdentifier[0])
                    {
                        case Categories.SelectionMode:
                            {
                                OnSelectionMode();
                                break;
                            }
                        case Categories.SlicingMode:
                            {
                                OnSlicingMode();
                                break;
                            }
                        case Categories.Select:
                            {
                                OnSelect();
                                break;
                            }
                        case Categories.Deselect:
                            {
                                OnDeselect();
                                break;
                            }
                        case Categories.Slice:
                            {
                                OnSlice();
                                break;
                            }
                        case Categories.RemoveSnapshot:
                            {
                                OnRemoveSnapshot();
                                break;
                            }
                        case Categories.ToggleAttached:
                            {
                                OnToggleAttached();
                                break;
                            }
                        case Categories.HoldBegin:
                            {
                                OnHoldBegin();
                                break;
                            }
                        case Categories.HoldEnd:
                            {
                                OnHoldEnd();
                                break;
                            }
                        case Categories.SendToScreen:
                            {
                                await OnSendToScreen();
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

        private void OnSelectionMode()
        {
            slicer.SetCuttingActive(false);
            ray.SetActive(true);
        }

        private void OnSlicingMode()
        {
            slicer.SetCuttingActive(true);
            ray.SetActive(false);
        }

        private void OnSelect()
        {
            if (Highlighted == null)
            {
                return;
            }

            Selected = Highlighted;
            Selected.IsSelected = true;

            ray.SetActive(false);
            Highlighted = null;

            if (tabletStream == null)
            {
                return;
            }
            
            if (Selected.TryGetComponent<Snapshot>(out _))
            {
                tabletStream.WriteByte(Categories.SelectedSnapshot);
            }
            else
            {
                tabletStream.WriteByte(Categories.SelectedModel);
            }
        }

        private void OnDeselect()
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
            selected = null;
            Highlighted = null;

            ray.SetActive(true);
        }

        private void OnSlice()
        {
            if (OpenIAWebSocketClient.Instance.IsOnline)
            {
                Sliced?.Invoke(slicer.transform);
                return;
            }
            
            var snapshot = slicer.CreateSnapshot();
            if (snapshot != null)
            {
                Sliced?.Invoke(slicer.transform);
            }
        }

        private void OnRemoveSnapshot()
        {
            if (Selected == null)
            {
                return;
            }
            if (!Selected.TryGetComponent<Snapshot>(out var snapshot))
            {
                return;
            }

            if (SnapshotManager.Instance.DeleteSnapshot(snapshot))
            {
                tabletStream?.WriteByte(Categories.SnapshotRemoved);
                SnapshotRemoved?.Invoke(snapshot);
            }
        }

        private void OnToggleAttached()
        {
            if (Selected == null)
            {
                return;
            }
            if (!Selected.TryGetComponent<Snapshot>(out var snapshot))
            {
                return;
            }

            if (snapshot.IsAttached)
            {
                snapshot.Detach();
            }
            else
            {
                var addition = interfaceController.GetNextAttachmentPoint();
                if (addition == null)
                {
                    return;
                }
                snapshot.Attach(interfaceController.Main.parent, addition);
            }
        }

        private void OnHoldBegin()
        {
            if (Selected == null)
            {
                return;
            }

            mappingAnchor.StartMapping(Selected.transform);
            MappingStarted?.Invoke(Selected);
        }

        private void OnHoldEnd()
        {
            if (mappingAnchor.StopMapping())
            {
                MappingStopped?.Invoke();
            }
        }

        private async Task OnSendToScreen()
        {
            if (Selected == null)
            {
                return;
            }
            if (!Selected.TryGetComponent<Snapshot>(out var snapshot))
            {
                return;
            }

            await ScreenServer.Instance.SendAsync(tablet.transform.position, tablet.transform.up, snapshot.SnapshotTexture);
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
            selected = null;
            Highlighted = null;
        }
    }
}
