#nullable enable

using System;
using Model;
using Selection;
using Slicing;
using UnityEngine;

namespace Snapshots
{
    [RequireComponent(typeof(Selectable))]
    public class Snapshot : MonoBehaviour
    {
        private Vector3 _detachedPosition;
        private Vector3 _detachedScale;

        private GameObject? _tempNeighbourOverlay;

        private GameObject _textureQuad = null!;
        private MeshRenderer _textureQuadRenderer = null!;
        
        public ulong ID { get; set; }

        public GameObject Viewer { get; set; } = null!;

        public GameObject OriginPlane { get; set; } = null!;

        public Dimensions Dimension { get; set; } = null!;

        public IntersectionPoints IntersectionPoints { get; set; } = null!;

        public Texture2D SnapshotTexture
        {
            get => _textureQuadRenderer.material.mainTexture as Texture2D ?? throw new NullReferenceException("Snapshot texture was null!");
            set => _textureQuadRenderer.material.mainTexture = value;
        }

        public bool IsAttached { get; private set; }

        public Selectable Selectable { get; private set; } = null!;

        private void Awake()
        {
            Selectable = GetComponent<Selectable>();
            _textureQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(_textureQuad.GetComponent<MeshCollider>());
            _textureQuadRenderer = _textureQuad.GetComponent<MeshRenderer>();
            _textureQuad.transform.SetParent(transform);
            _textureQuad.transform.localPosition = new Vector3(0, 0, 0.01f);
            _textureQuad.SetActive(false);
        }

        private void OnEnable()
        {
            Selectable.SelectChanged += HandleSelection;
        }

        private void OnDisable()
        {
            Selectable.SelectChanged -= HandleSelection;
        }

        private void Update()
        {
            if (Viewer && !IsAttached)
            {
                var cachedTransform = transform;
                cachedTransform.LookAt(Viewer.transform);
                cachedTransform.forward = -cachedTransform.forward; //need to adjust as quad is else not visible
            }
        }

        private void OnDestroy()
        {
            Destroy(OriginPlane);
            if (_tempNeighbourOverlay != null)
            {
                Destroy(_tempNeighbourOverlay);
            }
        }

        public void AttachToTransform(Transform t, Vector3 position)
        {
            IsAttached = true;
            var cachedTransform = transform;
            _detachedScale = cachedTransform.localScale;
            _detachedPosition = cachedTransform.localPosition;
            cachedTransform.SetParent(t);
            cachedTransform.SetPositionAndRotation(position, new Quaternion());
            cachedTransform.localScale = new Vector3(1, 0.65f, 0.1f);
        }

        public void Detach()
        {
            IsAttached = false;
            var cachedTransform = transform;
            cachedTransform.SetParent(null);
            cachedTransform.localScale = _detachedScale; 
            cachedTransform.position = _detachedPosition;
        }

        public void SetIntersectionChild(Texture2D texture, Vector3 startPoint, Model.Model model)
        {
            var quadScale = MaterialTools.GetTextureAspectRatioSize(transform.localScale, texture);
            _textureQuad.transform.localScale = quadScale;

            var quadMaterial = _textureQuadRenderer.material;
            quadMaterial.mainTexture = texture;
            _textureQuadRenderer.material = MaterialTools.GetMaterialOrientation(quadMaterial, model, startPoint);
            
            _textureQuad.SetActive(true);
        }

        public void MoveSliceX(float amount)
        {
            // TODO this function is pretty much equivalent with creating neighbours and deleting the original
        }

        public void MoveSliceY(float amount)
        {
            // TODO
        }

        public void MoveSliceZ(float value)
        {
            var model = ModelManager.Instance.CurrentModel;

            // value is pixels
            IntersectionPoints.UpperLeft = new Vector3()
            {
                x = IntersectionPoints.UpperLeft.x + value,
                y = IntersectionPoints.UpperLeft.y,
                z = IntersectionPoints.UpperLeft.z
            };
            IntersectionPoints.LowerLeft = new Vector3()
            {
                x = IntersectionPoints.LowerLeft.x + value,
                y = IntersectionPoints.LowerLeft.y,
                z = IntersectionPoints.LowerLeft.z
            };
            IntersectionPoints.LowerRight = new Vector3()
            {
                x = IntersectionPoints.LowerRight.x + value,
                y = IntersectionPoints.LowerRight.y,
                z = IntersectionPoints.LowerRight.z
            };
            IntersectionPoints.UpperRight = new Vector3()
            {
                x = IntersectionPoints.UpperRight.x + value,
                y = IntersectionPoints.UpperRight.y,
                z = IntersectionPoints.UpperRight.z
            };

            var texData = SlicePlane.CreateSliceTextureData(model, Dimension, IntersectionPoints);
            var texture = SlicePlane.CreateSliceTexture(Dimension, texData);
            
            SetIntersectionChild(texture, IntersectionPoints.LowerLeft, model);

            // value is unity coordinates
            var boxColliderSize = model.BoxCollider.size;
            var gameDimensionKey = boxColliderSize.z / model.XCount;
            var scaledValue = model.transform.localScale.x * value * gameDimensionKey;
            var originPosition = OriginPlane.transform.position;
            originPosition.z += scaledValue;
            OriginPlane.transform.position = originPosition;
        }
        
        private void SetOverlayTexture(bool isSelected)
        {
            if (isSelected)
            {
                SnapshotManager.Instance.InterfaceController.BlackenOut();

                var overlay = SnapshotManager.Instance.InterfaceController.Main;
                var snapshotQuad = Instantiate(_textureQuad);
                var cachedQuadTransform = snapshotQuad.transform;
                var cachedQuadScale = cachedQuadTransform.localScale;
                var scale = MaterialTools.GetAspectRatioSize(overlay.localScale, cachedQuadScale.y, cachedQuadScale.x); //new Vector3(1, 0.65f, 0.1f);
            
                cachedQuadTransform.SetParent(overlay);
                cachedQuadTransform.localScale = scale;
                cachedQuadTransform.SetLocalPositionAndRotation(new Vector3(0, 0, -0.1f), new Quaternion());
                Destroy(_tempNeighbourOverlay);
                _tempNeighbourOverlay = snapshotQuad;
            }
            else
            {
                SnapshotManager.Instance.InterfaceController.RestorePreviousOverlay();
                Destroy(_tempNeighbourOverlay);
            }
        }

        private void HandleSelection(bool selected)
        {
            OriginPlane.SetActive(selected);
            SetOverlayTexture(selected);
        }
    }
}