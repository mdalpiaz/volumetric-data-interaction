#nullable enable

using System.Collections.Generic;
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
                    var buffer = new byte[IDAdvertisement.Size];
                    await stream.ReadAllAsync(buffer, 0, buffer.Length);
                    var idAd = IDAdvertisement.FromByteArray(buffer);
                    _clients.Add(idAd.ID, (client, stream));
                    Debug.Log($"Client {idAd.ID} connected");
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

        public void Send(Vector3 trackerPosition, Vector3 trackerPointDirection, Texture2D data)
        {
            if (!FindScreen(out var screen, trackerPosition, trackerPointDirection))
            {
                return;
            }

            Debug.Log($"Sending to screen {screen}");

            var imageData = new ImageData(data.GetPixels32());
            var dims = new Dimensions(data.width, data.height);
            var (_, stream) = _clients[screen];
            stream.Write(dims.ToByteArray());
            stream.Write(imageData.ToByteArray());
        }

        public async Task SendAsync(Vector3 trackerPosition, Vector3 trackerPointDirection, Texture2D data)
        {
            if (!FindScreen(out var screen, trackerPosition, trackerPointDirection))
            {
                return;
            }
            
            Debug.Log($"Sending to screen {screen}");

            var imageData = new ImageData(data.GetPixels32());
            var dims = new Dimensions(data.width, data.height);
            var (_, stream) = _clients[screen];
            await stream.WriteAsync(dims.ToByteArray());
            await stream.WriteAsync(imageData.ToByteArray());
        }

        private bool FindScreen(out int id, Vector3 trackerPosition, Vector3 trackerPointDirection)
        {
            const float MaxCheckDistance = 100.0f;

            foreach (var s in screens)
            {
                var ray = new Ray(trackerPosition, trackerPointDirection);
                if (s.BoxCollider.Raycast(ray, out _, MaxCheckDistance))
                {
                    id = s.ID;
                    return true;
                }
            }

            id = 0;
            return false;
        }
    }
}