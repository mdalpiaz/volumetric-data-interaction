#nullable enable

using System;
using UnityEngine;

namespace Model
{
    public class ModelManager : MonoBehaviour
    {
        public static ModelManager Instance { get; private set; } = null!;

        public Model CurrentModel { get; private set; } = null!;

        private Transform? _tracker;

        private Vector3 _positionOffset;

        private Quaternion _rotationOffset;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
                CurrentModel = GetActiveModel() ?? throw new NullReferenceException("No active Model found!");
            }
            else
            {
                Destroy(this);
            }
        }

        private void Update()
        {
            // tracker is null if not mapping
            if (_tracker == null)
            {
                return;
            }

            var cachedTransform = CurrentModel.transform;
            cachedTransform.position = _positionOffset + _tracker.position;
            cachedTransform.rotation = _tracker.rotation * _rotationOffset;
        }

        public bool ModelExists(string nameToCheck)
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).name == nameToCheck)
                {
                    return true;
                }
            }

            return false;
        }

        public void ChangeModel(string nameToCheck)
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                gameObject.SetActive(false);
                if (transform.GetChild(i).name == nameToCheck)
                {
                    gameObject.SetActive(true);
                }
            }
        }

        public void StartMapping(Transform tracker)
        {
            Debug.Log("Started Mapping");
            _tracker = tracker;
            var cachedTransform = CurrentModel.transform;
            _positionOffset = cachedTransform.position - _tracker.position;
            _rotationOffset = _tracker.rotation * Quaternion.Inverse(cachedTransform.rotation);
        }

        public void StopMapping()
        {
            Debug.Log("Stopped Mapping");
            _tracker = null;
        }

        public void ResetState()
        {
            // TODO
        }

        private Model? GetActiveModel()
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                if (child.activeSelf)
                {
                    return child.GetComponent<Model>();
                }
            }

            return null;
        }
    }
}
