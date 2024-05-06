#nullable enable

using Model;
using System.Collections;
using System.Threading.Tasks;
using Extensions;
using UnityEngine;

namespace Slicing
{
    public class SliceTester : MonoBehaviour
    {
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
                var model = ModelManager.Instance.CurrentModel;
                slicer.GetLocalPositionAndRotation(out var position, out var rotation);
                Dimensions? dimensions = null;
                Color32[]? texData = null;

                var task = Task.Run(() =>
                {
                    var points = model.GetIntersectionPointsFromLocal(position, rotation);
                    if (points == null)
                    {
                        return;
                    }

                    dimensions = model.GetTextureDimension(points);
                    if (dimensions == null)
                    {
                        return;
                    }

                    texData = model.CreateSliceTextureData(dimensions, points);
                });
                
                yield return new WaitUntil(() => task.IsCompleted);

                if (texData != null && dimensions != null)
                {
                    var texture = SlicingExtensions.CreateSliceTexture(dimensions, texData);
                    var oldTexture = _mat.mainTexture;
                    _mat.mainTexture = texture;
                    Destroy(oldTexture);
                }
            }
        }
    }
}
