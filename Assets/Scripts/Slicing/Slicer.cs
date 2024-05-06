#nullable enable

using Constants;
using Extensions;
using EzySlice;
using Model;
using UnityEngine;

namespace Slicing
{
    /// <summary>
    /// https://github.com/LandVr/SliceMeshes
    /// </summary>
    public class Slicer : MonoBehaviour
    {
        [SerializeField]
        private CutQuad cutQuadPrefab = null!;
        
        [SerializeField]
        private GameObject cuttingPlane = null!;
        
        [SerializeField]
        private Material materialTemporarySlice = null!;
        
        [SerializeField]
        private Material materialWhite = null!;
        
        [SerializeField]
        private Material materialBlack = null!;
        
        [SerializeField]
        private Shader materialShader = null!;
        
        private bool _isTouched;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(Tags.Model))
            {
                return;
            }

            _isTouched = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(Tags.Model))
            {
                return;
            }

            _isTouched = false;
        }

        public void Slice()
        {
            if (!_isTouched)
            {
                return;
            }
            
            Debug.Log("Slicing");

            var cachedTransform = transform;
            var model = ModelManager.Instance.CurrentModel;
            var modelGo = model.gameObject;
            
            var slicedObject = modelGo.Slice(cachedTransform.position, cachedTransform.forward);
            if (slicedObject == null)
            {
                Debug.LogError("Nothing sliced");
                return;
            }
            AudioManager.Instance.PlayCameraSound();

            transform.GetLocalPositionAndRotation(out var position, out var rotation);
            var points = model.GetIntersectionPointsFromLocal(position, rotation);
            if (points == null)
            {
                Debug.LogWarning("Intersection image can't be calculated!");
                return;
            }
            
            var dimensions = model.GetTextureDimension(points);
            if (dimensions == null)
            {
                Debug.LogWarning("SliceCoords can't be calculated!");
                return;
            }
            var texData = model.CreateSliceTextureData(dimensions, points);
            var texture = SlicingExtensions.CreateSliceTexture(dimensions, texData);
            var mesh = model.CreateMesh(points);
            
            var transparentMaterial = MaterialTools.CreateTransparentMaterial();
            transparentMaterial.name = "SliceMaterial";
            transparentMaterial.mainTexture = texture;

            Debug.Log($"Sliced gameobject \"{model.name}\"");
            var lowerHull = slicedObject.CreateUpperHull(modelGo, materialBlack);
            model.UpdateModel(lowerHull.GetComponent<MeshFilter>().mesh, gameObject);
            Destroy(lowerHull);
            SetCuttingActive(true);

            var quad = Instantiate(cutQuadPrefab, model.transform, true);
            quad.name = "cut";
            quad.Mesh = mesh;
            quad.Material = transparentMaterial;
        }
        
        public void SetCuttingActive(bool active)
        {
            cuttingPlane.SetActive(active);
            var model = ModelManager.Instance.CurrentModel;

            if (active)
            {
                model.SetCuttingPlane(cuttingPlane);
            }

            model.SetCuttingPlaneActive(active);

            if (active)
            {
                model.SetModelMaterial(materialTemporarySlice, materialShader);
            }
            else
            {
                model.SetModelMaterial(materialWhite);
            }
        }
    }
}