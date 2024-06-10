#nullable enable

using System;
using Selection;
using UnityEngine;

namespace Snapshots
{
    [RequireComponent(typeof(Selectable))]
    public class Snapshot : MonoBehaviour
    {
        private Vector3 detachedPosition;
        private Vector3 detachedScale;

        private GameObject? tempNeighbourOverlay;

        private GameObject textureQuad = null!;
        private MeshRenderer textureQuadRenderer = null!;

        private AttachmentPoint? attachmentPoint;
        
        public ulong ID { get; set; }

        public GameObject OriginPlane { get; set; } = null!;

        public Texture2D SnapshotTexture => textureQuadRenderer.material.mainTexture as Texture2D ?? throw new NullReferenceException("Snapshot texture was null!");

        public bool IsAttached { get; private set; }

        public Selectable Selectable { get; private set; } = null!;

        private void Awake()
        {
            tag = Tags.Snapshot;
            
            Selectable = GetComponent<Selectable>();
            textureQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(textureQuad.GetComponent<MeshCollider>());
            textureQuadRenderer = textureQuad.GetComponent<MeshRenderer>();
            textureQuad.transform.SetParent(transform);
            textureQuad.transform.localPosition = new Vector3(0, 0, 0.01f);
            textureQuad.SetActive(false);
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
            if (IsAttached)
            {
                return;
            }
            
            var cachedTransform = transform;
            cachedTransform.LookAt(ViewModeSetter.Instance.Camera.transform);
            cachedTransform.forward = -cachedTransform.forward; //need to adjust as quad is else not visible
        }

        private void OnDestroy()
        {
            Destroy(OriginPlane);
            if (tempNeighbourOverlay != null)
            {
                Destroy(tempNeighbourOverlay);
            }
        }

        public void AttachToTransform(Transform t, AttachmentPoint ap)
        {
            IsAttached = true;
            attachmentPoint = ap;
            attachmentPoint.HasAttachment = true;
            var cachedTransform = transform;
            detachedScale = cachedTransform.localScale;
            detachedPosition = cachedTransform.localPosition;
            cachedTransform.SetParent(t);
            cachedTransform.SetPositionAndRotation(ap.transform.position, new Quaternion());
            cachedTransform.localScale = new Vector3(1, 0.65f, 0.1f);
        }

        public void Detach()
        {
            IsAttached = false;
            if (attachmentPoint != null)
            {
                attachmentPoint.HasAttachment = false;
                attachmentPoint = null;
            }
            var cachedTransform = transform;
            cachedTransform.SetParent(null);
            cachedTransform.localScale = detachedScale; 
            cachedTransform.position = detachedPosition;
        }

        public void SetIntersectionChild(Texture2D texture, Vector3 startPoint, Model.Model model)
        {
            var quadScale = MaterialTools.GetTextureAspectRatioSize(transform.localScale, texture);
            textureQuad.transform.localScale = quadScale;

            var quadMaterial = textureQuadRenderer.material;
            quadMaterial.mainTexture = texture;
            textureQuadRenderer.material = MaterialTools.GetMaterialOrientation(quadMaterial, model, startPoint);
            
            textureQuad.SetActive(true);
        }

        private void SetOverlayTexture(bool isSelected)
        {
            if (isSelected)
            {
                SnapshotManager.Instance.InterfaceController.BlackenOut();

                var overlay = SnapshotManager.Instance.InterfaceController.Main;
                var snapshotQuad = Instantiate(textureQuad);
                var cachedQuadTransform = snapshotQuad.transform;
                var cachedQuadScale = cachedQuadTransform.localScale;
                var scale = MaterialTools.GetAspectRatioSize(overlay.localScale, cachedQuadScale.y, cachedQuadScale.x); //new Vector3(1, 0.65f, 0.1f);
            
                cachedQuadTransform.SetParent(overlay);
                cachedQuadTransform.localScale = scale;
                cachedQuadTransform.SetLocalPositionAndRotation(new Vector3(0, 0, -0.1f), new Quaternion());
                Destroy(tempNeighbourOverlay);
                tempNeighbourOverlay = snapshotQuad;
            }
            else
            {
                SnapshotManager.Instance.InterfaceController.RestorePreviousOverlay();
                Destroy(tempNeighbourOverlay);
            }
        }

        private void HandleSelection(bool selected)
        {
            OriginPlane.SetActive(selected);
            SetOverlayTexture(selected);
        }
    }
}