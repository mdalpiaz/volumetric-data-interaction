using UnityEngine;

namespace Slicing
{
    public class IntersectionPoints
    {
        public Vector3 UpperLeft { get; }
        public Vector3 LowerLeft { get; }
        public Vector3 LowerRight { get; }
        public Vector3 UpperRight { get; }

        public IntersectionPoints(Vector3 upperLeft, Vector3 lowerLeft, Vector3 lowerRight, Vector3 upperRight)
        {
            UpperLeft = upperLeft;
            LowerLeft = lowerLeft;
            LowerRight = lowerRight;
            UpperRight = upperRight;
        }
    }
}