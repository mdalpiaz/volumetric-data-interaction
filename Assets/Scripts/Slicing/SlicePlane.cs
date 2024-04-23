#nullable enable

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
            IReadOnlyList<Vector3> points = GetIntersectionPoints_internal(out var plane, model, slicerPosition, slicerRotation).ToList();
            if (points.Count < 3)
            {
                Debug.LogError($"Cannot create proper intersection with less than 3 points!");
                return null;
            }
            if (points.Count != 4)
            {
                points = ConvertTo4Points(points);
            }

            // we need to sort the points by angle, so that the mesh later on will be visible
            // to find the right order of the points
            // we can find the middle point and then calculate the angle between all points
            var middle = GetCenterPoint(points);

            var rotation = Quaternion.LookRotation(plane.normal);
            var slicerUp = rotation * Vector3.up;
            var slicerLeft = rotation * Vector3.left;

            var pointsInQuadrants = points
                .Select(p => (p, Vector3.Normalize(p - middle)))
                .Select(p => (p.p, Vector3.Dot(slicerUp, p.Item2), Vector3.Dot(slicerLeft, p.Item2)))
                .ToList();

            // the quadrants go: top left, bottom left, bottom right, top right
            var q1 = pointsInQuadrants.Where(p => p is { Item2: >= 0, Item3: >= 0 }).OrderByDescending(p => p.Item2);
            var q2 = pointsInQuadrants.Where(p => p is { Item2: < 0, Item3: >= 0 }).OrderByDescending(p => p.Item2);
            var q3 = pointsInQuadrants.Where(p => p is { Item2: < 0, Item3: < 0 }).OrderBy(p => p.Item2);
            var q4 = pointsInQuadrants.Where(p => p is { Item2: >= 0, Item3: < 0 }).OrderBy(p => p.Item2);

            var newPoints = q1
                .Concat(q2)
                .Concat(q3)
                .Concat(q4)
                .Select(p => p.p)
                .ToList();

            return new IntersectionPoints(newPoints[0], newPoints[1], newPoints[2], newPoints[3]);
        }
        
        public static SlicePlaneCoordinates CreateSlicePlaneCoordinates(Model.Model model, IntersectionPoints points)
        {
            Debug.DrawRay(points.LowerLeft, Vector3.forward / 4.0f, Color.yellow, 120);
            Debug.DrawRay(model.transform.TransformPoint(points.LowerLeft), Vector3.forward / 4.0f, Color.yellow, 120);
            
            Debug.DrawLine(points.UpperLeft, points.LowerLeft, Color.blue, 120);
            Debug.DrawLine(points.LowerLeft, points.LowerRight, Color.blue, 120);
            Debug.DrawLine(points.LowerRight, points.UpperRight, Color.blue, 120);
            
            Debug.DrawLine(model.transform.TransformPoint(points.UpperLeft), model.transform.TransformPoint(points.LowerLeft), Color.blue, 120);
            Debug.DrawLine(model.transform.TransformPoint(points.LowerLeft), model.transform.TransformPoint(points.LowerRight), Color.blue, 120);
            Debug.DrawLine(model.transform.TransformPoint(points.LowerRight), model.transform.TransformPoint(points.UpperRight), Color.blue, 120);
            
            var ul = points.UpperLeft;
            var ll = points.LowerLeft;
            var lr = points.LowerRight;
            
            // TODO this method needs to be reworked, as the coordinates are not quite right yet
            var diffHeight = ul - ll;
            var diffXZ = lr - ll;

            // this is for calculating steps for height
            var ySteps = Mathf.RoundToInt(diffHeight.y / model.StepSize.y);    // Math.Abs is not needed, ySteps is ALWAYS from bottom to top

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

            var sliceCoords =  new SlicePlaneCoordinates(width, height, ll, textureStepX, textureStepY);
            
            Debug.DrawLine(sliceCoords.StartPoint, sliceCoords.StartPoint + sliceCoords.XSteps * sliceCoords.Width, Color.green, 120);
            Debug.DrawLine(sliceCoords.StartPoint, sliceCoords.StartPoint + sliceCoords.YSteps * sliceCoords.Height, Color.green, 120);
            
            Debug.DrawLine(model.transform.TransformPoint(sliceCoords.StartPoint), model.transform.TransformPoint(sliceCoords.StartPoint + sliceCoords.XSteps * sliceCoords.Width), Color.green, 120);
            Debug.DrawLine(model.transform.TransformPoint(sliceCoords.StartPoint), model.transform.TransformPoint(sliceCoords.StartPoint + sliceCoords.YSteps * sliceCoords.Height), Color.green, 120);

            return sliceCoords;
        }

        public static Texture2D CreateSliceTexture(Model.Model model, SlicePlaneCoordinates sliceCoords, InterpolationType interpolationType = InterpolationType.Nearest)
        {
            var resultImage = new Texture2D(sliceCoords.Width, sliceCoords.Height);

            for (var x = 0; x < sliceCoords.Width; x++)
            {
                for (var y = 0; y < sliceCoords.Height; y++)
                {
                    // get world position
                    var position = sliceCoords.StartPoint + sliceCoords.XSteps * x + sliceCoords.YSteps * y;

                    var index = model.LocalPositionToIndex(position);

                    // get image at index and then the pixel
                    var pixel = model.GetPixel(index, interpolationType);
                    resultImage.SetPixel(x, y, pixel);
                }
            }

            resultImage.Apply();
            return resultImage;
        }
        
        public static Mesh CreateMesh(Model.Model model, IntersectionPoints points)
        {
            // convert to world coordinates
            var worldPoints = new IntersectionPoints(
                model.transform.TransformPoint(points.UpperLeft),
                model.transform.TransformPoint(points.LowerLeft),
                model.transform.TransformPoint(points.LowerRight),
                model.transform.TransformPoint(points.UpperRight));
            
            //Debug.DrawRay(points[0], Vector3.forward, Color.blue, 120);
            //Debug.DrawRay(points[1], Vector3.forward, Color.green, 120);
            //Debug.DrawRay(points[2], Vector3.forward, Color.yellow, 120);
            //Debug.DrawRay(points[3], Vector3.forward, Color.red, 120);

            var arr = new Vector3[4];
            arr[0] = worldPoints.UpperLeft;
            arr[1] = worldPoints.LowerLeft;
            arr[2] = worldPoints.LowerRight;
            arr[3] = worldPoints.UpperRight;
            
            return new Mesh
            {
                vertices = arr,
                triangles = new int[] { 0, 2, 1, 0, 3, 2,   // mesh faces in both directions
                    1, 2, 0, 2, 3, 0 },
                normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back },
                uv = new Vector2[] { Vector2.up, Vector2.zero, Vector2.right, Vector2.one }
            };
        }
        
        /// <summary>
        /// Tests all edges for cuts and returns them.
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="model"></param>
        /// <param name="slicerPosition"></param>
        /// <param name="slicerRotation"></param>
        /// <returns></returns>
        private static IEnumerable<Vector3> GetIntersectionPoints_internal(out Plane plane, Model.Model model, Vector3 slicerPosition, Quaternion slicerRotation)
        {
            var list = new List<Vector3>(6);
            var mt = model.transform;
            var size = model.Size;

            // this is the normal of the slicer
            var normalVec = slicerRotation * Vector3.back;

            normalVec = mt.InverseTransformVector(normalVec);
            var localPosition = mt.InverseTransformPoint(slicerPosition);

            Debug.DrawLine(model.BottomFrontLeftCorner, model.BottomBackLeftCorner, Color.yellow, 120);
            Debug.DrawLine(model.BottomBackLeftCorner, model.BottomBackRightCorner, Color.yellow, 120);
            Debug.DrawLine(model.BottomBackRightCorner, model.TopBackRightCorner, Color.yellow, 120);

            Debug.DrawLine(model.BottomFrontLeftCorner, model.BottomFrontLeftCorner + size, Color.red, 120);

            // slicerPosition, because we can give it ANY point that is on the plane, and it sets itself up automatically
            plane = new Plane(normalVec, localPosition);

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
        
        private static IReadOnlyList<Vector3> ConvertTo4Points(IReadOnlyList<Vector3> points)
        {
            var rotation = Quaternion.LookRotation(new Plane(points[0], points[1], points[2]).normal);
            var left = rotation * Vector3.left;
            var right = rotation * Vector3.right;
            
            Debug.DrawLine(points[0], points[1], Color.blue, 120);
            Debug.DrawLine(points[1], points[2], Color.blue, 120);
            Debug.DrawLine(points[2], points[0], Color.blue, 120);
            
            // Debug.DrawRay(middle, rotation * Vector3.up, Color.green, 120);
            // Debug.DrawRay(middle, rotation * Vector3.right, Color.yellow, 120);
            // Debug.DrawRay(middle, rotation * Vector3.forward, Color.red, 120);

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
            
            plane.SetNormalAndPosition(right, leftPoint);
            plane.Raycast(new Ray(topPoint, left), out var distance);
            corners[0] = topPoint + distance * left;

            plane.Raycast(new Ray(bottomPoint, left), out distance);
            corners[1] = bottomPoint + distance * left;

            plane.SetNormalAndPosition(left, rightPoint);
            plane.Raycast(new Ray(bottomPoint, right), out distance);
            corners[2] = bottomPoint + distance * right;

            plane.Raycast(new Ray(topPoint, right), out distance);
            corners[3] = topPoint + distance * right;
            
            return corners;
        }
    }
}