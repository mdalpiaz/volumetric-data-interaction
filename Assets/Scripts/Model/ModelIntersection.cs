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
        // private readonly Matrix4x4 _slicerMatrix;
        // private readonly IEnumerable<Vector3> _planeMeshVertices;

        public ModelIntersection(Model model, Vector3 slicerPosition, Quaternion slicerRotation/*, Matrix4x4 slicerLocalToWorld, MeshFilter planeMeshFilter*/)
        {
            _model = model;
            _slicerPosition = slicerPosition;
            _slicerRotation = slicerRotation;
            // _slicerMatrix = Matrix4x4.TRS(slicerPosition, slicerRotation, Vector3.one);
            //
            // var mesh = planeMeshFilter.sharedMesh;
            // _planeMeshVertices = mesh.vertices.Select(v => slicerLocalToWorld.MultiplyPoint(v));
        }

        public IEnumerable<Vector3> GetNormalisedIntersectionPosition()
        {
            return GetIntersectionPoints()
                .Select(p => _model.transform.worldToLocalMatrix.MultiplyPoint(p))
                .Select(newPosition => newPosition + _model.Extents)
                .Select(contact => PositionInModel(contact, _model, _model.Size));
        }
        
        /// <summary>
        /// https://catlikecoding.com/unity/tutorials/procedural-meshes/creating-a-mesh/
        /// </summary>
        public Mesh CreateIntersectingMesh()
        {
            var points = GetIntersectionPoints().ToArray();
            foreach (var p in points)
            {
                Debug.DrawRay(p, Vector3.forward, Color.red, 120);
            }

            // cube cross-section has very specific cuts
            // we need to construct the smallest rectangle with all the points on the corner
            // and the up vector can only move up and rotate down by 90 degrees.
            // it can NOT be rotated otherwise! (no roll, only pitch and yaw)


            Debug.Log($"Intersection points: {points.Length}");
            if (points.Length == 3)
            {
            }
            else if (points.Length == 4)
            {
            }
            else if (points.Length == 6)
            {
            }
            
            return new Mesh
            {
                vertices = points.ToArray(),
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
    }
}