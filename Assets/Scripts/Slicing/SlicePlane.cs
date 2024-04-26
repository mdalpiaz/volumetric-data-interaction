﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Slicing
{
    public static class SlicePlane
    {
        /// <summary>
        /// Get all intersection points of the slicer. The points are sorted by starting at the top and moving counter-clockwise around the center.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="slicerPosition"></param>
        /// <param name="slicerRotation"></param>
        /// <returns></returns>
        public static IntersectionPoints? GetIntersectionPoints(Model.Model model, Vector3 slicerPosition, Quaternion slicerRotation)
        {
            Debug.DrawLine(model.BottomFrontLeftCorner, model.BottomBackLeftCorner, Color.black);
            Debug.DrawLine(model.BottomFrontRightCorner, model.BottomBackRightCorner, Color.black);
            Debug.DrawLine(model.TopFrontLeftCorner, model.TopBackLeftCorner, Color.black);
            Debug.DrawLine(model.TopFrontRightCorner, model.TopBackRightCorner, Color.black);

            Debug.DrawLine(model.BottomBackLeftCorner, model.TopBackLeftCorner, Color.black);
            Debug.DrawLine(model.BottomBackRightCorner, model.TopBackRightCorner, Color.black);

            Debug.DrawLine(model.BottomBackLeftCorner, model.BottomBackRightCorner, Color.black);
            Debug.DrawLine(model.TopBackLeftCorner, model.TopBackRightCorner, Color.black);

            // this is the normal of the slicer
            var normalVector = model.transform.InverseTransformVector(slicerRotation * Vector3.back);
            var localPosition = model.transform.InverseTransformPoint(slicerPosition);
            var plane = new Plane(normalVector, localPosition);
            
            var points = GetIntersectionPoints_internal(model, plane).ToList();
            if (points.Count < 3)
            {
                Debug.LogError("Cannot create proper intersection with less than 3 points!");
                return null;
            }
            
            // we only take the plane if it faces up (normal points down), else we just flip it
            // the rotation section is CORRECT
            var normal = plane.normal;
            if (normal.y > 0)
            {
                normal = plane.flipped.normal;
            }
            
            var rotation = Quaternion.LookRotation(normal);
            //var slicerUp = rotation * Vector3.up;
            var slicerLeft = rotation * Vector3.left;
            
            if (points.Count != 4)
            {
                points = ConvertTo4Points(rotation, points).ToList();
            }

            // points from here are always 4

            // we need to sort the points by angle, so that the mesh later on will be visible
            // to find the right order of the points
            // we can find the middle point and then calculate the dot product between all points
            //var middle = GetCenterPoint(points);

            var heightSortedPoints = points.OrderByDescending(p => p.y);

            var topPoints = heightSortedPoints.Take(2).ToList();
            var bottomPoints = heightSortedPoints.Reverse().Take(2).ToList();

            Debug.DrawLine(topPoints[0], topPoints[1], Color.magenta);
            Debug.DrawLine(bottomPoints[0], bottomPoints[1], Color.yellow);

            var testPlane = new Plane(slicerLeft, 0);
            testPlane.Raycast(new Ray(topPoints[0], slicerLeft), out var distance0);
            testPlane.Raycast(new Ray(topPoints[1], slicerLeft), out var distance1);
            Vector3 topLeft;
            Vector3 topRight;
            if (distance0 > distance1)
            {
                topLeft = topPoints[0];
                topRight = topPoints[1];
            }
            else
            {
                topLeft = topPoints[1];
                topRight = topPoints[0];
            }

            testPlane.Raycast(new Ray(bottomPoints[0], slicerLeft), out distance0);
            testPlane.Raycast(new Ray(bottomPoints[1], slicerLeft), out distance1);
            Vector3 bottomLeft;
            Vector3 bottomRight;
            if (distance0 > distance1)
            {
                bottomLeft = bottomPoints[0];
                bottomRight = bottomPoints[1];
            }
            else
            {
                bottomLeft = bottomPoints[1];
                bottomRight = bottomPoints[0];
            }

            return new IntersectionPoints
            {
                UpperLeft = topLeft,
                LowerLeft = bottomLeft,
                LowerRight = bottomRight,
                UpperRight = topRight
            };

            //var pointsInQuadrants = points
            //    .Select(p => (p, Vector3.Normalize(p - middle)))
            //    .Select(p => (p.p, Vector3.Dot(slicerUp, p.Item2), Vector3.Dot(slicerLeft, p.Item2)))
            //    .ToList();

            //// the quadrants go: top left, bottom left, bottom right, top right
            //var q1 = pointsInQuadrants.Where(p => p is { Item2: >= 0, Item3: >= 0 }).OrderByDescending(p => p.Item2);
            //var q2 = pointsInQuadrants.Where(p => p is { Item2: < 0, Item3: >= 0 }).OrderByDescending(p => p.Item2);
            //var q3 = pointsInQuadrants.Where(p => p is { Item2: < 0, Item3: < 0 }).OrderBy(p => p.Item2);
            //var q4 = pointsInQuadrants.Where(p => p is { Item2: >= 0, Item3: < 0 }).OrderBy(p => p.Item2);

            //var newPoints = q1
            //    .Concat(q2)
            //    .Concat(q3)
            //    .Concat(q4)
            //    .Select(p => p.p)
            //    .ToList();

            //return new IntersectionPoints(newPoints[0], newPoints[1], newPoints[2], newPoints[3]);
        }
        
        public static Dimension? GetTextureDimension(Model.Model model, IntersectionPoints points)
        {
            var ul = points.UpperLeft;
            var ll = points.LowerLeft;
            var lr = points.LowerRight;
            
            var diffHeight = ul - ll;
            var diffXZ = lr - ll;

            // this is for calculating steps for height
            var ySteps = Mathf.RoundToInt(diffHeight.y / model.StepSize.y);    // Math.Abs is not needed, ySteps is ALWAYS from bottom to top

            var forwardStepsX = Mathf.RoundToInt(diffHeight.x / model.StepSize.x);
            var forwardStepsZ = Mathf.RoundToInt(diffHeight.z / model.StepSize.z);

            // this is for calculating steps for width
            var xSteps = Mathf.RoundToInt(diffXZ.x / model.StepSize.x);
            var zSteps = Mathf.RoundToInt(diffXZ.z / model.StepSize.z);

            var height = Math.Max(Math.Max(Math.Abs(ySteps), Math.Abs(forwardStepsX)), Math.Abs(forwardStepsZ));
            var width = Math.Max(Math.Abs(xSteps), Math.Abs(zSteps));

            //var height = ySteps;
            //if (Math.Abs(forwardStepsX) > Math.Abs(height))
            //{
            //    height = forwardStepsX;
            //}

            //if (Math.Abs(forwardStepsZ) > Math.Abs(height))
            //{
            //    height = forwardStepsZ;
            //}

            //var width = xSteps;
            //if (Math.Abs(zSteps) > Math.Abs(width))
            //{
            //    width = zSteps;
            //}
            
            // 3) we get the step size using the edge points and width and height
            var textureStepX = diffXZ / width;
            var textureStepY = diffHeight / height;

            Debug.Log($"Width: {width}, Height: {height}");

            if (width == 0 || height == 0)
            {
                return null;
            }

            //if (width < 0)
            //{
            //    width = Math.Abs(width);
            //    textureStepX *= -1;
            //}

            //if (height < 0)
            //{
            //    height = Math.Abs(height);
            //    textureStepY *= -1;
            //}

            return new Dimension { Width = width, Height = height };

            //return new SlicePlaneCoordinates(width, height, ll, textureStepX, textureStepY);
        }

        public static Texture2D CreateSliceTexture(Model.Model model, Dimension dimension, IntersectionPoints points, InterpolationType interpolationType = InterpolationType.Nearest)
        {
            var resultImage = new Texture2D(dimension.Width, dimension.Height);

            var adjustedXStep = (points.LowerRight - points.LowerLeft) / dimension.Width;// * (dimension.Width > 0 ? 1 : -1);
            var adjustedYStep = (points.UpperLeft - points.LowerLeft) / dimension.Height;// * (dimension.Height > 0 ? 1 : -1);

            var startPoint = points.LowerLeft;
            if (dimension.Width < 0)
            {
                startPoint += dimension.Width * adjustedXStep;
            }
            if (dimension.Height < 0)
            {
                startPoint += dimension.Height * adjustedYStep;
            }

            for (var x = 0; x < dimension.Width; x++)
            {
                //var y = 0;
                for (var y = 0; y < dimension.Height; y++)
                {
                    // get local position
                    var position = startPoint + adjustedXStep * x + adjustedYStep * y;

                    var index = model.LocalPositionToIndex(position);

                    //Debug.DrawRay(position, Vector3.forward, new Color(x / (float)Math.Abs(sliceCoords.Width), 0, 0));

                    // get image at index and then the pixel
                    var pixel = model.GetPixel(index, interpolationType);
                    var realX = dimension.Width > 0 ? dimension.Width - x - 1 : x;
                    resultImage.SetPixel(realX, y, pixel);
                }
            }
            
            resultImage.Apply();
            return resultImage;
        }
        
        public static Mesh CreateMesh(Model.Model model, IntersectionPoints points)
        {
            // convert to world coordinates
            var worldPoints = new IntersectionPoints
            {
                UpperLeft = model.transform.TransformPoint(points.UpperLeft),
                LowerLeft = model.transform.TransformPoint(points.LowerLeft),
                LowerRight = model.transform.TransformPoint(points.LowerRight),
                UpperRight = model.transform.TransformPoint(points.UpperRight)
            };

            var arr = new Vector3[4];
            arr[0] = worldPoints.UpperLeft;
            arr[1] = worldPoints.LowerLeft;
            arr[2] = worldPoints.LowerRight;
            arr[3] = worldPoints.UpperRight;
            
            return new Mesh
            {
                vertices = arr,
                triangles = new int[] { //0, 2, 1, 0, 3, 2,   // mesh faces in both directions
                    0, 1, 2, 0, 2, 3
                    },
                normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back },
                uv = new Vector2[] { Vector2.up, Vector2.zero, Vector2.right, Vector2.one }
            };
        }
        
        /// <summary>
        /// Tests all edges for cuts and returns them.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        private static IEnumerable<Vector3> GetIntersectionPoints_internal(Model.Model model, Plane plane)
        {
            var list = new List<Vector3>(6);
            var size = model.Size;

            // test Z axis (front - back)
            var ray = new Ray(model.TopFrontLeftCorner, Vector3.forward);
            if (HitCheck(out var point, plane, ray, size.z))
            {
                list.Add(point);
            }

            ray = new Ray(model.TopFrontRightCorner, Vector3.forward);
            if (HitCheck(out point, plane, ray, size.z))
            {
                list.Add(point);
            }

            ray = new Ray(model.BottomFrontLeftCorner, Vector3.forward);
            if (HitCheck(out point, plane, ray, size.z))
            {
                list.Add(point);
            }

            ray = new Ray(model.BottomFrontRightCorner, Vector3.forward);
            if (HitCheck(out point, plane, ray, size.z))
            {
                list.Add(point);
            }

            // test Y axis (top - bottom)
            ray = new Ray(model.TopBackLeftCorner, Vector3.down);
            if (HitCheck(out point, plane, ray, size.y))
            {
                list.Add(point);
            }

            ray = new Ray(model.TopFrontLeftCorner, Vector3.down);
            if (HitCheck(out point, plane, ray, size.y))
            {
                list.Add(point);
            }

            ray = new Ray(model.TopFrontRightCorner, Vector3.down);
            if (HitCheck(out point, plane, ray, size.y))
            {
                list.Add(point);
            }

            ray = new Ray(model.TopBackRightCorner, Vector3.down);
            if (HitCheck(out point, plane, ray, size.y))
            {
                list.Add(point);
            }

            // test X axis (left - right)
            ray = new Ray(model.TopFrontLeftCorner, Vector3.right);
            if (HitCheck(out point, plane, ray, size.x))
            {
                list.Add(point);
            }

            ray = new Ray(model.BottomFrontLeftCorner, Vector3.right);
            if (HitCheck(out point, plane, ray, size.x))
            {
                list.Add(point);
            }

            ray = new Ray(model.TopBackLeftCorner, Vector3.right);
            if (HitCheck(out point, plane, ray, size.x))
            {
                list.Add(point);
            }

            ray = new Ray(model.BottomBackLeftCorner, Vector3.right);
            if (HitCheck(out point, plane, ray, size.x))
            {
                list.Add(point);
            }

            return list;

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
        }

        private static Vector3 GetCenterPoint(IReadOnlyCollection<Vector3> points)
        {
            return points.Aggregate((p1, p2) => p1 + p2) / points.Count;
        }
        
        private static IEnumerable<Vector3> ConvertTo4Points(Quaternion planeRotation, IReadOnlyList<Vector3> points)
        {
            var left = planeRotation * Vector3.left;
            var right = planeRotation * Vector3.right;

            //for (var i = 0; i < points.Count; i++)
            //{
            //    Debug.DrawLine(points[i], points[(i + 1) % points.Count], Color.red);
            //}

            //Debug.DrawRay(Vector3.zero, left, Color.green);
            //Debug.DrawRay(Vector3.zero, right, Color.yellow);

            var plane = new Plane(right, points[0]);
            var sortedPoints = points.Select(p =>
                {
                    var ray = new Ray(p, left);
                    plane.Raycast(ray, out var d);
                    return (p, d);
                })
                .OrderBy(p => p.d)
                .Select(p => p.p)
                .ToList();

            var leftPoint = sortedPoints.First();
            var rightPoint = sortedPoints.Last();

            sortedPoints = points.OrderBy(p => p.y).ToList();
            var topPoint = sortedPoints.Last();
            var bottomPoint = sortedPoints.First();

            var corners = new Vector3[4];
            
            plane.SetNormalAndPosition(left, leftPoint);
            plane.Raycast(new Ray(topPoint, left), out var distance);
            corners[0] = topPoint + distance * left;

            plane.Raycast(new Ray(bottomPoint, left), out distance);
            corners[1] = bottomPoint + distance * left;

            plane.SetNormalAndPosition(right, rightPoint);
            plane.Raycast(new Ray(bottomPoint, right), out distance);
            corners[2] = bottomPoint + distance * right;

            plane.Raycast(new Ray(topPoint, right), out distance);
            corners[3] = topPoint + distance * right;

            return corners;
        }
    }
}