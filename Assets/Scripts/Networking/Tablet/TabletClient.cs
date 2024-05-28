#nullable enable

using Extensions;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Networking.Tablet
{
    public class TabletClient : MonoBehaviour
    {
        [SerializeField]
        private string ip = "127.0.0.1";

        [SerializeField]
        private int port = Ports.TabletPort;

        public string IP
        {
            get => ip;
            set => ip = value;
        }

        public int Port
        {
            get => port;
            set => port = value;
        }
        
        private TcpClient tcpClient = null!;
        private NetworkStream stream = null!;
        private Thread receivingThread = null!;
        
        public event Action<MenuMode>? MenuModeChanged;

        private void Awake()
        {
            tcpClient = new TcpClient();
        }

        private void OnDestroy()
        {
            tcpClient.Close();
            receivingThread.Join();
        }

        public async Task Connect()
        {
            await tcpClient.ConnectAsync(IP, Port);
            stream = tcpClient.GetStream();
            Debug.Log("Connected to server");

            receivingThread = new Thread(() =>
            {
                // the only command which can be received is "changing menu mode"
                var buffer = new byte[2];
                while (true)
                {
                    try
                    {
                        stream.ReadAll(buffer, 0, 2);
                    }
                    catch
                    {
                        break;
                    }
                    if (buffer[0] != Categories.MenuMode)
                    {
                        Debug.LogWarning("Unsupported command was received!");
                        continue;
                    }
                    MenuModeChanged?.Invoke((MenuMode)buffer[1]);
                }
            });
            receivingThread.Start();
        }

        public async Task SendMenuChangedMessage(MenuMode mode)
        {
            Debug.Log($"Sending menu change: {mode}");
            await stream.WriteAsync(new MenuModeCommand(mode).ToByteArray());
        }

        public async Task SendSwipeMessage(bool inward, float endPointX, float endPointY, float angle)
        {
            Debug.Log($"Sending swipe: inward: {inward} at ({endPointX}, {endPointY}), angle: {angle}");
            await stream.WriteAsync(new SwipeCommand(inward, endPointX, endPointY, angle).ToByteArray());
        }

        public async Task SendScaleMessage(float scale)
        {
            Debug.Log($"Sending scale: {scale}");
            await stream.WriteAsync(new ScaleCommand(scale).ToByteArray());
        }

        public async Task SendShakeMessage(int count)
        {
            Debug.Log($"Sending shake: {count}");
            await stream.WriteAsync(new ShakeCommand(count).ToByteArray());
        }

        public async Task SendTapMessage(TapType type, float x, float y)
        {
            Debug.Log($"Sending tap: {type} at ({x},{y})");
            await stream.WriteAsync(new TapCommand(type, x, y).ToByteArray());
        }
    }
}
