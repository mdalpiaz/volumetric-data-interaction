#nullable enable

using System.Buffers.Binary;
using System;
using UnityEngine;
using System.Text;
using Helper;

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

    public record ClientLoginResponse(ulong ID) : ICommand
    {
        public byte[] ToByteArray()
        {
            var request = new byte[Size];
            request[0] = Categories.Client.Value;
            BinaryPrimitives.WriteUInt64BigEndian(request.AsSpan(1), ID);
            return request;
        }

        public static int Size => 1 + sizeof(ulong);

        public static ClientLoginResponse FromByteArray(byte[] buffer)
        {
            return new ClientLoginResponse(BinaryPrimitives.ReadUInt64BigEndian(buffer.AsSpan(1)));
        }
    }

    public record LoadDataset(string Name) : ICommand
    {
        public byte[] ToByteArray()
        {
            var bytes = Encoding.UTF8.GetBytes(Name);
            var record = new byte[1 + 1 + sizeof(uint) + bytes.Length];
            record[0] = Categories.Datasets.Value;
            record[1] = Categories.Datasets.LoadDataset;
            BinaryPrimitives.WriteUInt32BigEndian(record.AsSpan(2), (uint)bytes.Length);
            Buffer.BlockCopy(bytes, 0, record, 6, bytes.Length);
            return record;
        }

        public static LoadDataset FromByteArray(byte[] buffer)
        {
            var size = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(2, sizeof(uint)));
            var convertedSize = (int)size;
            if (size != convertedSize)
            {
                throw new ArgumentException($"Conversion from uint to int resulted in different sizes! {size} != {convertedSize}");
            }
            var name = Encoding.UTF8.GetString(buffer, 2 + sizeof(uint), convertedSize);
            return new LoadDataset(name);
        }
    }

    public record SetObjectTranslation(ulong ID, Vector3 Translation) : ICommand
    {
        public byte[] ToByteArray()
        {
            throw new NotImplementedException();
        }

        public static int Size => 1 + 1 + sizeof(ulong) + sizeof(float) * 3;

        public static SetObjectTranslation FromByteArray(byte[] buffer)
        {
            var id = BinaryPrimitives.ReadUInt64BigEndian(buffer.AsSpan(2, sizeof(ulong)));
            var translation = new Vector3(
                BitConverter.ToSingle(buffer.AsSpan(10, sizeof(float))),
                BitConverter.ToSingle(buffer.AsSpan(14, sizeof(float))),
                BitConverter.ToSingle(buffer.AsSpan(18, sizeof(float))));
            return new SetObjectTranslation(id, translation);
        }
    }

    public record SetObjectScale(ulong ID, Vector3 Scale) : ICommand
    {
        public byte[] ToByteArray()
        {
            throw new NotImplementedException();
        }

        public static int Size => 1 + 1 + sizeof(ulong) + sizeof(float) * 3;

        public static SetObjectScale FromByteArray(byte[] buffer)
        {
            var id = BinaryPrimitives.ReadUInt64BigEndian(buffer.AsSpan(2, sizeof(ulong)));
            var scale = new Vector3(
                BitConverter.ToSingle(buffer.AsSpan(10, sizeof(float))),
                BitConverter.ToSingle(buffer.AsSpan(14, sizeof(float))),
                BitConverter.ToSingle(buffer.AsSpan(18, sizeof(float))));
            return new SetObjectScale(id, scale);
        }
    }

    public record SetObjectRotationQuaternion(ulong ID, Quaternion Rotation) : ICommand
    {
        public byte[] ToByteArray()
        {
            throw new NotImplementedException();
        }

        public static int Size => 1 + 1 + sizeof(ulong) + sizeof(float) * 4;

        public static SetObjectRotationQuaternion FromByteArray(byte[] buffer)
        {
            var id = BinaryPrimitives.ReadUInt64BigEndian(buffer.AsSpan(2, sizeof(ulong)));
            var rotation = new Quaternion(
                BitConverter.ToSingle(buffer.AsSpan(10, sizeof(float))),
                BitConverter.ToSingle(buffer.AsSpan(14, sizeof(float))),
                BitConverter.ToSingle(buffer.AsSpan(18, sizeof(float))),
                BitConverter.ToSingle(buffer.AsSpan(22, sizeof(float))));
            return new SetObjectRotationQuaternion(id, rotation);
        }
    }

    public record SetObjectRotationEuler(ulong ID, Axis Axis, float Value) : ICommand
    {
        public byte[] ToByteArray()
        {
            throw new NotImplementedException();
        }

        public static int Size => 1 + 1 + sizeof(ulong) + sizeof(Axis) + sizeof(float);

        public static SetObjectRotationEuler FromByteArray(byte[] buffer)
        {
            var id = BinaryPrimitives.ReadUInt64BigEndian(buffer.AsSpan(2, sizeof(ulong)));
            var axis = (Axis)buffer[10];
            var value = BitConverter.ToSingle(buffer.AsSpan(11, sizeof(float)));
            return new SetObjectRotationEuler(id, axis, value);
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