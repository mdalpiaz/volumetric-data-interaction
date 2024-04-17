using UnityEngine;

namespace Slicing
{
    /// <summary>
    /// This class defines the parameters needed for creating the texture of the slice.
    /// Width and Height are the size in pixels of the texture.
    /// XSteps and YSteps is the size of ONE step in either X or Y direction ON THE TEXTURE. They are used to translate 2D coordinates of the texture into 3D world coordinates.
    /// </summary>
    public class SlicePlaneCoordinates
    {
        public int Width { get; }
        public int Height { get; }
        public Vector3 StartPoint { get; set; }
        public Vector3 XSteps { get; }
        public Vector3 YSteps { get; }

        public SlicePlaneCoordinates(int width, int height, Vector3 startPoint, Vector3 xSteps, Vector3 ySteps)
        {
            Width = width;
            Height = height;
            StartPoint = startPoint;
            XSteps = xSteps;
            YSteps = ySteps;
        }

        public override string ToString()
        {
            return $"{nameof(Width)}: {Width}, {nameof(Height)}: {Height}, {nameof(StartPoint)}: {StartPoint}, {nameof(XSteps)}: {XSteps}, {nameof(YSteps)}: {YSteps}";
        }
    }
}