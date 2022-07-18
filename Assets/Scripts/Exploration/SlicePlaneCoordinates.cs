﻿using UnityEngine;

namespace Assets.Scripts.Exploration
{

    public class SlicePlaneCoordinates
    {
        public int Width;
        public int Height;
        public Vector3 StartPoint;
        public Vector3 XSteps;
        public Vector3 YSteps;

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