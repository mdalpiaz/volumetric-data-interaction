#nullable enable

using System;
using System.IO;
using Constants;
using Extensions;
using Selection;
using UnityEngine;

namespace Model
{
    [RequireComponent(typeof(Selectable))]
    public class Model : MonoBehaviour
    {
        [SerializeField]
        private string stackPath = StringConstants.XStackPath;
        
        [SerializeField]
        private MeshFilter sectionQuad = null!;

        private MeshFilter _meshFilter = null!;
        private Renderer _renderer = null!;
        private OnePlaneCuttingController _onePlaneCuttingController = null!;

        private Mesh _originalMesh = null!;

        private Texture2D[] _originalBitmap = null!;

        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Vector3 _originalScale;

        public Selectable Selectable { get; private set; } = null!;

        public BoxCollider BoxCollider { get; private set; } = null!;
        
        public int XCount { get; private set; }

        public int YCount { get; private set; }

        public int ZCount { get; private set; }
        
        public Vector3 CountVector => new(XCount, YCount, ZCount);

        public Vector3 Size { get; private set; }

        public Vector3 Extents => Size / 2.0f;

        public Vector3 Steps => new()
        {
            x = Size.x / XCount,
            y = Size.y / YCount,
            z = Size.z / ZCount
        };

        // transform.position is NOT the centerpoint of the model!
        public Vector3 BottomFrontLeftCorner => transform.TransformPoint(BoxCollider.center) +
            transform.left() * Extents.x +
            transform.down() * Extents.y +
            transform.back() * Extents.z;

        public Vector3 BottomFrontRightCorner => transform.TransformPoint(BoxCollider.center) +
            transform.right * Extents.x +
            transform.down() * Extents.y +
            transform.back() * Extents.z;

        public Vector3 TopFrontLeftCorner => transform.TransformPoint(BoxCollider.center) +
            transform.left() * Extents.x +
            transform.up * Extents.y +
            transform.back() * Extents.z;

        public Vector3 TopFrontRightCorner => transform.TransformPoint(BoxCollider.center) +
            transform.right * Extents.x +
            transform.up * Extents.y +
            transform.back() * Extents.z;

        public Vector3 BottomBackLeftCorner => transform.TransformPoint(BoxCollider.center) +
            transform.left() * Extents.x +
            transform.down() * Extents.y +
            transform.forward * Extents.z;

        public Vector3 BottomBackRightCorner => transform.TransformPoint(BoxCollider.center) +
            transform.right * Extents.x +
            transform.down() * Extents.y +
            transform.forward * Extents.z;

        public Vector3 TopBackLeftCorner => transform.TransformPoint(BoxCollider.center) +
            transform.left() * Extents.x +
            transform.up * Extents.y +
            transform.forward * Extents.z;

        public Vector3 TopBackRightCorner => transform.TransformPoint(BoxCollider.center) +
            transform.right * Extents.x +
            transform.up * Extents.y +
            transform.forward * Extents.z;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            Selectable = GetComponent<Selectable>();
            BoxCollider = GetComponent<BoxCollider>();
            _renderer = GetComponent<Renderer>();
            _onePlaneCuttingController = GetComponent<OnePlaneCuttingController>();

            // this only works if the model is perfectly aligned with the world! (rotation 0,0,0 or 90 degree rotations)
            var s = transform.TransformVector(BoxCollider.size);
            s.x = Mathf.Abs(s.x);
            s.y = Mathf.Abs(s.y);
            s.z = Mathf.Abs(s.z);
            Size = s;
            
            _originalBitmap = InitModel(stackPath);

            // we use slices of the XY plane, why was this called XCount if its on the Z axis?
            ZCount = _originalBitmap.Length;
            YCount = _originalBitmap.Length > 0 ? _originalBitmap[0].height : 0;
            XCount = _originalBitmap.Length > 0 ? _originalBitmap[0].width : 0;

            _originalMesh = Instantiate(_meshFilter.sharedMesh);

            _originalPosition = transform.position;
            _originalRotation = transform.rotation;
            _originalScale = transform.localScale;
        }

        public bool IsXEdgeVector(Vector3 point) => point.x == 0 || (point.x + 1) >= XCount;

        public bool IsZEdgeVector(Vector3 point) => point.z == 0 || (point.z + 1) >= ZCount;

        public bool IsYEdgeVector(Vector3 point) => point.y == 0 || (point.y + 1) >= YCount;
        
        public void UpdateModel(Mesh newMesh, GameObject cuttingPlane)
        {
            Debug.Log("Replacing model");
            // TODO
            _onePlaneCuttingController.plane = cuttingPlane;
            _meshFilter.mesh = newMesh;
            Selectable.Freeze();
            //CurrentModel.OnePlaneCuttingController.plane = cuttingPlane;
            /*
            //objBase.AddComponent<MeshCollider>().convex = true;
            objBase.transform.position = previousModel.transform.position;
            objBase.name = StringConstants.ModelName;
            objBase.AddComponent<Rigidbody>().useGravity = false;

            /* Original collider needs to be kept for the calculation of intersection points
             * Remove mesh collider which is automatically set
             * Only the original box collider is needed
             * Otherwise the object will be duplicated!
             */
            /*
            _boxCollider = objBase.AddComponent<BoxCollider>();
            _boxCollider.center = previousModel.BoxCollider.center;
            _boxCollider.size = previousModel.BoxCollider.size;
            if (objBase.TryGetComponent(out MeshCollider meshCollider))
            {
                Destroy(meshCollider);
            }

            var oldTransform = previousModel.gameObject.transform;
            while (oldTransform.childCount > 0)
            {
                oldTransform.GetChild(oldTransform.childCount - 1).SetParent(objBase.transform);
            }

            Destroy(previousModel.gameObject);

            var model = objBase.AddComponent<Model>();
            var selectable = objBase.AddComponent<Selectable>();
            _listener = objBase.AddComponent<CollisionListener>();
            _cuttingController = objBase.AddComponent<OnePlaneCuttingController>();
            _modelRenderer = objBase.GetComponent<Renderer>();
            selectable.Freeze();
            _cuttingController.plane = cuttingPlane;

            previousModel = CurrentModel;
            CurrentModel = model;
            */
        }
        
        public void SetModelMaterial(Material material)
        {
            _renderer.material = material;
        }

        public void SetModelMaterial(Material material, Shader shader)
        {
            _renderer.material = material;
            _renderer.material.shader = shader;
        }
        
        public void SetCuttingPlaneActive(bool active) => _onePlaneCuttingController.enabled = active;

        public void SetCuttingPlane(GameObject plane) => _onePlaneCuttingController.plane = plane;
        
        public void ResetMesh()
        {
            _meshFilter.mesh = Instantiate(_originalMesh);
        }

        public void RemoveCuts()
        {
            // destroy top to bottom to stop index out of bounds
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        public Color GetPixel(int x, int y, int z, InterpolationType interpolation = InterpolationType.Nearest)
        {
            return interpolation switch
            {
                InterpolationType.Nearest => _originalBitmap[z].GetPixel(x, y),
                InterpolationType.Bilinear => _originalBitmap[z].GetPixelBilinear(_originalBitmap[z].width / (float)x, _originalBitmap[z].height / (float)y),
                _ => throw new NotImplementedException()
            };
        }
        
        public void ResetState()
        {
            transform.position = _originalPosition;
            transform.rotation = _originalRotation;
            transform.localScale = _originalScale;
        }

        private static Texture2D[] InitModel(string path)
        {
            if (!Directory.Exists(path))
            {
                Debug.LogError($"Directory \"{path}\" not found!");
                return Array.Empty<Texture2D>();
            }
            var files = Directory.GetFiles(path);
            if (files.Length == 0)
            {
                Debug.LogError($"No files loaded from \"{path}\"!");
                return Array.Empty<Texture2D>();
            }
            
            var model3D = new Texture2D[files.Length];

            for (var i = 0; i < files.Length; i++)
            {
                var imagePath = Path.Combine(path, files[i]);
                model3D[i] = FileTools.LoadImage(imagePath);
            }

            return model3D;
        }
    }
}