#nullable enable

using System;
using System.Threading.Tasks;
using Networking.openIA.Commands;
using UnityEngine;

namespace Networking.openIA
{
    public class OpenIaWebSocketClient : MonoBehaviour
    {
        public static OpenIaWebSocketClient Instance { get; private set; } = null!;
        
        [SerializeField]
        private bool https;
        
        [SerializeField]
        private string ip = "127.0.0.1";

        [SerializeField]
        private int port = Ports.OpenIAPort;

        [SerializeField]
        private string path = "/";

        private WebSocketClient _ws = null!;

        private ICommandInterpreter _interpreter = null!;

        private ICommandSender _sender = null!;

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

        private async void Start()
        {
            if (!OnlineState.Instance.IsOnline)
            {
                return;
            }
            _ws = new WebSocketClient($"{(https ? "wss" : "ws")}://{ip}:{port}{(path.StartsWith("/") ? path : "/" + path)}", HandleBinaryData, HandleText);
            
            var negotiator = new ProtocolNegotiator(_ws);
            _interpreter = negotiator;
            
            Debug.Log("Starting WebSocket client");
            try
            {
                await _ws.ConnectAsync();
            }
            catch
            {
                Debug.LogError("Couldn't connect to WebSocket Server! Check ip and path!");
                return;
            }
            Debug.Log("Connected WebSocket client");
            var runTask = _ws.Run();

            try
            {
                (_interpreter, _sender) = await negotiator.Negotiate();
            }
            catch (NoProtocolMatchException)
            {
                // no supported version matches
                await _ws.Close();
                _ws.Dispose();
                return;
            }
            
            await runTask;
            Debug.Log("WebSocket client stopped");
        }

        private void OnDestroy()
        {
            if (!OnlineState.Instance.IsOnline)
            {
                return;
            }
            
            _ws.Dispose();
        }

        public async Task Send(ICommand cmd) => await _sender.Send(cmd);
        
        private void HandleText(string text)
        {
            Debug.Log($"WS text received: \"{text}\"");
        }
        
        private async void HandleBinaryData(byte[] data)
        {
            Debug.Log($"WS bytes received: {BitConverter.ToString(data)}");
            await _interpreter.Interpret(data);
        }
    }
}