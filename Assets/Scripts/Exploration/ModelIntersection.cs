﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Exploration
{
    public class ModelIntersection : MonoBehaviour
    {
        private GameObject plane;
        private GameObject model;
        private Model modelScript;

        public ModelIntersection(GameObject model, GameObject plane)
        {
            this.plane = plane;
            this.model = model;
            this.modelScript = model.GetComponent<Model>();
        }

        public List<Vector3> GetNormalisedIntersectionPosition()
        {
            var yellow = Resources.Load(StringConstants.MaterialYellowHighlighted, typeof(Material)) as Material;

            var modelCollider = model.GetComponent<Collider>();
            var intersectionPoints = GetIntersectionPoints();

            var normalisedPositions = new List<Vector3>();
            foreach (var p in intersectionPoints)
            {
                var c = CreateDebugPrimitive(p, yellow);
                c.transform.SetParent(model.transform);
                normalisedPositions.Add(GetNormalisedPosition(c.transform.position, modelCollider.bounds.min));
                Destroy(c);
            }

            var positions = CalculatePositionWithinModel(normalisedPositions, modelCollider.bounds.size);
            return positions;
            //var intersection = modelScript.GetIntersectionPlane(positions);
            //var fileLocation = Path.Combine(ConfigurationConstants.IMAGES_FOLDER_PATH, "TestImg.bmp");
            //intersection.Save(fileLocation, ImageFormat.Bmp);
        }

        private List<Vector3> GetPlaneMeshVertices()
        {
            var localVertices = plane.GetComponent<MeshFilter>().sharedMesh.vertices;
            var globalVertices = new List<Vector3>();

            foreach (var localPoint in localVertices)
            {
                globalVertices.Add(plane.transform.TransformPoint(localPoint));
            }

            return globalVertices;
        }

        private List<Vector3> GetIntersectionPoints()
        {
            var black = Resources.Load(StringConstants.MaterialBlack, typeof(Material)) as Material;
            var modelCollider = model.GetComponent<Collider>();

            var globalPlaneVertices = GetPlaneMeshVertices();
            var planePosition = plane.transform.position;

            var isTouching = false;
            var touchPoints = new List<Vector3>();
            foreach (var planePoint in globalPlaneVertices)
            {
                isTouching = false;
                var touchPoint = planePoint;

                while (!isTouching && touchPoint != planePosition)
                {
                    touchPoint = Vector3.MoveTowards(touchPoint, planePosition, 0.005f);

                    var hitColliders = Physics.OverlapBox(touchPoint, new Vector3());
                    isTouching = hitColliders.FirstOrDefault(c => c.name == modelCollider.name);
                    //if (isTouching)
                    //{
                    //    CreateDebugPrimitive(touchPoint, black);
                    //}
                }

                touchPoints.Add(touchPoint);
            }

            return touchPoints;
        }

        private List<Vector3> CalculatePositionWithinModel(List<Vector3> normalisedContacts, Vector3 size)
        {
            var xMax = modelScript.xCount;
            var yMax = modelScript.yCount;
            var zMax = modelScript.zCount;

            var positions = new List<Vector3>();
            foreach (var contact in normalisedContacts)
            {
                var xRelativePosition = (contact.x / size.x) * xMax;
                var yRelativePosition = (contact.y / size.y) * yMax;
                var zRelativePosition = (contact.z / size.z) * zMax;
                positions.Add(new Vector3(Mathf.Round(xRelativePosition), Mathf.Round(yRelativePosition), Mathf.Round(zRelativePosition)));
            }

            return positions;
        }

        private GameObject CreateDebugPrimitive(Vector3 position, Material material, string name = "primitive")
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            primitive.transform.position = position;
            primitive.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            primitive.GetComponent<MeshRenderer>().material = material;
            primitive.name = name;
            return primitive;
        }

        private Vector3 GetNormalisedPosition(Vector3 relativePosition, Vector3 minPosition)
        {
            var x = relativePosition.x + Mathf.Abs(minPosition.x);
            var y = relativePosition.y + Mathf.Abs(minPosition.y);
            var z = relativePosition.z + Mathf.Abs(minPosition.z);

            return new Vector3(x, y, z);
        }
    }
}