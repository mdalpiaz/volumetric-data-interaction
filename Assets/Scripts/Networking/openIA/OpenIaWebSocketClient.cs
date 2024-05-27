#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Model;
using Networking.Tablet;
using Snapshots;
using UnityEngine;

namespace Networking.openIA
{
    public class OpenIaWebSocketClient : MonoBehaviour
    {
        public static OpenIaWebSocketClient Instance { get; private set; } = null!;

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

        public ulong? ClientID { get; set; }

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
        
        private async void MappingStopped(Model.Model model)
        {
            model.transform.GetPositionAndRotation(out var position, out var rotation);
            await Send(new SetObjectTranslation(model.ID, position));
            await Send(new SetObjectRotationQuaternion(model.ID, rotation));
        }

        private async void Sliced(Transform slicerTransform)
        {
            slicerTransform.GetPositionAndRotation(out var position, out var rotation);
            var localPosition = ModelManager.Instance.CurrentModel.transform.InverseTransformPoint(position);
            var openIAPosition = CoordinateConverter.UnityToOpenIA(localPosition);
            await Send(new CreateSnapshotClient(openIAPosition, rotation));
        }

        //private async void SnapshotCreated(Snapshot s)
        //{
        //    s.OriginPlane.transform.GetPositionAndRotation(out var position, out var rotation);
        //    await Send(new CreateSnapshotClient(position, rotation));
        //}

        private async void SnapshotRemoved(Snapshot s)
        {
            await Send(new RemoveSnapshot(s.ID));
        }

        private async void SnapshotsCleared(List<ulong> snapshotIDs)
        {
            var tasks = new List<Task>();
            foreach (var id in snapshotIDs)
            {
                tasks.Add(Send(new RemoveSnapshot(id)));
            }

            await Task.WhenAll(tasks);
        }

        private async void ResettedState()
        {
            await Send(new Reset());
        }
    }
}