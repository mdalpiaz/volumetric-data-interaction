#nullable enable

using Extensions;
using System;
using System.Net.Sockets;
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
        
        private TcpClient _tcpClient = null!;
        private NetworkStream _stream = null!;
        
        public event Action<MenuMode>? MenuModeChanged;

        private void Awake()
        {
            _tcpClient = new TcpClient();
        }

        private async void OnEnable()
        {
            await _tcpClient.ConnectAsync(ip, port);
            _stream = _tcpClient.GetStream();
        }

        private async void Start()
        {
            // the only command which can be received is "changing menu mode"
            var buffer = new byte[2];
            while (true)
            {
                try
                {
                    await _stream.ReadAllAsync(buffer, 0, 2);
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
        }

        private void OnDisable()
        {
            _tcpClient.Close();
        }

        public async Task SendMenuChangedMessage(MenuMode mode)
        {
            Debug.Log($"Sending menu change: {mode}");
            var buffer = new byte[2];
            buffer[0] = Categories.MenuMode;
            buffer[1] = (byte)mode;
            await _stream.WriteAsync(buffer);
        }

        public async Task SendSwipeMessage(bool inward, float endPointX, float endPointY, float angle)
        {
            var buffer = new byte[14];
            buffer[0] = Categories.Swipe;
            buffer[1] = BitConverter.GetBytes(inward)[0];
            Buffer.BlockCopy(BitConverter.GetBytes(endPointX), 0, buffer, 2, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(endPointY), 0, buffer, 6, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(angle), 0, buffer, 10, sizeof(float));
            await _stream.WriteAsync(buffer);
        }

        public async Task SendScaleMessage(float scale)
        {
            Debug.Log($"Sending scale: {scale}");
            var buffer = new byte[5];
            buffer[0] = Categories.Scale;
            Buffer.BlockCopy(BitConverter.GetBytes(scale), 0, buffer, 1, sizeof(float));
            await _stream.WriteAsync(buffer);
        }

        public async Task SendRotateMessage(float rotation)
        {
            Debug.Log($"Sending rotation: {rotation}");
            var buffer = new byte[5];
            buffer[0] = Categories.Rotate;
            Buffer.BlockCopy(BitConverter.GetBytes(rotation), 0, buffer, 1, sizeof(float));
            await _stream.WriteAsync(buffer);
        }

        public async Task SendTiltMessage(bool isLeft)
        {
            Debug.Log($"Sending tilt {(isLeft ? "left" : "right")}");
            var buffer = new byte[2];
            buffer[0] = Categories.Tilt;
            buffer[1] = BitConverter.GetBytes(isLeft)[0];
            await _stream.WriteAsync(buffer);
        }

        public async Task SendShakeMessage(int count)
        {
            Debug.Log($"Sending shake: {count}");
            var buffer = new byte[5];
            buffer[0] = Categories.Shake;
            Buffer.BlockCopy(BitConverter.GetBytes(count), 0, buffer, 1, sizeof(int));
            await _stream.WriteAsync(buffer);
        }

        public async Task SendTapMessage(TapType type, float x, float y)
        {
            Debug.Log($"Sending tap: {type} at ({x},{y})");
            var buffer = new byte[10];
            buffer[0] = Categories.Tap;
            buffer[1] = (byte)type;
            Buffer.BlockCopy(BitConverter.GetBytes(x), 0, buffer, 2, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(y), 0, buffer, 6, sizeof(float));
            await _stream.WriteAsync(buffer);
        }
    }
}
