using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class SpawnPreview : MonoBehaviour
{
    [Header("A simple flat plane or bounding box outline prefab")]
    public GameObject previewPrefab;

    [Header("Match the spawn layout footprint (width × depth in metres)")]
    public Vector3 previewScale = new Vector3(2.0f, 1f, 1.5f);

    private ARRaycastManager _raycastManager;
    private static List<ARRaycastHit> _hits = new List<ARRaycastHit>();
    private GameObject _preview;
    private bool _spawned = false;

    void Start()
    {
        _raycastManager = FindFirstObjectByType<ARRaycastManager>();
        _preview = Instantiate(previewPrefab);
        _preview.transform.localScale = previewScale;
        _preview.SetActive(false);
    }

    public void OnPartsSpawned() => _spawned = true; // Call from SwitchSpawnerMR

    void Update()
    {
        if (_spawned) { _preview.SetActive(false); return; }
        if (_raycastManager == null) return;

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (_raycastManager.Raycast(center, _hits, TrackableType.PlaneWithinBounds))
        {
            _preview.SetActive(true);
            _preview.transform.position = _hits[0].pose.position + Vector3.up * 0.005f;
            _preview.transform.rotation = _hits[0].pose.rotation * Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            _preview.SetActive(false);
        }
    }
}