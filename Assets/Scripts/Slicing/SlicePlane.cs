#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Extensions;
using Helper;
using UnityEngine;

namespace Slicing
{
    public static class SlicePlane
    {
        public static bool CalculateIntersectionPlane(
            [NotNullWhen(true)] out SlicePlaneCoordinates? sliceCoords,
            [NotNullWhen(true)] out Texture2D? texture,
            Plane plane,
            Model.Model model, IReadOnlyList<Vector3> intersectionPoints,
            Vector3? alternativeStartPoint = null,
            InterpolationType interpolationType = InterpolationType.Nearest)
        {
            sliceCoords = GetSliceCoordinatesV2(plane, model, intersectionPoints);
            if (sliceCoords == null)
            {
                texture = null;
                return false;
            }

            texture = CalculateIntersectionPlane(model, sliceCoords, alternativeStartPoint, interpolationType);
            return true;
        }
        
        public static Texture2D CalculateIntersectionPlane(Model.Model model, SlicePlaneCoordinates sliceCoords, Vector3? alternativeStartPoint = null, InterpolationType interpolationType = InterpolationType.Nearest)
        {
            var resultImage = new Texture2D(sliceCoords.Width, sliceCoords.Height);

            var startPoint = alternativeStartPoint ?? sliceCoords.StartPoint;

            Debug.Log($"Startpoint: {startPoint}");
            Debug.DrawRay(startPoint, Vector3.down, Color.yellow, 120);
            Debug.Log($"Startpoint transformed to local: {model.transform.InverseTransformPoint(startPoint)}");

            for (var x = 0; x < sliceCoords.Width; x++)
            {
                for (var y = 0; y < sliceCoords.Height; y++)
                {
                    // get world position
                    var position = startPoint + sliceCoords.XSteps * x + sliceCoords.YSteps * y;

                    // convert position into index
                    var diff = position - startPoint;
                    var xStep = Mathf.RoundToInt(diff.x / (model.Size.x / model.XCount));
                    var yStep = Mathf.RoundToInt(diff.y / (model.Size.y / model.YCount));
                    var zStep = Mathf.RoundToInt(diff.z / (model.Size.z / model.ZCount));

                    //Debug.Log($"X: {xStep}, Y: {yStep}, Z: {zStep}");

                    // get image at index and then the pixel
                    var pixel = model.GetPixel(xStep, yStep, zStep, interpolationType);
                    resultImage.SetPixel(x, y, pixel);
                }
            }

            resultImage.Apply();
            return resultImage;
        }
        
        private static SlicePlaneCoordinates? GetSliceCoordinatesV2(Plane plane, Model.Model model, IReadOnlyList<Vector3> intersectionPoints)
        {
            if (intersectionPoints.Count < 3)
            {
                Debug.LogError("Can't create plane with less than 3 points!");
                return null;
            }

            // get bottom left edge (get left-most point and bottom-most point and check where they meet on the plane)
            var forward = plane.normal;
            var rotation = Quaternion.LookRotation(forward);

            var up = rotation * Vector3.up;
            var down = rotation * Vector3.down;

            // yes, they are swapped!
            //var left = rotation * Vector3.right;
            //var right = rotation * Vector3.left;

            // to get the left-most point the following algorithm is run:
            // construct a horizontal plane at to bottom-most point
            // for every intersection point, raycast with the down direction until the plane is hit (we get the distance to the bottom)
            // move all points onto the new plane
            // now get the left-most point

            // 1) get start point at the bottom left
            var minPoint = intersectionPoints.Select(p => p.y).Min();
            var lowerPlane = new Plane(Vector3.up, -minPoint);
            var lowerPoints = intersectionPoints.Select(p =>
            {
                var ray = new Ray(p, down);
                lowerPlane.Raycast(ray, out var distance);
                return p + down * distance;
            });

            // we take the first point as base measurement and compare with all other points
            var first = lowerPoints.First();
            lowerPoints = lowerPoints.OrderBy(p => Vector3.Distance(first, p)).ToArray();
            var lowerLeft = lowerPoints.First();
            var lowerRight = lowerPoints.Last();

            // 1.5) and also at the top right to calculate the difference
            var maxPoint = intersectionPoints.Select(p => p.y).Max();
            var upperPlane = new Plane(Vector3.up, -maxPoint);
            var upperPoints = intersectionPoints.Select(p =>
            {
                var ray = new Ray(p, up);
                upperPlane.Raycast(ray, out var distance);
                return p + up * distance;
            });

            var last = upperPoints.Last();
            upperPoints = upperPoints.OrderBy(p => Vector3.Distance(last, p)).ToArray();
            var upperLeft = upperPoints.Last();
            var upperRight = upperPoints.First();

            // TODO
            // 2) we get the width and height of the new texture
            // what we do is:
            // for width we only look at X and Z axis and we get the one with the most pixels
            // for height we DON'T look at the Y height, we compare the point of the lower points with the higher points and count pixels

            // we need to convert the world coordinates of the intersection points
            // to int-steps based on the model X/Y/Z-Counts
            Debug.Log($"X: {model.XCount}, Y: {model.YCount}, Z: {model.ZCount}");

            var steps = new Vector3
            {
                x = model.Size.x / model.XCount,
                y = model.Size.y / model.YCount,
                z = model.Size.z / model.ZCount
            };

            Debug.Log($"Size: {model.Size}, Steps: {steps}");

            var diffAll = upperRight - lowerLeft;
            var diffXZ = lowerRight - lowerLeft;
            //var xSteps = (int)(diffXZ.x / steps.x);
            //var ySteps = (int)(diffAll.y / steps.y);
            //var zSteps = (int)(diffXZ.z / steps.z);

            // this is for calculating steps for height
            var ySteps = Mathf.RoundToInt(diffAll.y / steps.y);
            //var forwardStepsX = Mathf.RoundToInt(diffAll.x / steps.x);
            //var forwardStepsZ = Mathf.RoundToInt(diffAll.z / steps.z);

            // this is for calculating steps for width
            var xSteps = Mathf.RoundToInt(diffXZ.x / steps.x);
            var zSteps = Mathf.RoundToInt(diffXZ.z / steps.z);

            //Debug.Log($"Steps X: {xSteps}, Y: {ySteps}, Z: {zSteps}");

            var height = ySteps;//Math.Max(Math.Max(ySteps, forwardStepsX), forwardStepsZ);
            var width = Math.Max(xSteps, zSteps);

            Debug.Log($"Height: {height}, Width: {width}");

            // TODO
            // 3) we get the step size using the edge points and width and height
            var textureStepX = (lowerRight - lowerLeft) / width;
            var textureStepY = (upperLeft - lowerLeft) / height;

            Debug.Log($"Texture Steps X: {textureStepX}, Y: {textureStepY}");

            var sliceCoords = new SlicePlaneCoordinates(width, height, lowerLeft, textureStepX, textureStepY);
            Debug.Log($"SliceCoords: {sliceCoords}");
            return sliceCoords;
        }
        
        private static IEnumerable<Vector3> CalculateEdgePoints(PlaneFormula planeFormula, int x, int y, int z)
        {
            var edgePoints = new List<Vector3>();

            edgePoints.AddIfNotNull(planeFormula.GetValidXVectorOnPlane(x, 0, 0));
            edgePoints.AddIfNotNull(planeFormula.GetValidXVectorOnPlane(x, y, 0));
            edgePoints.AddIfNotNull(planeFormula.GetValidXVectorOnPlane(x, 0, z));
            edgePoints.AddIfNotNull(planeFormula.GetValidXVectorOnPlane(x, y, z));

            edgePoints.AddIfNotNull(planeFormula.GetValidYVectorOnPlane(0, y, 0));
            edgePoints.AddIfNotNull(planeFormula.GetValidYVectorOnPlane(x, y, 0));
            edgePoints.AddIfNotNull(planeFormula.GetValidYVectorOnPlane(0, y, z));
            edgePoints.AddIfNotNull(planeFormula.GetValidYVectorOnPlane(x, y, z));

            edgePoints.AddIfNotNull(planeFormula.GetValidZVectorOnPlane(0, 0, z));
            edgePoints.AddIfNotNull(planeFormula.GetValidZVectorOnPlane(x, 0, z));
            edgePoints.AddIfNotNull(planeFormula.GetValidZVectorOnPlane(0, y, z));
            edgePoints.AddIfNotNull(planeFormula.GetValidZVectorOnPlane(x, y, z));

            return edgePoints;
        }
        
        /// <summary>
        /// Method to get height and width dynamically
        /// Cannot use the biggest differences as these can be from the same coordinates
        /// Need to choose two coordinate axis
        /// Additional to the max difference, the additional width/height from possible angles must be calculated
        /// For this the third axis (which is not height or width) is used
        /// </summary>
        private static (float max1, float max2) GetDimensionsSyncDifferences(ref Vector3 diffWidth, ref Vector3 diffHeight)
        {
            var listWidth = new List<float>() { diffWidth.x, diffWidth.y, diffWidth.z };
            var listHeight = new List<float>() { diffHeight.x, diffHeight.y, diffHeight.z };
            //var indexSum = 3;

            var maxWidthIndex = GetIndexOfAbsHigherValue(listWidth);
            var maxHeightIndex = GetIndexOfAbsHigherValue(listHeight);

            var width = listWidth[maxWidthIndex];
            var height = listHeight[maxHeightIndex];

            //var addIndex = (indexSum - maxWidthIndex - maxHeightIndex) % indexSum;
            //var addWidth = listWidth[addIndex];
            //var addHeight = listHeight[addIndex];

            var zeroVector = GetCustomZeroVector(maxWidthIndex);
            if (maxWidthIndex == maxHeightIndex) // cannot use same coordinate for step calculation as a 2d image has 2 coordinates
            {
                listWidth.RemoveAt(maxWidthIndex);
                listHeight.RemoveAt(maxHeightIndex);
                //indexSum = 1;

                maxWidthIndex = GetIndexOfAbsHigherValue(listWidth);
                maxHeightIndex = GetIndexOfAbsHigherValue(listHeight);
                var tempWidth = listWidth[maxWidthIndex];
                var tempHeight = listHeight[maxHeightIndex];

                if (Math.Abs(tempWidth) > Math.Abs(tempHeight))
                {
                    width = tempWidth;
                    diffWidth.x *= zeroVector.x;
                    diffWidth.y *= zeroVector.y;
                    diffWidth.z *= zeroVector.z;
                    //addIndex = indexSum - maxWidthIndex;
                }
                else
                {
                    height = tempHeight;
                    diffHeight.x *= zeroVector.x;
                    diffHeight.y *= zeroVector.y;
                    diffHeight.z *= zeroVector.z;
                    //addIndex = indexSum - maxHeightIndex;
                }

                //addHeight = listHeight[addIndex];
                //addWidth = listWidth[addIndex];
            }

            return (Math.Abs(width), Math.Abs(height));
            //return (Math.Abs(width) + Math.Abs(addWidth), Math.Abs(height) + Math.Abs(addHeight));
        }

        private static (Vector3, Vector3) MinimiseSteps(Vector3 widthSteps, Vector3 heightSteps)
        {
            widthSteps.x = Math.Abs(widthSteps.x) < Math.Abs(heightSteps.x) ? 0 : widthSteps.x;
            heightSteps.x = Math.Abs(heightSteps.x) <= Math.Abs(widthSteps.x) ? 0 : heightSteps.x;

            widthSteps.y = Math.Abs(widthSteps.y) < Math.Abs(heightSteps.y) ? 0 : widthSteps.y;
            heightSteps.y = Math.Abs(heightSteps.y) <= Math.Abs(widthSteps.y) ? 0 : heightSteps.y;

            widthSteps.z = Math.Abs(widthSteps.z) < Math.Abs(heightSteps.z) ? 0 : widthSteps.z;
            heightSteps.z = Math.Abs(heightSteps.z) <= Math.Abs(widthSteps.z) ? 0 : heightSteps.z;

            return (widthSteps, heightSteps);
        }

        private static Vector3 GetClosestPoint(IEnumerable<Vector3> edgePoints, Vector3 targetPoint)
        {
            return edgePoints
                .ToDictionary(p => p, p => Vector3.Distance(p, targetPoint))
                .OrderBy(p => p.Value)
                .First()
                .Key;
        }
        
        private static Vector3 GetCustomZeroVector(int zeroOnIndex) => new(
            zeroOnIndex == 0 ? 0 : 1,
            zeroOnIndex == 1 ? 0 : 1,
            zeroOnIndex == 2 ? 0 : 1);

        private static int GetIndexOfAbsHigherValue(IList<float> values)
        {
            var min = values.Min();
            var max = values.Max();
            return values.IndexOf(Mathf.Abs(min) > max ? min : max);
        }
    }
}