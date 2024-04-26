#nullable enable

using System.Collections;
using UnityEngine;

namespace Slicing
{
    public class SliceTester : MonoBehaviour
    {
        [SerializeField]
        private Model.Model model = null!;
        
        [SerializeField]
        private Transform slicer = null!;

        private Material _mat = null!;
        
        private MeshRenderer _meshRenderer = null!;

        private IEnumerator _coroutine = null!;

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

                var dimensions = SlicePlane.GetTextureDimension(model, points);
                if (dimensions == null)
                {
                    yield return null;
                    continue;
                }

                var texture = SlicePlane.CreateSliceTexture(model, dimensions, points);
                var oldTexture = _mat.mainTexture;
                _mat.mainTexture = texture;
                Destroy(oldTexture);
                yield return null;
            }
        }
    }
}