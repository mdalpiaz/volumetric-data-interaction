#nullable enable

using System;
using System.Collections;
using System.Threading.Tasks;
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
                slicer.GetPositionAndRotation(out var position, out var rotation);
                var slicerPositionLocal = model.transform.InverseTransformPoint(position);
                var slicerRotationNormal = model.transform.InverseTransformVector(rotation * Vector3.back);
                Dimensions? dimensions = null;
                Color32[]? texData = null;

                var task = Task.Run(async () =>
                {
                    var points = await SlicePlane.GetIntersectionPointsFromLocalAsync(model, slicerPositionLocal, slicerRotationNormal);
                    if (points == null)
                    {
                        return;
                    }

                    dimensions = SlicePlane.GetTextureDimension(model, points);
                    if (dimensions == null)
                    {
                        return;
                    }

                    texData = await SlicePlane.CreateSliceTextureAsync(model, dimensions, points);
                });
                
                yield return new WaitUntil(() => task.IsCompleted);

                if (texData != null && dimensions != null)
                {
                    var texture = new Texture2D(Math.Abs(dimensions.Width), Math.Abs(dimensions.Height));
                    texture.SetPixels32(texData);
                    texture.Apply();
                    var oldTexture = _mat.mainTexture;
                    _mat.mainTexture = texture;
                    Destroy(oldTexture);
                }
            }
        }
    }
}
