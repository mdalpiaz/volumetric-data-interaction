#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Extensions;
using UnityEngine;

namespace Networking.Screens
{
    public class ScreenServer : MonoBehaviour
    {
        public static ScreenServer Instance { get; private set; } = null!;
        
        private const float ConeAngleDegree = 30.0f;
        
        [SerializeField]
        private int port = Ports.ScreenPort;

        [SerializeField]
        private List<Screen> screens = new();

        private TcpListener _server = null!;

        private readonly Dictionary<int, (TcpClient, NetworkStream)> _clients = new();

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

        private async void Start()
        {
            _server.Start();
            Debug.Log($"Screen server started on port {port}.");

            while (true)
            {
                try
                {
                    var client = await _server.AcceptTcpClientAsync();
                    var stream = client.GetStream();
                    var buffer = new byte[4];
                    await stream.ReadAllAsync(buffer, 0, 4);
                    var id = BitConverter.ToInt32(buffer);
                    _clients.Add(id, (client, stream));
                    Debug.Log($"Client {id} connected");
                }
                catch
                {
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var (_, (c, _)) in _clients)
            {
                c.Close();
            }
            _server.Stop();
        }

        public async Task Send(Vector3 trackerPosition, Vector3 trackerPointDirection, Texture2D data)
        {
            if (!FindScreen(out var screen, trackerPosition, trackerPointDirection))
            {
                return;
            }
            
            Debug.Log($"Sending to screen {screen}");
            
            var colors = data.GetPixels32();
            var dimBuffer = new byte[8];
            Buffer.BlockCopy(BitConverter.GetBytes(data.width), 0, dimBuffer, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(data.height), 0, dimBuffer, 4, 4);
            
            var bytes = new byte[colors.Length * 4];

            for (var i = 0; i < colors.Length; i++)
            {
                bytes[i * 4] = colors[i].r;
                bytes[i * 4 + 1] = colors[i].g;
                bytes[i * 4 + 2] = colors[i].b;
                bytes[i * 4 + 3] = colors[i].a;
            }

            var (_, stream) = _clients[screen];
            await stream.WriteAsync(dimBuffer);
            await stream.WriteAsync(bytes);
        }

        private bool FindScreen(out int id, Vector3 trackerPosition, Vector3 trackerPointDirection)
        {
            if (screens.Count == 0)
            {
                id = 0;
                return false;
            } 
            
            var possibleScreens = screens
                .Select(s => (s.ID, CalculateAngleToScreen(trackerPosition, trackerPointDirection, s.transform.position)))
                .Where(s => s.Item2 <= ConeAngleDegree)
                .OrderBy(s => s.Item2)
                .Select(s => s.ID)
                .ToList();

            if (!possibleScreens.Any())
            {
                id = 0;
                return false;
            }

            id = possibleScreens.First();
            return true;
        }

        private static float CalculateAngleToScreen(Vector3 trackerPosition, Vector3 trackerRotation, Vector3 screenPosition)
        {
            // based on answer here: https://stackoverflow.com/questions/1167022/2d-geometry-how-to-check-if-a-point-is-inside-an-angle
            var vec = Vector3.Normalize(screenPosition - trackerPosition);
            var dot = Vector3.Dot(vec, trackerRotation);
            return Mathf.Acos(dot) * Mathf.Rad2Deg;
        }
    }
}