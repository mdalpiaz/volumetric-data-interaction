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
        
        public static Texture2D CalculateIntersectionPlane(Model.Model model, SlicePlaneCoordinates slicePlaneCoordinates, Vector3? alternativeStartPoint = null, InterpolationType interpolationType = InterpolationType.Nearest)
        {
            var resultImage = new Texture2D(slicePlaneCoordinates.Width, slicePlaneCoordinates.Height);

            var startPoint = alternativeStartPoint ?? slicePlaneCoordinates.StartPoint;
            var currVector1 = startPoint;
            var currVector2 = startPoint;

            for (var w = 0; w < slicePlaneCoordinates.Width; w++)
            {
                currVector1.x = (int)Math.Round(startPoint.x + w * slicePlaneCoordinates.XSteps.x, 0);
                currVector1.y = (int)Math.Round(startPoint.y + w * slicePlaneCoordinates.XSteps.y, 0);
                currVector1.z = (int)Math.Round(startPoint.z + w * slicePlaneCoordinates.XSteps.z, 0);

                for (var h = 0; h < slicePlaneCoordinates.Height; h++)
                {
                    currVector2.x = (int)Math.Round(currVector1.x + h * slicePlaneCoordinates.YSteps.x, 0);
                    currVector2.y = (int)Math.Round(currVector1.y + h * slicePlaneCoordinates.YSteps.y, 0);
                    currVector2.z = (int)Math.Round(currVector1.z + h * slicePlaneCoordinates.YSteps.z, 0);

                    var croppedIndex = ValueCropper.CropIntVector(currVector2, model.CountVector);
                    var currBitmap = model.OriginalBitmap[croppedIndex.x];

                    // convert coordinates from top-left to bottom-left
                    var result = Interpolation.Interpolate(interpolationType, currBitmap, croppedIndex.z, currBitmap.height - croppedIndex.y);
                    
                    //if (alternativeStartPoint == null)
                    //{
                    //    result = result.MakeBlackTransparent();
                    //}
                    // flip the image
                    resultImage.SetPixel(w, slicePlaneCoordinates.Height - 1 - h, result);
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
            var width = 0;
            var height = 0;

            Debug.Log($"X: {model.XCount}, Y: {model.YCount}, Z: {model.ZCount}");

            var steps = Vector3.zero;
            steps.x = model.Size.x / model.XCount;
            steps.y = model.Size.y / model.YCount;
            steps.z = model.Size.z / model.ZCount;

            Debug.Log($"Size: {model.Size}, Steps: {steps}");

            var diff = upperRight - lowerLeft;
            var xSteps = (int)(diff.x / steps.x);
            var ySteps = (int)(diff.y / steps.y);
            var zSteps = (int)(diff.z / steps.z);

            Debug.Log($"Steps X: {xSteps}, Y: {ySteps}, Z: {zSteps}");

            // TODO only works, if top and bottom are touching the top and bottom of model
            // fully vertical slices don't work yet
            height = ySteps;
            width = xSteps > zSteps ? xSteps : zSteps;

            // TODO
            // 3) we get the step size using the edge points and width and height
            var textureStepX = (lowerRight - lowerLeft) / width;
            var textureStepY = (upperRight - lowerLeft) / height;

            var sliceCoords = new SlicePlaneCoordinates(width, height, lowerLeft, textureStepX, Vector3.zero);
            Debug.Log($"SliceCoords: {sliceCoords}");
            return sliceCoords;
        }

        private static SlicePlaneCoordinates? GetSliceCoordinates(Model.Model model, IReadOnlyList<Vector3> intersectionPoints)
        {
            if (intersectionPoints.Count < 3)
            {
                Debug.LogError("Can't create plane formula with less than 3 points!");
                return null;
            }

            // we don't need edge points, we already have them!
            var planeFormula = new PlaneFormula(intersectionPoints[0], intersectionPoints[1], intersectionPoints[2]);

            var edgePoints = CalculateEdgePoints(planeFormula, model.XCount, model.YCount, model.ZCount).ToList();

            if (edgePoints.Count < 3)
            {
                Debug.LogError("Cannot calculate a cutting plane with fewer than 3 coordinates");
                return null;
            }

            // TODO what is happening here?

            //edgePoints.ForEach(p => Debug.Log(p.ToString()));
            var startLeft = GetClosestPoint(edgePoints, intersectionPoints[2]);
            edgePoints.Remove(startLeft);
            var startRight = GetClosestPoint(edgePoints, intersectionPoints[3]);
            edgePoints.Remove(startRight);

            var p1 = startRight; 
            var p2 = edgePoints[1];

            var diff1 = p1 - startLeft;
            var diff2 = p2 - startLeft;
            var (newWidth, newHeight) = GetDimensionsSyncDifferences(ref diff1, ref diff2);

            var width = (int)Math.Round(newWidth, 0); // bigger image if angled -  CalculateAngledPlaneLength(p1 - startLeft, newWidth);
            var height = (int)Math.Round(newHeight, 0); // bigger image if angled - CalculateAngledPlaneLength(p2 - startLeft, newHeight);

            var xSteps = diff1 / width;
            var ySteps = diff2 / height;
            (xSteps, ySteps) = MinimiseSteps(xSteps, ySteps);

            return new SlicePlaneCoordinates(width, height, startLeft, xSteps, ySteps);
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