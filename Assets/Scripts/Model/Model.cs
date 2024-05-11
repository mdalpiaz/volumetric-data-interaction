#nullable enable

using System;
using System.IO;
using System.Linq;
using Extensions;
using Selection;
using UnityEngine;

namespace Model
{
    [RequireComponent(typeof(Selectable))]
    public class Model : MonoBehaviour
    {
        [SerializeField]
        private string? stackPath;

        private MeshFilter _meshFilter = null!;
        private Renderer _renderer = null!;
        private OnePlaneCuttingController _onePlaneCuttingController = null!;

        private Mesh _originalMesh = null!;
        private Material _originalMaterial = null!;

        private Color32[] _slices = null!;

        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Vector3 _originalScale;

        public Selectable Selectable { get; private set; } = null!;

        public BoxCollider BoxCollider { get; private set; } = null!;
        
        public int XCount { get; private set; }

        public int YCount { get; private set; }

        public int ZCount { get; private set; }
        
        public Vector3 Size { get; private set; }

        public Vector3 StepSize { get; private set; }

        public Vector3 BottomFrontLeftCorner { get; private set; }

        public Vector3 BottomFrontRightCorner { get; private set; }

        public Vector3 TopFrontLeftCorner { get; private set; }

        public Vector3 TopFrontRightCorner { get; private set; }

        public Vector3 BottomBackLeftCorner { get; private set; }

        public Vector3 BottomBackRightCorner { get; private set; }

        public Vector3 TopBackLeftCorner { get; private set; }

        public Vector3 TopBackRightCorner { get; private set; }

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            Selectable = GetComponent<Selectable>();
            BoxCollider = GetComponent<BoxCollider>();
            _renderer = GetComponent<Renderer>();
            _onePlaneCuttingController = GetComponent<OnePlaneCuttingController>();

            if (stackPath == null)
            {
                throw new ArgumentNullException(nameof(stackPath), "The stack path mustn't be empty!");
            }
            
            if (!Directory.Exists(stackPath))
            {
                Debug.LogError($"Directory \"{stackPath}\" not found!");
                throw new ArgumentException($"Directory \"{stackPath}\" not found!");
            }
            var files = Directory.GetFiles(stackPath);
            if (files.Length == 0)
            {
                Debug.LogError($"No files loaded from \"{stackPath}\"!");
                throw new ArgumentException($"No files loaded from \"{stackPath}\"!");
            }

            var imagePath = Path.Combine(stackPath, files[0]);
            var texture = FileTools.LoadImage(imagePath);

            ZCount = files.Length;
            XCount = texture.width;
            YCount = texture.height;

            _slices = new Color32[XCount * YCount * ZCount];

            texture.GetPixels32().CopyTo(_slices, 0);
            Destroy(texture);

            for (var i = 1; i < files.Length; i++)
            {
                imagePath = Path.Combine(stackPath, files[i]);
                texture = FileTools.LoadImage(imagePath);
                texture.GetPixels32().CopyTo(_slices, i * XCount * YCount);
                Destroy(texture);
            }

            _originalMesh = Instantiate(_meshFilter.sharedMesh);
            _originalMaterial = GetComponent<Renderer>().material;

            _originalPosition = transform.position;
            _originalRotation = transform.rotation;
            _originalScale = transform.localScale;

            // this only works if the model is perfectly aligned with the world! (rotation 0,0,0 or 90 degree rotations)
            var worldSize = transform.TransformVector(BoxCollider.size);
            var worldExtents = worldSize / 2.0f;

            // this code gets ALL corner points and sorts them locally, so we can easily determin to which corner which point belongs
            // this code has already been tested and is CORRECT
            var points = new Vector3[8];
            var center = transform.TransformPoint(BoxCollider.center);  // transform.position is NOT the centerpoint of the model!
            points[0] = transform.InverseTransformPoint(center + transform.left() * worldExtents.x + transform.down() * worldExtents.y + transform.back() *  worldExtents.z);
            points[1] = transform.InverseTransformPoint(center + transform.left() * worldExtents.x + transform.down() * worldExtents.y + transform.forward * worldExtents.z);
            points[2] = transform.InverseTransformPoint(center + transform.left() * worldExtents.x + transform.up *     worldExtents.y + transform.back() *  worldExtents.z);
            points[3] = transform.InverseTransformPoint(center + transform.left() * worldExtents.x + transform.up *     worldExtents.y + transform.forward * worldExtents.z);
            points[4] = transform.InverseTransformPoint(center + transform.right *  worldExtents.x + transform.down() * worldExtents.y + transform.back() *  worldExtents.z);
            points[5] = transform.InverseTransformPoint(center + transform.right *  worldExtents.x + transform.down() * worldExtents.y + transform.forward * worldExtents.z);
            points[6] = transform.InverseTransformPoint(center + transform.right *  worldExtents.x + transform.up *     worldExtents.y + transform.back() *  worldExtents.z);
            points[7] = transform.InverseTransformPoint(center + transform.right *  worldExtents.x + transform.up *     worldExtents.y + transform.forward * worldExtents.z);

            BottomBackLeftCorner =   points.OrderBy(p => p.x)          .Take(4).OrderBy(p => p.y)          .Take(2).OrderByDescending(p => p.z).First();
            BottomBackRightCorner =  points.OrderByDescending(p => p.x).Take(4).OrderBy(p => p.y)          .Take(2).OrderByDescending(p => p.z).First();
            BottomFrontLeftCorner =  points.OrderBy(p => p.x)          .Take(4).OrderBy(p => p.y)          .Take(2).OrderBy(p => p.z).First();
            BottomFrontRightCorner = points.OrderByDescending(p => p.x).Take(4).OrderBy(p => p.y)          .Take(2).OrderBy(p => p.z).First();
            TopBackLeftCorner =      points.OrderBy(p => p.x)          .Take(4).OrderByDescending(p => p.y).Take(2).OrderByDescending(p => p.z).First();
            TopBackRightCorner =     points.OrderByDescending(p => p.x).Take(4).OrderByDescending(p => p.y).Take(2).OrderByDescending(p => p.z).First();
            TopFrontLeftCorner =     points.OrderBy(p => p.x)          .Take(4).OrderByDescending(p => p.y).Take(2).OrderBy(p => p.z).First();
            TopFrontRightCorner =    points.OrderByDescending(p => p.x).Take(4).OrderByDescending(p => p.y).Take(2).OrderBy(p => p.z).First();

            Size = new Vector3
            {
                x = BottomBackRightCorner.x - BottomBackLeftCorner.x,
                y = TopBackLeftCorner.y - BottomBackLeftCorner.y,
                z = BottomBackLeftCorner.z - BottomFrontLeftCorner.z
            };

            StepSize = new Vector3
            {
                x = Size.x / XCount,
                y = Size.y / YCount,
                z = Size.z / ZCount
            };
        }

        private void Start()
        {
            _onePlaneCuttingController.plane = ModelManager.Instance.CuttingPlane;
        }

        public bool IsXEdgeVector(Vector3 point) => point.x == 0 || (point.x + 1) >= XCount;

        public bool IsZEdgeVector(Vector3 point) => point.z == 0 || (point.z + 1) >= ZCount;

        public bool IsYEdgeVector(Vector3 point) => point.y == 0 || (point.y + 1) >= YCount;
        
        public void UpdateModel(GameObject newModel, GameObject cuttingPlane)
        {
            Debug.Log("Replacing model");
            _onePlaneCuttingController.plane = cuttingPlane;
            _meshFilter.mesh = newModel.GetComponent<MeshFilter>().mesh;
            _renderer.materials = newModel.GetComponent<MeshRenderer>().materials;
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

        public void SetCuttingPlaneActive(bool active)
        {
            _onePlaneCuttingController.plane.SetActive(active);
            _onePlaneCuttingController.enabled = active;
        }

        public void ResetState()
        {
            transform.SetPositionAndRotation(_originalPosition, _originalRotation);
            transform.localScale = _originalScale;

            _meshFilter.mesh = Instantiate(_originalMesh);
            _renderer.material = _originalMaterial;

            // destroy top to bottom to stop index out of bounds
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        public Vector3Int LocalPositionToIndex(Vector3 pos)
        {
            var diff = pos - BottomFrontLeftCorner;

            return new Vector3Int
            {
                x = Mathf.RoundToInt(diff.x / StepSize.x),
                y = Mathf.RoundToInt(diff.y / StepSize.y),
                z = Mathf.RoundToInt(diff.z / StepSize.z)
            };
        }

        /// <summary>
        /// Returns the pixel color at the specific location. Out of bounds locations are returned as black.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Color32 GetPixel32(Vector3Int index)
        {
            if (index.x >= XCount || index.y >= YCount || index.z >= ZCount ||
                index.x < 0 || index.y < 0 || index.z < 0)
            {
                return new Color32(0, 0, 0, 0);
            }

            return _slices[ToSliceIndex(index)];
        }

        private int ToSliceIndex(Vector3Int i)
        {
            return i.x + i.y * XCount + i.z * XCount * YCount;
        }
    }
}