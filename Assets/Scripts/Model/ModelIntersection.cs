#nullable enable

using System.Collections.Generic;
using System.Linq;
using Extensions;
using UnityEngine;

namespace Model
{
    public class ModelIntersection
    {
        private readonly Model _model;
        private readonly Vector3 _slicerPosition;
        private readonly Quaternion _slicerRotation;
        private readonly Matrix4x4 _slicerMatrix;

        private readonly IEnumerable<Vector3> _planeMeshVertices;

        public ModelIntersection(Model model, Vector3 slicerPosition, Quaternion slicerRotation, Matrix4x4 slicerLocalToWorld, MeshFilter planeMeshFilter)
        {
            _model = model;
            _slicerPosition = slicerPosition;
            _slicerRotation = slicerRotation;
            _slicerMatrix = Matrix4x4.TRS(slicerPosition, slicerRotation, Vector3.one);
            
            var mesh = planeMeshFilter.sharedMesh;
            _planeMeshVertices = mesh.vertices.Select(v => slicerLocalToWorld.MultiplyPoint(v));
        }

        public IEnumerable<Vector3> GetNormalisedIntersectionPosition()
        {
            var intersectionPoints = GetIntersectionPoints();
            var halfColliderSize = _model.BoxCollider.size / 2;

            var normalisedPositions = intersectionPoints
                .Select(p => _model.transform.worldToLocalMatrix.MultiplyPoint(p))
                .Select(newPosition => GetNormalisedPosition(newPosition, halfColliderSize));

            return CalculatePositionWithinModel(normalisedPositions, _model, _model.BoxCollider.size);
        }
        
        /// <summary>
        /// https://catlikecoding.com/unity/tutorials/procedural-meshes/creating-a-mesh/
        /// </summary>
        public Mesh CreateIntersectingMesh()
        {
            var originalIntersectionPoints = GetIntersectionPoints();
            foreach (var p in originalIntersectionPoints)
            {
                Debug.DrawRay(p, Vector3.forward, Color.green, 120);
            }
            var intersectionPoints = GetBoundaryIntersections(originalIntersectionPoints.ToList(), _model.BoxCollider);
            // foreach (var p in intersectionPoints)
            // {
            //     Debug.DrawRay(p, Vector3.forward, Color.red, 120);
            // }

            return new Mesh
            {
                vertices = intersectionPoints.ToArray(),
                triangles = new int[] { 0, 2, 1, 1, 2, 3},
                normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back , Vector3.back },
                uv = new Vector2[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one }
            };
        }

        private IEnumerable<Vector3> GetIntersectionPoints()
        {
            // test ALL edges for cuts and return them

            var mt = _model.transform;
            // transform.position is NOT the centerpoint of the model!
            var center = mt.TransformPoint(_model.BoxCollider.center);
            var size = _model.Size;
            var extents = _model.Extents;
            
            // this is the normal of the slicer
            var normalVec = _slicerRotation * Vector3.back;
            
            // _slicerPosition, because we can give it ANY point on the plane, and it sets itself up automatically
            var plane = new Plane(normalVec, _slicerPosition);
            
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
              
        private static IEnumerable<Vector3> CalculatePositionWithinModel(IEnumerable<Vector3> normalisedContacts, Model model, Vector3 size)
        {
            foreach (var contact in normalisedContacts)
            {
                yield return PositionInModel(contact, model, size);
            }
        }

        private static Vector3 PositionInModel(Vector3 contact, Model model, Vector3 size)
        {
            var xRelativePosition = (contact.z / size.z) * model.XCount;
            var yRelativePosition = (contact.y / size.y) * model.YCount;
            var zRelativePosition = (contact.x / size.x) * model.ZCount;

            return new Vector3(
                Mathf.Round(xRelativePosition),
                Mathf.Round(yRelativePosition),
                Mathf.Round(zRelativePosition));
        }

        private static IEnumerable<Vector3> GetBoundaryIntersections(IReadOnlyList<Vector3> intersectionPoints, BoxCollider collider)
        {
            var p1 = SetBoundsPoint(intersectionPoints[0], collider);
            var p2 = SetBoundsPoint(intersectionPoints[1], collider);
            var p3 = SetBoundsPoint(intersectionPoints[2], collider);
            var p4 = SetBoundsPoint(intersectionPoints[3], collider);

            // vertically
            var v1 = GetMostOuterPointOnBound(collider, p1, p3);
            var v2 = GetMostOuterPointOnBound(collider, p2, p4);
            var v3 = GetMostOuterPointOnBound(collider, p3, p1);
            var v4 = GetMostOuterPointOnBound(collider, p4, p2);

            //horizontally
            var h1 = GetMostOuterPointOnBound(collider, v1, v2);
            var h2 = GetMostOuterPointOnBound(collider, v2, v1);
            var h3 = GetMostOuterPointOnBound(collider, v3, v4);
            var h4 = GetMostOuterPointOnBound(collider, v4, v3);
            
            return new Vector3[] { h1, h2, h3, h4 };
        }

        /// <summary>
        /// Position outside point outside of collider, use two points to create line
        /// move outside point towards original point until collision with collider to find outside border
        /// Beforehand, it was tried to work with ray casting, which was not reliable
        /// See commit f0222339 for obsolete code
        /// </summary>
        private static Vector3 GetMostOuterPointOnBound(BoxCollider collider, Vector3 point, Vector3 referencePoint)
        {
            const float threshold = 0.01f;
            const int maxIterations = 10000;
            
            var direction = point - referencePoint;
            var outsidePoint = point + direction * 20;
            var i = 0;
            var distance = 100f;
            while (distance > threshold && i < maxIterations)
            {
                outsidePoint = Vector3.MoveTowards(outsidePoint, point, threshold);
                distance = Vector3.Distance(outsidePoint, collider.ClosestPoint(outsidePoint));
                i++;
            }

            var result = i == maxIterations ? point : outsidePoint;
            return SetBoundsPoint(result, collider);
        }

        private static Vector3 GetNormalisedPosition(Vector3 relativePosition, Vector3 minPosition) =>
            relativePosition + minPosition;
        
        private static Vector3 SetBoundsPoint(Vector3 point, BoxCollider collider)
        {
            const float threshold = 0.1f;
            
            var boundsPoint = collider.ClosestPointOnBounds(point);
            var distance = Vector3.Distance(point, boundsPoint);
            return distance > threshold ? point : boundsPoint;
        }
    }
}