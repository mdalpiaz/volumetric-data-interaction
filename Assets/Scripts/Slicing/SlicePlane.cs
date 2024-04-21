#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
            sliceCoords = GetSliceCoordinates(plane, model, intersectionPoints);
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

            //var x = 0;
            for (var x = 0; x < sliceCoords.Width; x++)
            {
                for (var y = 0; y < sliceCoords.Height; y++)
                {
                    // get world position
                    var position = startPoint + sliceCoords.XSteps * x + sliceCoords.YSteps * y;

                    var index = model.WorldPositionToIndex(position);

                    // get image at index and then the pixel
                    //var pixel = model.GetPixel(xStep, yStep, zStep - 1, interpolationType); // z needs correction
                    var pixel = model.GetPixel(index, interpolationType);
                    resultImage.SetPixel(sliceCoords.Width - x - 1, y, pixel);
                }
            }

            resultImage.Apply();
            return resultImage;
        }
        
        private static SlicePlaneCoordinates? GetSliceCoordinates(Plane plane, Model.Model model, IReadOnlyList<Vector3> intersectionPoints)
        {
            // intersectionPoints are pre-sorted counter-clockwise
            if (intersectionPoints.Count == 3)
            {
                return GetSliceCoordinates3Points(model, intersectionPoints[0], intersectionPoints[1], intersectionPoints[2]);
            }
            else if (intersectionPoints.Count == 4)
            {
                return GetSliceCoordinates4Points(model, intersectionPoints[0], intersectionPoints[1], intersectionPoints[2]);
            }
            else if (intersectionPoints.Count == 6)
            {
                return GetSliceCoordinates6Points(model, intersectionPoints[0], intersectionPoints[1], intersectionPoints[2], intersectionPoints[3], intersectionPoints[4], intersectionPoints[5]);
            }
            else
            {
                Debug.LogError($"Can't create plane with {intersectionPoints.Count} points!");
                return null;
            }

            // get bottom left edge (get left-most point and bottom-most point and check where they meet on the plane)
            var forward = plane.normal;
            var rotation = Quaternion.LookRotation(forward);

            var up = rotation * Vector3.up;
            var down = rotation * Vector3.down;

            // yes, they are swapped!
            var left = rotation * Vector3.right;
            var right = rotation * Vector3.left;

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

            //var last = upperPoints.Last();
            //upperPoints = upperPoints.OrderBy(p => Vector3.Distance(last, p)).ToArray();
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

            Debug.Log($"Size: {model.Size}, Steps: {model.StepSize}");

        }

        private static SlicePlaneCoordinates GetSliceCoordinates3Points(Model.Model model, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // TODO convert to 4 point
            return null;
        }

        private static SlicePlaneCoordinates GetSliceCoordinates4Points(Model.Model model, Vector3 ul, Vector3 ll, Vector3 lr)
        {
            Debug.DrawLine(ul, ll, Color.red, 120);
            Debug.DrawLine(ll, lr, Color.red, 120);

            var diffHeight = ul - ll;
            var diffXZ = lr - ll;

            // this is for calculating steps for height
            var ySteps = Mathf.RoundToInt(diffHeight.y / model.StepSize.y);    // Math.Abs is not needed, ySteps is ALWAYS from bottom to top

            Debug.DrawLine(ll, new() { x = ll.x, y = ll.y + diffHeight.y, z = ll.z }, Color.yellow, 120);

            var forwardStepsX = Math.Abs(Mathf.RoundToInt(diffHeight.x / model.StepSize.x));
            var forwardStepsZ = Math.Abs(Mathf.RoundToInt(diffHeight.z / model.StepSize.z));

            // this is for calculating steps for width
            var xSteps = Mathf.RoundToInt(diffXZ.x / model.StepSize.x);
            var zSteps = Mathf.RoundToInt(diffXZ.z / model.StepSize.z);

            var height = Math.Max(Math.Max(ySteps, forwardStepsX), forwardStepsZ);
            var width = Math.Max(xSteps, zSteps);

            // 3) we get the step size using the edge points and width and height
            var textureStepX = (lr - ll) / width;
            var textureStepY = (ul - ll) / height;

            //Debug.DrawRay(ul, Vector3.up, Color.red, 120);
            //Debug.DrawRay(ll, Vector3.up, Color.green, 120);
            //Debug.DrawRay(lr, Vector3.up, Color.blue, 120);

            return new SlicePlaneCoordinates(width, height, ll, textureStepX, textureStepY);
        }

        private static SlicePlaneCoordinates GetSliceCoordinates6Points(Model.Model model, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 p5, Vector3 p6)
        {
            // TODO convert to 4 point
            return null;
        }

        public static Mesh? CreateIntersectingMesh(Vector3[] points)
        {
            // cube cross-section has very specific cuts
            // we need to construct the smallest rectangle with all the points on the corner
            // and the up vector can only move up and rotate down by 90 degrees.
            // it can NOT be rotated otherwise! (no roll, only pitch and yaw)

            Debug.Log($"Intersection points: {points.Length}");
            if (points.Length == 3)
            {
                //Debug.DrawRay(points[0], Vector3.forward, Color.blue, 120);
                //Debug.DrawRay(points[1], Vector3.forward, Color.magenta, 120);
                //Debug.DrawRay(points[2], Vector3.forward, Color.red, 120);

                return new Mesh
                {
                    vertices = points,
                    triangles = new int[] { 0, 2, 1 },
                    normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back },
                    uv = new Vector2[] { Vector2.zero, Vector2.right, Vector2.up }
                };
            }
            if (points.Length == 4)
            {
                //Debug.DrawRay(points[0], Vector3.forward, Color.blue, 120);
                //Debug.DrawRay(points[1], Vector3.forward, Color.green, 120);
                //Debug.DrawRay(points[2], Vector3.forward, Color.yellow, 120);
                //Debug.DrawRay(points[3], Vector3.forward, Color.red, 120);

                return new Mesh
                {
                    vertices = points,
                    triangles = new int[] { 0, 2, 1, 0, 3, 2,   // mesh faces in both directions
                        1, 2, 0, 2, 3, 0 },
                    normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back },
                    uv = new Vector2[] { Vector2.up, Vector2.zero, Vector2.right, Vector2.one }
                };
            }
            if (points.Length == 6)
            {
                // ordering triangles the right way now gets much harder
                //Debug.DrawRay(points[0], Vector3.forward, Color.magenta, 120);
                //Debug.DrawRay(points[1], Vector3.forward, Color.blue, 120);
                //Debug.DrawRay(points[2], Vector3.forward, Color.green, 120);
                //Debug.DrawRay(points[3], Vector3.forward, Color.yellow, 120);
                //Debug.DrawRay(points[4], Vector3.forward, new Color(1.0f, 0.65f, 0.0f, 1.0f), 120);
                //Debug.DrawRay(points[5], Vector3.forward, Color.red, 120);

                return new Mesh
                {
                    vertices = points,
                    triangles = new int[] { 0, 2, 1, 0, 5, 2, 5, 3, 2, 5, 4, 3 },
                    normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back, Vector3.back, Vector3.back },
                    uv = new Vector2[] { }
                };
            }
            return null;
        }

        /// <summary>
        /// Get all intersection points of the slicer. The points are sorted by starting at the top and moving counter-clockwise around the center.
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="model"></param>
        /// <param name="slicerPosition"></param>
        /// <param name="slicerRotation"></param>
        /// <returns></returns>
        public static Vector3[] GetIntersectionPoints(out Plane plane, Model.Model model, Vector3 slicerPosition, Quaternion slicerRotation)
        {
            var points = GetIntersectionPoints_internal(out plane, model, slicerPosition, slicerRotation).ToArray();

            // we need to sort the points by angle, so that the mesh later on will be visible
            // to find the right order of the points
            // we can find the middle point and then calculate the angle between all points
            var minX = points.Min(p => p.x);
            var maxX = points.Max(p => p.x);
            var minY = points.Min(p => p.y);
            var maxY = points.Max(p => p.y);
            var minZ = points.Min(p => p.z);
            var maxZ = points.Max(p => p.z);

            var middle = new Vector3
            {
                x = (minX + maxX) / 2,
                y = (minY + maxY) / 2,
                z = (minZ + maxZ) / 2
            };

            var rotation = Quaternion.LookRotation(plane.normal);
            var slicerUp = rotation * Vector3.up;
            var slicerLeft = rotation * Vector3.left;

            var pointsInQuadrants = points
                .Select(p => (p, Vector3.Normalize(p - middle)))
                .Select(p => (p.p, Vector3.Dot(slicerUp, p.Item2), Vector3.Dot(slicerLeft, p.Item2)))
                .ToArray();

            // the quadrants go: top left, bottom left, bottom right, top right
            var q1 = pointsInQuadrants.Where(p => p is { Item2: >= 0, Item3: >= 0 }).OrderByDescending(p => p.Item2);
            var q2 = pointsInQuadrants.Where(p => p is { Item2: < 0, Item3: >= 0 }).OrderByDescending(p => p.Item2);
            var q3 = pointsInQuadrants.Where(p => p is { Item2: < 0, Item3: < 0 }).OrderBy(p => p.Item2);
            var q4 = pointsInQuadrants.Where(p => p is { Item2: >= 0, Item3: < 0 }).OrderBy(p => p.Item2);

            return q1
                .Concat(q2)
                .Concat(q3)
                .Concat(q4)
                .Select(p => p.p)
                .ToArray();
        }

        /// <summary>
        /// Tests all edges for cuts and returns them.
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="model"></param>
        /// <param name="slicerPosition"></param>
        /// <param name="slicerRotation"></param>
        /// <returns></returns>
        private static List<Vector3> GetIntersectionPoints_internal(out Plane plane, Model.Model model, Vector3 slicerPosition, Quaternion slicerRotation)
        {
            static bool HitCheck(out Vector3 hitPoint, Plane plane, Ray ray, float maxDistance)
            {
                var result = plane.Raycast(ray, out var distance);
                if (!(!result && distance == 0) &&  // false AND 0 means plane and raycast are parallel
                    ((distance >= 0 && distance <= maxDistance) ||
                    (distance < 0 && distance >= maxDistance)))
                {
                    hitPoint = ray.GetPoint(distance);
                    return true;
                }
                hitPoint = Vector3.zero;
                return false;
            }

            var list = new List<Vector3>(6);
            var mt = model.transform;
            // TODO why size?
            var size = mt.InverseTransformVector(model.Size);

            //var forward = mt.forward;
            //var down = mt.down();
            //var right = mt.right;

            var forward = Vector3.forward;
            var down = Vector3.down;
            var right = Vector3.right;

            // this is the normal of the slicer
            var normalVec = slicerRotation * Vector3.back;

            normalVec = mt.InverseTransformVector(normalVec);
            var localPosition = mt.InverseTransformPoint(slicerPosition);

            // slicerPosition, because we can give it ANY point that is on the plane, and it sets itself up automatically
            plane = new Plane(normalVec, localPosition);

            // test Z axis (front - back)
            var ray = new Ray(model.TopFrontLeftCorner, forward);
            if (HitCheck(out var point, plane, ray, size.z))
            {
                list.Add(point);
            }

            ray = new Ray(model.TopFrontRightCorner, forward);
            if (HitCheck(out point, plane, ray, size.z))
            {
                list.Add(point);
            }

            ray = new Ray(model.BottomFrontLeftCorner, forward);
            if (HitCheck(out point, plane, ray, size.z))
            {
                list.Add(point);
            }

            ray = new Ray(model.BottomFrontRightCorner, forward);
            if (HitCheck(out point, plane, ray, size.z))
            {
                list.Add(point);
            }

            // test Y axis (top - bottom)
            ray = new Ray(model.TopBackLeftCorner, down);
            if (HitCheck(out point, plane, ray, size.y))
            {
                list.Add(point);
            }

            ray = new Ray(model.TopFrontLeftCorner, down);
            if (HitCheck(out point, plane, ray, size.y))
            {
                list.Add(point);
            }

            ray = new Ray(model.TopFrontRightCorner, down);
            if (HitCheck(out point, plane, ray, size.y))
            {
                list.Add(point);
            }

            ray = new Ray(model.TopBackRightCorner, down);
            if (HitCheck(out point, plane, ray, size.y))
            {
                list.Add(point);
            }

            // test X axis (left - right)
            ray = new Ray(model.TopFrontLeftCorner, right);
            if (HitCheck(out point, plane, ray, size.x))
            {
                list.Add(point);
            }

            ray = new Ray(model.BottomFrontLeftCorner, right);
            if (HitCheck(out point, plane, ray, size.x))
            {
                list.Add(point);
            }

            ray = new Ray(model.TopBackLeftCorner, right);
            if (HitCheck(out point, plane, ray, size.x))
            {
                list.Add(point);
            }

            ray = new Ray(model.BottomBackLeftCorner, right);
            if (HitCheck(out point, plane, ray, size.x))
            {
                list.Add(point);
            }

            return list;
        }
    }
}