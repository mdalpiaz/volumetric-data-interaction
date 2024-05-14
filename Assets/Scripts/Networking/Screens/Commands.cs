#nullable enable

using System;
using System.Buffers.Binary;
using UnityEngine;

namespace Networking.Screens
{
    public record IDAdvertisement(int ID) : ICommand
    {
        public byte[] ToByteArray()
        {
            var buffer = new byte[Size];
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(), ID);
            return buffer;
        }

        public static int Size => sizeof(int);

        public static IDAdvertisement FromByteArray(byte[] buffer)
        {
            return new IDAdvertisement(BinaryPrimitives.ReadInt32LittleEndian(buffer));
        }
    }

    public record Dimensions(int Width, int Height) : ICommand
    {
        public byte[] ToByteArray()
        {
            var request = new byte[Size];
            BinaryPrimitives.WriteInt32LittleEndian(request.AsSpan(), Width);
            BinaryPrimitives.WriteInt32LittleEndian(request.AsSpan(sizeof(int)), Height);
            return request;
        }

        public static int Size => sizeof(int) + sizeof(int);

        public static Dimensions FromByteArray(byte[] buffer)
        {
            var width = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan());
            var height = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(sizeof(int)));
            
            return new Dimensions(width, height);
        }
    }

    public record ImageData(Color32[] Data) : ICommand
    {
        public byte[] ToByteArray()
        {
            var request = new byte[Data.Length * 4];

            for (var i = 0; i < Data.Length; i++)
            {
                request[i * 4] = Data[i].r;
                request[i * 4 + 1] = Data[i].g;
                request[i * 4 + 2] = Data[i].b;
                request[i * 4 + 3] = Data[i].a;
            }
            
            return request;
        }

        public static int GetBufferSize(Dimensions dims)
        {
            return dims.Width * dims.Height * 4;
        }

        public static ImageData FromByteArray(byte[] buffer)
        {
            var data = new Color32[buffer.Length / 4];

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = new Color32(
                    buffer[i * 4],
                    buffer[i * 4 + 1],
                    buffer[i * 4 + 2],
                    buffer[i * 4 + 3]);
            }
            
            return new ImageData(data);
        }
    }
}