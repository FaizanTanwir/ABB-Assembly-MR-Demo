using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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
        _raycastManager = FindFirstObjectByType<ARRaycastManager>();
        // Hide snap zones until parts are spawned
        snapZonesRoot.SetActive(false);
    }
    
    void Update()
    {
        if (_spawned)
            return;

    #if UNITY_EDITOR

        // ===============================
        // UNITY EDITOR (Mouse Input)
        // ===============================

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("Mouse Click Detected");
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();

        // First try the normal AR raycast against simulated planes
        if (_raycastManager != null &&
            _raycastManager.Raycast(mousePos, _hits, TrackableType.PlaneWithinBounds))
        {
            Debug.Log("Editor: AR plane hit.");
            SpawnAllParts(_hits[0].pose);
        }
        else
        {
            // Fallback: spawn one meter in front of the camera
            Camera cam = Camera.main;

            if (cam == null)
            {
                Debug.LogWarning("Main Camera not found.");
                return;
            }

            Vector3 spawnPosition =
                cam.transform.position +
                cam.transform.forward * 1.0f;

            spawnPosition.y = 0f;

            SpawnAllParts(
                new Pose(
                    spawnPosition,
                    Quaternion.identity));

            Debug.Log("Editor fallback spawn used.");
        }

    #else

        // ===============================
        // ANDROID / META QUEST
        // ===============================

        if (Touchscreen.current == null)
            return;

        var primaryTouch = Touchscreen.current.primaryTouch;
        Debug.Log("Touch Detected");

        if (!primaryTouch.press.wasPressedThisFrame)
            return;

        Vector2 touchPosition = primaryTouch.position.ReadValue();

        if (_raycastManager != null &&
            _raycastManager.Raycast(
                touchPosition,
                _hits,
                TrackableType.PlaneWithinBounds))
        {
            Debug.Log("Device: AR plane hit.");
            SpawnAllParts(_hits[0].pose);
        }

    #endif
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
            
            Debug.Log($"Spawning {entry.groupLabel} at {spawnPos}");
            GameObject part = Instantiate(entry.partPrefab, spawnPos, spawnRot);

            // KEY CHANGE: override moduleIndex from the layout entry
            SwitchPart switchPart = part.GetComponent<SwitchPart>();
            if (switchPart != null)
                switchPart.moduleIndex = entry.moduleIndex;
                
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