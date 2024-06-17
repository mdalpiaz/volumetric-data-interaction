#nullable enable

using System.Net.Sockets;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.Screens
{
    public class ScreenClient : MonoBehaviour
    {
        [SerializeField]
        private int port = Ports.ScreenPort;

        [SerializeField]
        private RawImage image = null!;

        [SerializeField]
        private GameObject networkConfig = null!;

        [SerializeField]
        private TMP_InputField ipInput = null!;

        [SerializeField]
        private TMP_InputField idInput = null!;

        private TcpClient client = null!;
        
        private RectTransform rect = null!;

        private Vector2 rectSize;

        private void Awake()
        {
            client = new TcpClient();
            rect = image.GetComponent<RectTransform>();
            rectSize = rect.sizeDelta;
        }

        private void OnDisable()
        {
            client.Close();
        }

        public void OnConnectClicked()
        {
            var ip = ipInput.text;
            if (!int.TryParse(idInput.text, out var id))
            {
                Debug.LogError("Couldn't parse ID!");
                return;
            }
            
            client.Connect(ip, port);
            using var stream = client.GetStream();
            
            networkConfig.SetActive(false);
            image.gameObject.SetActive(true);

            stream.Write(new IDAdvertisement(id).ToByteArray());
            Debug.Log($"ID sent {id}");

            var dimBuffer = new byte[Dimensions.Size];
            
            while (true)
            {
                try
                {
                    stream.ReadAll(dimBuffer, 0, Dimensions.Size);
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
                    stream.ReadAll(buffer, 0, buffer.Length);
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
                rect.sizeDelta = ExpandToRectSize(dims.Width, dims.Height);
            }
            
            Debug.LogWarning("Client loop has stopped!");
        }

        private Vector2 ExpandToRectSize(int width, int height)
        {
            // currently only supports images that are taller than wider
            var aspect = width / (float)height;
            var newWidth = aspect * rectSize.y;
            return new Vector2(newWidth, rectSize.y);
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