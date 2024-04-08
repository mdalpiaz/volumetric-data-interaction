using UnityEngine;

namespace Slicing
{
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
    }
}