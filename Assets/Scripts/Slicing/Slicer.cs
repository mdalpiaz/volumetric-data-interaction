#nullable enable

using System.Linq;
using Constants;
using EzySlice;
using Helper;
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
        private GameObject temporaryCuttingPlane = null!;

        [SerializeField]
        private GameObject sectionQuad = null!;

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
        
        private MeshFilter _cuttingPlaneMeshFilter = null!;
        
        private bool _isTouched;

        private void Awake()
        {
            _cuttingPlaneMeshFilter = cuttingPlane.GetComponent<MeshFilter>();
        }

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

            var sectionQuadTransform = sectionQuad.transform;
            
            var intersectionPoints = new ModelIntersection(model, sectionQuadTransform.position, sectionQuadTransform.rotation)
                .GetNormalisedIntersectionPosition()
                // .Select(p => ValueCropper.ApplyThresholdCrop(p, CountVector, CropThreshold))
                .ToArray();
            AudioManager.Instance.PlayCameraSound();
            
            var slicePlane = SlicePlane.Create(model, intersectionPoints);
            if (slicePlane == null)
            {
                Debug.LogWarning("Intersection image can't be calculated!");
                return;
            }

            var transparentMaterial = MaterialTools.CreateTransparentMaterial();
            transparentMaterial.name = "SliceMaterial";
            transparentMaterial.mainTexture = slicePlane.CalculateIntersectionPlane();
            //var sliceMaterial = MaterialTools.GetMaterialOrientation(transparentMaterial, model, slicePlane.SlicePlaneCoordinates.StartPoint);

            var slicedObject = modelGo.Slice(cachedTransform.position, cachedTransform.forward);
            if (slicedObject == null)
            {
                Debug.LogError("Nothing sliced");
                return;
            }

            Debug.Log($"Sliced gameobject \"{model.name}\"");
            var lowerHull = slicedObject.CreateUpperHull(modelGo, materialBlack);
            model.UpdateModel(lowerHull.GetComponent<MeshFilter>().mesh, gameObject);
            Destroy(lowerHull);
            SetTemporaryCuttingPlaneActive(true);

            //SetIntersectionMesh(Model.Model newModel, Material intersectionTexture)
            var cuttingPlaneTransform = _cuttingPlaneMeshFilter.transform;
            var mesh = new ModelIntersection(model,
                cuttingPlaneTransform.position,
                cuttingPlaneTransform.rotation/*,
                cuttingPlaneTransform.localToWorldMatrix,
                _cuttingPlaneMeshFilter*/)
                .CreateIntersectingMesh();

            var quad = Instantiate(cutQuadPrefab, model.transform, true);
            quad.name = "cut";
            quad.Mesh = mesh;
            //quad.Material = sliceMaterial;
        }
        
        public void SetTemporaryCuttingPlaneActive(bool active)
        {
            temporaryCuttingPlane.SetActive(active);

            if (active)
            {
                ModelManager.Instance.CurrentModel.SetCuttingPlane(temporaryCuttingPlane);
            }

            ModelManager.Instance.CurrentModel.SetCuttingPlaneActive(active);

            if (active)
            {
                ModelManager.Instance.CurrentModel.SetModelMaterial(materialTemporarySlice, materialShader);
            }
            else
            {
                ModelManager.Instance.CurrentModel.SetModelMaterial(materialWhite);
            }
        }
    }
}