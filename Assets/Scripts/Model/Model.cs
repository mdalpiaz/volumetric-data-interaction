#nullable enable

using System;
using System.IO;
using Constants;
using Helper;
using Selection;
using UnityEngine;

namespace Model
{
    /// <summary>
    /// Add scs.rsp to be able to use Bitmaps in Unity
    /// https://forum.unity.com/threads/using-bitmaps-in-unity.899168/
    /// </summary>
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
        
        public Selectable Selectable { get; private set; } = null!;

        public BoxCollider BoxCollider { get; private set; } = null!;

        public Texture2D[] OriginalBitmap { get; private set; } = null!;
        
        public int XCount { get; private set; }

        public int YCount { get; private set; }

        public int ZCount { get; private set; }
        
        public Vector3 CountVector => new(XCount, YCount, ZCount);

        public Vector3 Size { get; private set; }

        public Vector3 Extents => Size / 2.0f;

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
            
            OriginalBitmap = InitModel(stackPath);

            XCount = OriginalBitmap.Length;
            YCount = OriginalBitmap.Length > 0 ? OriginalBitmap[0].height : 0;
            ZCount = OriginalBitmap.Length > 0 ? OriginalBitmap[0].width : 0;

            _originalMesh = Instantiate(_meshFilter.sharedMesh);
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