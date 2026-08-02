using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARPlaneManager))]
public class ARPlaneColliderGenerator : MonoBehaviour
{
    private ARPlaneManager _planeManager;

    void Awake()
    {
        _planeManager = GetComponent<ARPlaneManager>();
    }

    void OnEnable()
    {
        _planeManager.trackablesChanged.AddListener(OnPlanesChanged);
    }

    void OnDisable()
    {
        _planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
    }
    private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        foreach (var plane in args.added)
            AddOrUpdateCollider(plane);
        foreach (var plane in args.updated)
            AddOrUpdateCollider(plane);
    }

    private void AddOrUpdateCollider(ARPlane plane)
    {
        MeshCollider col = plane.GetComponent<MeshCollider>();
        if (col == null)
            col = plane.gameObject.AddComponent<MeshCollider>();

        MeshFilter mf = plane.GetComponent<MeshFilter>();
        if (mf != null && mf.mesh != null)
            col.sharedMesh = mf.mesh;
    }
}