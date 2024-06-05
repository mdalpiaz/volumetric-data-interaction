#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Model;
using Networking.Tablet;
using Selection;
using Snapshots;
using UnityEngine;

namespace Networking.openIA
{
    public class OpenIAWebSocketClient : MonoBehaviour
    {
        public static OpenIAWebSocketClient Instance { get; private set; } = null!;

        [SerializeField]
        private bool isOnline = true;
        
        [SerializeField]
        private bool https;
        
        [SerializeField]
        private string ip = "127.0.0.1";

        [SerializeField]
        private int port = Ports.OpenIAPort;

        [SerializeField]
        private string path = "/";

        private WebSocketClient ws = null!;

        private ICommandInterpreter interpreter = null!;

        private ICommandSender sender = null!;

        private Selectable? selected;
        
        public ulong? ClientID { get; set; }

        public List<Viewer> Viewers { get; private set; } = new();
        
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
            if (!isOnline)
            {
                return;
            }

            TabletServer.Instance.MappingStarted += MappingStarted;
            TabletServer.Instance.MappingStopped += MappingStopped;
            TabletServer.Instance.Sliced += Sliced;
            TabletServer.Instance.SnapshotRemoved += SnapshotRemoved;
            TabletServer.Instance.SnapshotsCleared += SnapshotsCleared;
            TabletServer.Instance.ResettedState += ResettedState;
        }

        private async void Start()
        {
            if (!isOnline)
            {
                return;
            }
            ws = new WebSocketClient($"{(https ? "wss" : "ws")}://{ip}:{port}{(path.StartsWith("/") ? path : "/" + path)}", HandleBinaryData, HandleText);

            var newInterpreter = new InterpreterV1(ws);
            interpreter = newInterpreter;
            sender = newInterpreter;
            
            Debug.Log("Starting WebSocket client");
            try
            {
                await ws.ConnectAsync();
            }
            catch
            {
                Debug.LogError("Couldn't connect to WebSocket Server! Check ip and path!");
                return;
            }
            Debug.Log("Connected WebSocket client");
            var runTask = ws.Run();
            await newInterpreter.Start();
            await runTask;
            Debug.Log("WebSocket client stopped");
        }

        private void OnDisable()
        {
            if (!isOnline)
            {
                return;
            }

            TabletServer.Instance.MappingStarted -= MappingStarted;
            TabletServer.Instance.MappingStopped -= MappingStopped;
            TabletServer.Instance.Sliced -= Sliced;
            TabletServer.Instance.SnapshotRemoved -= SnapshotRemoved;
            TabletServer.Instance.SnapshotsCleared -= SnapshotsCleared;
            TabletServer.Instance.ResettedState -= ResettedState;
        }

        private void OnDestroy()
        {
            if (!isOnline)
            {
                return;
            }
            
            ws.Dispose();
        }

        private async Task Send(ICommand cmd) => await sender.Send(cmd);
        
        private Task HandleText(string text)
        {
            Debug.Log($"WS text received: \"{text}\"");
            return Task.CompletedTask;
        }
        
        private async Task HandleBinaryData(byte[] data)
        {
            Debug.Log($"WS bytes received: {BitConverter.ToString(data)}");
            await interpreter.Interpret(data);
        }
        
        private void MappingStarted(Selectable sel)
        {
            selected = sel;
        }

        private async void MappingStopped()
        {
            if (selected == null)
            {
                return;
            }

            if (!selected.TryGetComponent<Model.Model>(out var model))
            {
                return;
            }

            model.transform.GetPositionAndRotation(out var position, out var rotation);
            var localPosition = model.transform.InverseTransformPoint(position);
            var openIAPosition = CoordinateConverter.UnityToOpenIA(model, localPosition);
            await Send(new SetObjectTranslation(model.ID, openIAPosition));
            //await Send(new SetObjectRotationNormal(model.ID, rotation * Vector3.forward, rotation * Vector3.up));
            await Send(new SetObjectRotationQuaternion(model.ID, rotation));
        }

        private async void Sliced(Transform slicerTransform)
        {
            var model = ModelManager.Instance.CurrentModel;
            slicerTransform.GetPositionAndRotation(out var position, out var rotation);
            var localPosition = model.transform.InverseTransformPoint(position);
            var openIAPosition = CoordinateConverter.UnityToOpenIA(model, localPosition);
            await Send(new CreateSnapshotNormalClient(openIAPosition, rotation * Vector3.back));
        }

        private async void SnapshotRemoved(Snapshot s)
        {
            await Send(new RemoveSnapshot(s.ID));
        }

        private async void SnapshotsCleared(List<ulong> snapshotIDs)
        {
            var tasks = snapshotIDs.Select(id => Send(new RemoveSnapshot(id)));
            await Task.WhenAll(tasks);
        }

        private async void ResettedState()
        {
            await Send(new Reset());
        }
    }
}