#nullable enable

using System;
using System.Threading.Tasks;
using Networking.Tablet;
using UnityEngine;

namespace Networking.openIA
{
    public class OpenIaWebSocketClient : MonoBehaviour
    {
        private static OpenIaWebSocketClient Instance { get; set; } = null!;

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

            TabletServer.Instance.Sliced += Sliced;
            TabletServer.Instance.MappingStopped += MappingStopped;
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
            TabletServer.Instance.Sliced -= Sliced;
            TabletServer.Instance.MappingStopped -= MappingStopped;
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

        private async void Sliced(Transform slicerTransform)
        {
            slicerTransform.GetPositionAndRotation(out var position, out var rotation);
            await Send(new CreateSnapshotClient(position, rotation));
        }
        
        private async void MappingStopped(Model.Model model)
        {
            model.transform.GetPositionAndRotation(out var position, out var rotation);
            await Send(new SetObjectTranslation(model.ID, position));
            await Send(new SetObjectRotationQuaternion(model.ID, rotation));
        }
    }
}