using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class SwitchSpawner : MonoBehaviour
{
    public SwitchSpawnLayout layout;
    public GameObject snapZonesRoot; // Parent of all your snap zone GameObjects
    
    private ARRaycastManager _raycastManager;
    private static List<ARRaycastHit> _hits = new List<ARRaycastHit>();
    
    private bool _spawned = false;
    private List<GameObject> _spawnedParts = new List<GameObject>();
    
    void Start()
    {
        _raycastManager = FindObjectOfType<ARRaycastManager>();
        // Hide snap zones until parts are spawned
        snapZonesRoot.SetActive(false);
    }
    
    void Update()
    {
        if (_spawned) return;
        if (Input.touchCount == 0) return;
        
        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;
        
        if (_raycastManager.Raycast(touch.position, _hits, 
            TrackableType.PlaneWithinBounds))
        {
            Pose hitPose = _hits[0].pose;
            SpawnAllParts(hitPose);
        }
    }
    
    private void SpawnAllParts(Pose tablePose)
    {
        _spawned = true;
        
        // Position snap zones at the center of the table area
        snapZonesRoot.transform.position = tablePose.position;
        snapZonesRoot.transform.rotation = tablePose.rotation;
        snapZonesRoot.SetActive(true);
        
        // Spawn all parts in layout positions around the table
        foreach (var entry in layout.parts)
        {
            Vector3 worldOffset = tablePose.rotation * entry.localOffset;
            Vector3 spawnPos = tablePose.position + worldOffset 
                               + Vector3.up * 0.05f; // 5cm above table
            
            Quaternion spawnRot = tablePose.rotation * 
                                  Quaternion.Euler(entry.localRotation);
            
            GameObject part = Instantiate(entry.partPrefab, spawnPos, spawnRot);
            _spawnedParts.Add(part);
        }
        
        // Notify AssemblyManager that assembly can begin
        AssemblyManager.Instance.enabled = true;
    }
    
    public void ResetAssembly() // Call from Reset button later
    {
        foreach (var part in _spawnedParts)
            Destroy(part);
        _spawnedParts.Clear();
        
        snapZonesRoot.SetActive(false);
        _spawned = false;
        
        // Reset all zones in AssemblyManager
        AssemblyManager.Instance.ResetAllZones();
    }
}