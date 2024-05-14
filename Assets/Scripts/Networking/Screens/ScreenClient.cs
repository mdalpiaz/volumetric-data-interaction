#nullable enable

using System.Net.Sockets;
using Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.Screens
{
    public class ScreenClient : MonoBehaviour
    {
        [SerializeField]
        private string ip = "127.0.0.1";

        [SerializeField]
        private int port = Ports.ScreenPort;

        [SerializeField]
        private int id = 1;

        [SerializeField]
        private RawImage image = null!;

        private TcpClient _client = null!;
        
        private RectTransform _rect = null!;

        private Vector2 _rectSize;

        private void Awake()
        {
            _client = new TcpClient();
            _rect = image.GetComponent<RectTransform>();
            _rectSize = _rect.sizeDelta;
        }

        private async void OnEnable()
        {
            await _client.ConnectAsync(ip, port);
            await using var stream = _client.GetStream();

            await stream.WriteAsync(new IDAdvertisement(id).ToByteArray());
            Debug.Log($"ID sent {id}");

            var dimBuffer = new byte[Dimensions.Size];
            
            while (true)
            {
                try
                {
                    await stream.ReadAllAsync(dimBuffer, 0, Dimensions.Size);
                }
                catch
                {
                    break;
                }

                var dims = Dimensions.FromByteArray(dimBuffer);
                Debug.Log($"Received dimensions: {dims.Width}, {dims.Height}");

                var buffer = new byte[ImageData.GetBufferSize(dims)];
                try
                {
                    await stream.ReadAllAsync(buffer, 0, buffer.Length);
                }
                catch
                {
                    break;
                }
                Debug.Log("Image read");
                var imageData = ImageData.FromByteArray(buffer);

                // we are done with a packet
                // the texture is correct! it exports to the correct image
                image.texture = DataToTexture(dims, imageData);
                _rect.sizeDelta = ExpandToRectSize(dims.Width, dims.Height);
            }
            
            Debug.LogWarning("Client loop has stopped!");
        }

        private void OnDisable()
        {
            _client.Close();
        }

        private Vector2 ExpandToRectSize(int width, int height)
        {
            // currently only supports images that are taller than wider
            var aspect = width / (float)height;
            var newWidth = aspect * _rectSize.y;
            return new Vector2(newWidth, _rectSize.y);
        }

        private static Texture2D DataToTexture(Dimensions dims, ImageData data)
        {
            var tex = new Texture2D(dims.Width, dims.Height);
            tex.SetPixels32(data.Data);
            tex.Apply();
            return tex;
        }
    }
}