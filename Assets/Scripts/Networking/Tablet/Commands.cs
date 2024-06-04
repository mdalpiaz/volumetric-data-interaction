#nullable enable

using System;

namespace Networking.Tablet
{
    public interface ICommand
    {
        byte[] ToByteArray();
    }

    public record MenuModeCommand(MenuMode Mode) : ICommand
    {
        public byte[] ToByteArray()
        {
            var buffer = new byte[Size];
            buffer[0] = Categories.MenuMode;
            buffer[1] = (byte)Mode;
            return buffer;
        }

        public static int Size => 1 + sizeof(bool);

        public static MenuModeCommand FromByteArray(byte[] buffer)
        {
            return new MenuModeCommand((MenuMode)buffer[1]);
        }
    }

    public record SwipeCommand(bool Inward, float EndPointX, float EndPointY, float Angle) : ICommand
    {
        public byte[] ToByteArray()
        {
            var buffer = new byte[Size];
            buffer[0] = Categories.Swipe;
            buffer[1] = BitConverter.GetBytes(Inward)[0];
            Buffer.BlockCopy(BitConverter.GetBytes(EndPointX), 0, buffer, 2, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(EndPointY), 0, buffer, 6, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(Angle), 0, buffer, 10, sizeof(float));
            return buffer;
        }

        public static int Size => 1 + sizeof(bool) + sizeof(float) * 3;

        public static SwipeCommand FromByteArray(byte[] buffer)
        {
            return new SwipeCommand(
                BitConverter.ToBoolean(buffer, 1),
                BitConverter.ToSingle(buffer, 2),
                BitConverter.ToSingle(buffer, 6),
                BitConverter.ToSingle(buffer, 10));
        }
    }

    //public record ScaleCommand(float Scale) : ICommand
    //{
    //    public byte[] ToByteArray()
    //    {
    //        var buffer = new byte[Size];
    //        buffer[0] = Categories.Scale;
    //        Buffer.BlockCopy(BitConverter.GetBytes(Scale), 0, buffer, 1, sizeof(float));
    //        return buffer;
    //    }

    //    public static int Size => 1 + sizeof(float);

    //    public static ScaleCommand FromByteArray(byte[] buffer)
    //    {
    //        return new ScaleCommand(BitConverter.ToSingle(buffer, 1));
    //    }
    //}

    public record RotateCommand(float Rotation) : ICommand
    {
        public byte[] ToByteArray()
        {
            var buffer = new byte[Size];
            buffer[0] = Categories.Rotate;
            Buffer.BlockCopy(BitConverter.GetBytes(Rotation), 0, buffer, 1, sizeof(float));
            return buffer;
        }

        public static int Size => 1 + sizeof(float);

        public static RotateCommand FromByteArray(byte[] buffer)
        {
            return new RotateCommand(BitConverter.ToSingle(buffer, 1));
        }
    }

    public record TiltCommand(bool IsLeft) : ICommand
    {
        public byte[] ToByteArray()
        {
            var buffer = new byte[Size];
            buffer[0] = Categories.Tilt;
            buffer[1] = BitConverter.GetBytes(IsLeft)[0];
            return buffer;
        }

        public static int Size => 1 + sizeof(bool);

        public static TiltCommand FromByteArray(byte[] buffer)
        {
            return new TiltCommand(BitConverter.ToBoolean(buffer, 1));
        }
    }

    public record ShakeCommand(int Count) : ICommand
    {
        public byte[] ToByteArray()
        {
            var buffer = new byte[Size];
            buffer[0] = Categories.Shake;
            Buffer.BlockCopy(BitConverter.GetBytes(Count), 0, buffer, 1, sizeof(int));
            return buffer;
        }

        public static int Size => 1 + sizeof(int);

        public static ShakeCommand FromByteArray(byte[] buffer)
        {
            return new ShakeCommand(BitConverter.ToInt32(buffer, 1));
        }
    }

    public record TapCommand(TapType Type, float X, float Y) : ICommand
    {
        public byte[] ToByteArray()
        {
            var buffer = new byte[Size];
            buffer[0] = Categories.Tap;
            buffer[1] = (byte)Type;
            Buffer.BlockCopy(BitConverter.GetBytes(X), 0, buffer, 2, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(Y), 0, buffer, 6, sizeof(float));
            return buffer;
        }

        public static int Size => 1 + sizeof(byte) + sizeof(float) + sizeof(float);

        public static TapCommand FromByteArray(byte[] buffer)
        {
            return new TapCommand(
                (TapType)buffer[1],
                BitConverter.ToSingle(buffer, 2),
                BitConverter.ToSingle(buffer, 6));
        }
    }

    // new commands
    public class ScaleCommand : ICommand
    {
        public float Scale { get; private set; }

        public ScaleCommand(float scale)
        {
            Scale = scale;
        }

        public ScaleCommand(byte[] buffer) : this(BitConverter.ToSingle(buffer, 1)) { }

        public byte[] ToByteArray()
        {
            var buffer = new byte[Size];
            buffer[0] = Categories.Scale;
            Buffer.BlockCopy(BitConverter.GetBytes(Scale), 0, buffer, 1, sizeof(float));
            return buffer;
        }

        public static int Size => 1 + sizeof(float);
    }
}