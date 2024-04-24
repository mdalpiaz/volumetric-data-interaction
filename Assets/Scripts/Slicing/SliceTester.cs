using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Slicing
{
    public class SliceTester : MonoBehaviour
    {
        [SerializeField]
        private Model.Model model;
        
        [SerializeField]
        private Transform slicer;

        private Material _mat;
        
        private MeshRenderer _meshRenderer;

        private IEnumerator _coroutine;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _mat = MaterialTools.CreateTransparentMaterial();
            _meshRenderer.material = _mat;
        }

        private void OnEnable()
        {
            var routine = UpdateTextures();
            _coroutine = routine;
            StartCoroutine(routine);
        }

        private void OnDisable()
        {
            StopCoroutine(_coroutine);
        }

        private IEnumerator UpdateTextures()
        {
            while (true)
            {
                var points = SlicePlane.GetIntersectionPoints(model, slicer.position, slicer.rotation);
                if (points == null)
                {
                    yield return null;
                    continue;
                }

                var sliceCoords = SlicePlane.CreateSlicePlaneCoordinates(model, points);
                var texture = SlicePlane.CreateSliceTexture(model, sliceCoords);
                _mat.mainTexture = texture;
                yield return null;
            }
        }
    }
}
