#nullable enable

using System;
using System.Collections.Generic;
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

            await stream.WriteAsync(BitConverter.GetBytes(id));
            Debug.Log($"ID sent {id}");

            var dimBuffer = new byte[8];
            
            while (true)
            {
                try
                {
                    await stream.ReadAllAsync(dimBuffer, 0, 8);
                }
                catch
                {
                    break;
                }

                var width = BitConverter.ToInt32(dimBuffer, 0);
                var height = BitConverter.ToInt32(dimBuffer, 4);
                Debug.Log($"Received dimensions: {width}, {height}");

                var buffer = new byte[width * height * 4];
                try
                {
                    await stream.ReadAllAsync(buffer, 0, buffer.Length);
                }
                catch
                {
                    break;
                }
                Debug.Log("Image read");

                // we are done with a packet
                // the texture is correct! it exports to the correct image
                image.texture = DataToTexture(width, height, buffer);
                _rect.sizeDelta = ExpandToRectSize(width, height);
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
            var aspect = (float)width / (float)height;
            var newWidth = aspect * _rectSize.y;
            return new Vector2(newWidth, _rectSize.y);
        }

        private static Texture2D DataToTexture(int width, int height, IReadOnlyList<byte> data)
        {
            var tex = new Texture2D(width, height);
            var colors = new Color32[data.Count / 4];
            for (var i = 0; i < colors.Length; i++)
            {
                colors[i].r = data[i * 4];
                colors[i].g = data[i * 4 + 1];
                colors[i].b = data[i * 4 + 2];
                colors[i].a = data[i * 4 + 3];
            }
            tex.SetPixels32(colors);
            tex.Apply();

            return tex;
        }
    }
}