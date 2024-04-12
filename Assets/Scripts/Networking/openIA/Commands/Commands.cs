#nullable enable

using System.Buffers.Binary;
using System;
using UnityEngine;

namespace Networking.openIA.Commands
{
    public record ACK() : ICommand
    {
        public byte[] ToByteArray()
        {
            return new byte[] { Categories.ACK.Value };
        }
    }

    public record NAK() : ICommand
    {
        public byte[] ToByteArray()
        {
            return new byte[] { Categories.NAK.Value };
        }
    }

    public record ProtocolAdvertisement(ulong Version) : ICommand
    {
        public byte[] ToByteArray()
        {
            var request = new byte[Size];
            request[0] = Categories.ProtocolAdvertisement.Value;
            BinaryPrimitives.WriteUInt64BigEndian(request.AsSpan(1), Version);
            return request;
        }

        public static int Size => 1 + sizeof(ulong);
    }

    public record ClientLoginResponse(ulong Id) : ICommand
    {
        public byte[] ToByteArray()
        {
            var request = new byte[Size];
            request[0] = Categories.Client.Value;
            BinaryPrimitives.WriteUInt64BigEndian(request.AsSpan(1), Id);
            return request;
        }

        public static int Size => 1 + sizeof(ulong);

        public static ClientLoginResponse FromByteArray(byte[] buffer)
        {
            return new ClientLoginResponse(BinaryPrimitives.ReadUInt64BigEndian(buffer.AsSpan(1)));
        }
    }

    public record CreateSnapshotClient(Vector3 Position, Quaternion Rotation) : ICommand
    {
        public byte[] ToByteArray()
        {
            var request = new byte[Size];
            request[0] = Categories.Snapshots.Value;
            request[1] = Categories.Snapshots.Create;
            Buffer.BlockCopy(BitConverter.GetBytes(Position.x), 0, request, 2, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(Position.y), 0, request, 6, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(Position.z), 0, request, 10, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(Rotation.x), 0, request, 14, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(Rotation.y), 0, request, 18, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(Rotation.z), 0, request, 22, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(Rotation.w), 0, request, 26, sizeof(float));
            return request;
        }

        public static int Size => 1 + 1 + sizeof(float) * 3 + sizeof(float) * 4;
    }
}