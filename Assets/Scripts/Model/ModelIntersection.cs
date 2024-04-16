﻿#nullable enable

using System.Collections.Generic;
using System.Linq;
using Extensions;
using UnityEngine;

namespace Model
{
    public static class ModelIntersection
    {
        /// <summary>
        /// https://catlikecoding.com/unity/tutorials/procedural-meshes/creating-a-mesh/
        /// </summary>
        // public static Mesh CreateIntersectingMesh(Model model, Vector3 slicerPosition, Quaternion slicerRotation)
        // {
        //     var points = GetIntersectionPoints(model, slicerPosition, slicerRotation).ToArray();
        //     return CreateIntersectingMesh(points);
        // }

        public static Mesh? CreateIntersectingMesh(Vector3[] points)
        {
            foreach (var p in points)
            {
                Debug.DrawRay(p, Vector3.forward, Color.red, 120);
            }

            // cube cross-section has very specific cuts
            // we need to construct the smallest rectangle with all the points on the corner
            // and the up vector can only move up and rotate down by 90 degrees.
            // it can NOT be rotated otherwise! (no roll, only pitch and yaw)

            // TODO
            Debug.Log($"Intersection points: {points.Length}");
            if (points.Length == 3)
            {
                return new Mesh
                {
                    vertices = points,
                    triangles = new int[] { 0, 2, 1 },
                    normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back },
                    uv = new Vector2[] { Vector2.zero, Vector2.right, Vector2.up }
                };
            }
            else if (points.Length == 4)
            {
                return new Mesh
                {
                    vertices = points,
                    triangles = new int[] { 0, 2, 1, 1, 2, 3 },
                    normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back },
                    uv = new Vector2[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one }
                };
            }
            else if (points.Length == 6)
            {
                // ordering triangles the right way now gets much harder
                return null;
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<Vector3> GetIntersectionPoints(Model model, Vector3 slicerPosition, Quaternion slicerRotation)
        {
            // test ALL edges for cuts and return them

            var mt = model.transform;
            // transform.position is NOT the centerpoint of the model!
            var center = mt.TransformPoint(model.BoxCollider.center);
            var size = model.Size;
            var extents = model.Extents;
            
            // this is the normal of the slicer
            var normalVec = slicerRotation * Vector3.back;
            
            // _slicerPosition, because we can give it ANY point on the plane, and it sets itself up automatically
            var plane = new Plane(normalVec, slicerPosition);
            
            // test Z axis (front - back)
            var topFrontLeft = center + mt.left() * extents.x + mt.up * extents.y + mt.forward * extents.z;
            var ray = new Ray(topFrontLeft, mt.backward());
            if (plane.Raycast(ray, out var distance) &&
                distance >= 0 && distance <= size.z)
            {
                yield return ray.GetPoint(distance);
            }
            
            var topFrontRight = center + mt.right * extents.x + mt.up * extents.y + mt.forward * extents.z;
            ray = new Ray(topFrontRight, mt.backward());
            if (plane.Raycast(ray, out distance) &&
                distance >= 0 && distance <= size.z)
            {
                yield return ray.GetPoint(distance);
            }
            
            var bottomFrontLeft = center + mt.left() * extents.x + mt.down() * extents.y + mt.forward * extents.z;
            ray = new Ray(bottomFrontLeft, mt.backward());
            if (plane.Raycast(ray, out distance) &&
                distance >= 0 && distance <= size.z)
            {
                yield return ray.GetPoint(distance);
            }
            
            var bottomFrontRight = center + mt.right * extents.x + mt.down() * extents.y + mt.forward * extents.z;
            ray = new Ray(bottomFrontRight, mt.backward());
            if (plane.Raycast(ray, out distance) &&
                distance >= 0 && distance <= size.z)
            {
                yield return ray.GetPoint(distance);
            }
            
            // test Y axis (top - bottom)
            var topBackLeft = center + mt.left() * extents.x + mt.up * extents.y + mt.backward() * extents.z;
            ray = new Ray(topBackLeft, mt.down());
            if (plane.Raycast(ray, out distance) &&
                distance >= 0 && distance <= size.y)
            {
                yield return ray.GetPoint(distance);
            }
            
            ray = new Ray(topFrontLeft, mt.down());
            if (plane.Raycast(ray, out distance) &&
                distance >= 0 && distance <= size.y)
            {
                yield return ray.GetPoint(distance);
            }
            
            ray = new Ray(topFrontRight, mt.down());
            if (plane.Raycast(ray, out distance) &&
                distance >= 0 && distance <= size.y)
            {
                yield return ray.GetPoint(distance);
            }
            
            var topBackRight = center + mt.right * extents.x + mt.up * extents.y + mt.backward() * extents.z;
            ray = new Ray(topBackRight, mt.down());
            if (plane.Raycast(ray, out distance) &&
                distance >= 0 && distance <= size.y)
            {
                yield return ray.GetPoint(distance);
            }

            // test X axis (left - right)
            ray = new Ray(topFrontLeft, mt.right);
            if (plane.Raycast(ray, out distance) &&
                distance >= 0 && distance <= size.x)
            {
                yield return ray.GetPoint(distance);
            }
            
            ray = new Ray(bottomFrontLeft, mt.right);
            if (plane.Raycast(ray, out distance) &&
                distance >= 0 && distance <= size.x)
            {
                yield return ray.GetPoint(distance);
            }
            
            ray = new Ray(topBackLeft, mt.right);
            if (plane.Raycast(ray, out distance) &&
                distance >= 0 && distance <= size.x)
            {
                yield return ray.GetPoint(distance);
            }
            
            var bottomBackLeft = center + mt.left() * extents.x + mt.down() * extents.y + mt.backward() * extents.z;
            ray = new Ray(bottomBackLeft, mt.right);
            if (plane.Raycast(ray, out distance) &&
                distance >= 0 && distance <= size.x)
            {
                yield return ray.GetPoint(distance);
            }
        }


        // public static IEnumerable<Vector3> GetNormalisedIntersectionPosition(Model model, Vector3 slicerPosition, Quaternion slicerRotation)
        // {
        //     return GetIntersectionPoints(model, slicerPosition, slicerRotation)
        //         .Select(p => model.transform.worldToLocalMatrix.MultiplyPoint(p))
        //         .Select(newPosition => newPosition + model.Extents)
        //         .Select(contact => PositionInModel(contact, model, model.Size));
        // }

        public static IEnumerable<Vector3> PointsToModelCoordinates(IEnumerable<Vector3> points, Model model)
        {
            foreach (var p in points)
            {
                var temp = model.transform.worldToLocalMatrix.MultiplyPoint(p);
                yield return PositionInModel(temp, model);
            }
        }

        private static Vector3 PositionInModel(Vector3 contact, Model model)
        {
            var xRelativePosition = (contact.z / model.Size.z) * model.XCount;
            var yRelativePosition = (contact.y / model.Size.y) * model.YCount;
            var zRelativePosition = (contact.x / model.Size.x) * model.ZCount;

            return new Vector3(
                Mathf.Round(xRelativePosition),
                Mathf.Round(yRelativePosition),
                Mathf.Round(zRelativePosition));
        }
    }
}