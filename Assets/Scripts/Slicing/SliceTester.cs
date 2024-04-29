#nullable enable

using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections;
using System.Threading;
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

        private SemaphoreSlim _sem = new(0, 1);

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
                Texture2D? texture = null;
                var task = Task.Run(async () =>
                {
                    var points = await SlicePlane.GetIntersectionPointsAsync(model, slicerPositionLocal, slicerRotationNormal);
                    if (points == null)
                    {
                        return;
                    }

                    var dimensions = SlicePlane.GetTextureDimension(model, points);
                    if (dimensions == null)
                    {
                        return;
                    }

                    await UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
                    {
                        texture = SlicePlane.CreateSliceTexture(model, dimensions, points);
                        _sem.Release();
                    });
                    await _sem.WaitAsync();
                });
                
                yield return new WaitUntil(() => task.IsCompleted);

                if (texture != null)
                {
                    var oldTexture = _mat.mainTexture;
                    _mat.mainTexture = texture;
                    Destroy(oldTexture);
                    texture = null;
                }
            }
        }
    }
}
