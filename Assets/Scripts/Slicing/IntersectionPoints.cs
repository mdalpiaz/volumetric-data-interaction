#nullable enable

using UnityEngine;

namespace Slicing
{
    public class IntersectionPoints
    {
        public Vector3 UpperLeft { get; set; }
        public Vector3 LowerLeft { get; set; }
        public Vector3 LowerRight { get; set; }
        public Vector3 UpperRight { get; set; }
    }
}