using UnityEngine;

namespace Slicing
{
    /// <summary>
    /// This class defines the parameters needed for creating the texture of the slice.
    /// Width and Height are the size in pixels of the texture.
    /// XStep and YStep is the size of ONE pixel-step in either X or Y direction ON THE TEXTURE. They are used to translate 2D coordinates of the texture into 3D coordinates.
    /// </summary>
    public class SlicePlaneCoordinates
    {
        public int Width { get; }
        public int Height { get; }
        public Vector3 StartPoint { get; set; }
        public Vector3 XStep { get; }
        public Vector3 YStep { get; }

        public SlicePlaneCoordinates(int width, int height, Vector3 startPoint, Vector3 xStep, Vector3 yStep)
        {
            Width = width;
            Height = height;
            StartPoint = startPoint;
            XStep = xStep;
            YStep = yStep;
        }

        public override string ToString()
        {
            return $"{nameof(Width)}: {Width}, {nameof(Height)}: {Height}, {nameof(StartPoint)}: {StartPoint}, {nameof(XStep)}: {XStep}, {nameof(YStep)}: {YStep}";
        }
    }
}