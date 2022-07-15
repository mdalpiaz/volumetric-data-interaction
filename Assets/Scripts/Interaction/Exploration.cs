﻿using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interactions class has to be attached to gameobject holding the tracked device
/// </summary>
public class Exploration : MonoBehaviour
{
    private GameObject tracker;

    public Exploration(GameObject tracker)
    {
        this.tracker = tracker;
    }

    /// <summary>
    /// e.g. 5 cm before HHD
    /// Do not allow multiple models!
    /// </summary>
    public GameObject CreateModel()
    {       
        var currTrackingPosition = tracker.transform.position;
        currTrackingPosition.z += 5;

        return CreateModel(currTrackingPosition, Quaternion.identity);
    }

    private GameObject CreateModel(Vector3 currPosition, Quaternion rotation)
    {
        var currModel = FindCurrentModel();
        if (currModel)
        {
            DeleteModel(currModel);
        }

        var model = Resources.Load(StringConstants.PrefabSectionModel, typeof(GameObject)) as GameObject;
        Debug.Log($"** Create model with name {StringConstants.PrefabSectionModel}.");
        return Instantiate(model, currPosition, rotation);
    }

    private GameObject FindCurrentModel()
    {
        var currModel = GameObject.Find(StringConstants.Model) ?? GameObject.Find($"{StringConstants.Model}({StringConstants.Clone})");
        if (currModel == null)
        {
            Debug.Log($"** No model with name {StringConstants.Model} found.");
        }
        return currModel;
    }

    private void DeleteModel(GameObject currModel)
    {
        if (!currModel)
        {
            return;
        }

        DeleteAllSnapshots();
        Destroy(currModel);
        Debug.Log($"** Model with name {StringConstants.PrefabSectionModel} destroyed.");
    }

    public GameObject ResetModel()
    {
        var currModel = FindCurrentModel();
        if (!currModel)
        {
            Debug.Log("** There is no model to be reset!");
            return currModel;
        }

        var currPosition = new Vector3(currModel.transform.position.x, currModel.transform.position.y, currModel.transform.position.z);
        var currRotation = new Quaternion(currModel.transform.rotation.x, currModel.transform.rotation.y, currModel.transform.rotation.z, currModel.transform.rotation.w);

        return CreateModel(currPosition, currRotation);
    }

    #region Snapshot Logic
    public bool HasSnapshots()
    {
        return GetAllSnapshots().Count != 0;
    }
    private List<GameObject> GetAllSnapshots()
    {
        var snapshots = new List<GameObject>();
        foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
        {
            if (go.name.Contains(StringConstants.Snapshot + StringConstants.Clone))
            {
                snapshots.Add(go);
            }
        }

        return snapshots;
    }

    public void DeleteAllSnapshots()
    {
        var snapshots = GetAllSnapshots();
        snapshots.ForEach(DeleteSnapshot);
    }

    public void DeleteSnapshot(GameObject snapshot)
    {
        var snapshotScript = snapshot.GetComponent<Snapshot>();
        snapshotScript.SetSelected(false);
        Destroy(snapshotScript.OriginPlane);
        Destroy(snapshot);
    }

    public void AlignSnapshots()
    {
        var snapshots = GetAllSnapshots();

        var overlay = tracker.transform.FindChild(StringConstants.OverlayScreen);
        if (!overlay)
        {
            Debug.Log("Alignment not possible. Overlay screen not found as child of tracker.");
        }

        if (snapshots.Count > 5)
        {
            Debug.Log("More than 5 snapshots detected. Only 5 will be aligned.");
        }

        var index = 1;
        foreach (var shot in snapshots)
        {
            var child = overlay.GetChild(index++);
            shot.transform.SetParent(overlay);
            shot.GetComponent<Snapshot>().IsLookingAt = false;
            shot.transform.position = child.position;
            shot.transform.rotation = new Quaternion();
            shot.transform.localScale = child.localScale;
        }

        // align in a circle
        //var radius = snapshots.Count * 2;
        //for (int i = 0; i < snapshots.Count; i++)
        //{
        //    var angle = i * Mathf.PI * 2f / radius;
        //    var newPos = tracker.transform.position + new Vector3(Mathf.Cos(angle) * radius, -2, Mathf.Sin(angle) * radius);
        //    snapshots[i].transform.position = newPos;
        //    snapshots[i].transform.SetParent(tracker.transform);
        //}
    }

    public void PlaceSnapshot(Vector3 newPosition)
    {
        var snapshotPrefab = Resources.Load(StringConstants.PrefabSnapshot, typeof(GameObject)) as GameObject;
        var snapshot = Instantiate(snapshotPrefab);
        snapshot.transform.position = newPosition;

        Texture2D testImage = Resources.Load(StringConstants.ImageTest) as Texture2D;
        snapshot.GetComponent<MeshRenderer>().material.mainTexture = testImage; // TODO exchange with calculated image from cutting plane

        // set origin plane
        var originPlane = Instantiate(Resources.Load(StringConstants.PrefabOriginPlane), tracker.transform.position, tracker.transform.rotation) as GameObject;
        originPlane.transform.SetParent(FindCurrentModel().transform);

        var snapshotScript = snapshot.GetComponent<Snapshot>();
        snapshotScript.Viewer = tracker;
        snapshotScript.OriginPlane = originPlane;
        snapshotScript.SetSelected(false);
    }
    #endregion // Snapshot Logic
}