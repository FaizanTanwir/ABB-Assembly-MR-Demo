using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// AR Mobile version of the switch spawner.
/// Editor:  left mouse click → AR raycast or fallback spawn.
/// Android: screen tap      → AR raycast (ARCore plane detection).
/// Mirrors all features of SwitchSpawnerMR except controller input.
/// </summary>
public class SwitchSpawnerAR : MonoBehaviour
{
    [Header("Layout and Roots")]
    public SwitchSpawnLayout layout;
    public GameObject snapZonesRoot; // Parent of all your snap zone GameObjects
    public GameObject visualGuidesRoot;

    [Header("Spawn Settings")]
    [Tooltip("Fallback distance in front of camera when no plane is hit")]
    public float fallbackSpawnDistance = 1.0f;

    [Tooltip("Vertical offset from camera when using fallback spawn")]
    public float fallbackYOffset = -0.5f;

    // Public accessor used by AssemblyHintSystem.RecoverLostParts
    public GameObject SnapZonesRoot => snapZonesRoot;
    private ARRaycastManager _raycastManager;
    private static List<ARRaycastHit> _hits = new List<ARRaycastHit>();
    
    private bool _spawned = false;
    private List<GameObject> _spawnedParts = new List<GameObject>();
    // ─── Lifecycle ────────────────────────────────────────────────────────────
    void Start()
    {
        _raycastManager = FindFirstObjectByType<ARRaycastManager>();
        // Hide snap zones until parts are spawned
        if (snapZonesRoot    != null) snapZonesRoot.SetActive(false);
        if (visualGuidesRoot != null) visualGuidesRoot.SetActive(false);

        if (_raycastManager == null)
            Debug.LogWarning("[SwitchSpawner] No ARRaycastManager found. " +
                             "Fallback spawn will be used.");
    }
    // ─── Input Detection ──────────────────────────────────────────────────────
    void Update()
    {
        if (_spawned)
            return;

    #if UNITY_EDITOR
        // ── Editor: left mouse click ─────────────────────────────────────────
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Debug.Log("[SwitchSpawner] Editor mouse click.");
        TrySpawnFromScreenPosition(Mouse.current.position.ReadValue());

    #else
            // ── Android: first finger tap ─────────────────────────────────────────
            if (Touchscreen.current == null) return;

            var touch = Touchscreen.current.primaryTouch;
            if (!touch.press.wasPressedThisFrame) return;

            Vector2 touchPos = touch.position.ReadValue();
            Debug.Log($"[SwitchSpawner] Touch at {touchPos}.");
            TrySpawnFromScreenPosition(touchPos);
    #endif
    }
    // ─── Spawn Attempts ───────────────────────────────────────────────────────

    /// <summary>
    /// Tries AR Foundation raycast first, then physics raycast, then fallback.
    /// Same priority chain as SwitchSpawnerMR.TrySpawnFromScreenPosition.
    /// </summary>
    private void TrySpawnFromScreenPosition(Vector2 screenPos)
    {
        // 1. AR Foundation raycast against ARCore-detected planes
        if (_raycastManager != null &&
            _raycastManager.Raycast(screenPos, _hits, TrackableType.PlaneWithinBounds))
        {
            Debug.Log("[SwitchSpawner] AR plane hit.");
            SpawnAllParts(_hits[0].pose);
            return;
        }

        // 2. Physics raycast against MeshColliders added by ARPlaneColliderGenerator
        Camera cam = Camera.main;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 5.0f))
            {
                Debug.Log($"[SwitchSpawner] Physics hit: {hit.collider.gameObject.name}");
                Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
                Quaternion rot = fwd.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(fwd)
                    : Quaternion.identity;
                SpawnAllParts(new Pose(hit.point, rot));
                return;
            }
        }

        // 3. Fallback: fixed position in front of camera
        SpawnAtFallback();
    }

    private void SpawnAtFallback()
    {
        Camera cam = Camera.main;
        Vector3 pos = cam != null
            ? cam.transform.position
              + cam.transform.forward * fallbackSpawnDistance
              + Vector3.up * fallbackYOffset
            : Vector3.zero;

        Debug.Log($"[SwitchSpawner] Fallback spawn at {pos}.");
        SpawnAllParts(new Pose(pos, Quaternion.identity));
    }
    // ─── Core Spawn ───────────────────────────────────────────────────────────
    private void SpawnAllParts(Pose tablePose)
    {
        _spawned = true;
        
        // Snap zones and visual guides move to the tap position.
        // tablePose.rotation keeps both horizontal (matching the floor plane normal).
        Quaternion rootRotation = tablePose.rotation;

        // To align with the vertical assembly zone instead, swap to:
        //     Quaternion.Euler(-90f, 0f, 0f)
        // Correction rotation: counteracts the 90 X rotation baked into mesh vertices.
        // Adjust the Y value if the driver module faces the wrong horizontal direction.
        Quaternion assemblyCorrection = Quaternion.Euler(-90f, 0f, 0f);
        Quaternion assemblyRotation   = tablePose.rotation * assemblyCorrection;

        if (snapZonesRoot != null)
        {
            snapZonesRoot.transform.position = tablePose.position;
            snapZonesRoot.transform.rotation = rootRotation; // To rotate the visual guides upright
            //snapZonesRoot.transform.rotation = assemblyRotation; // To rotate the assembly zone (snap zones) upright
            
            snapZonesRoot.SetActive(true);
        }

        if (visualGuidesRoot != null)
        {
            visualGuidesRoot.transform.position = tablePose.position;
            visualGuidesRoot.transform.rotation = rootRotation; // To rotate the visual guides upright
            //visualGuidesRoot.transform.rotation = assemblyRotation; // To rotate the assembly zone (snap zones) upright
            visualGuidesRoot.SetActive(true);
        }

        
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
            Debug.Log($"[SwitchSpawner] Spawned {entry.groupLabel} at {spawnPos}");
        }
        
        // Notify AssemblyManager that assembly can begin
        AssemblyManager.Instance.enabled = true;

        // Notify SpawnPreview to hide the placement ghost
        FindFirstObjectByType<SpawnPreview>()?.OnPartsSpawned();

        // ── NEW: create static floor collider at the detected plane ─────────────
        // This gives recovered parts a permanent surface to land on regardless of
        // ARCore plane updates or camera movement.
        FindFirstObjectByType<LostPartRecoveryAR>()?.InitFloor(tablePose.position);


        // Refresh gold highlights and counter after AssemblyManager initialises
        StartCoroutine(RefreshHintsAfterDelay());
    }

    // ─── Reset ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Destroys all spawned parts and hides roots. Wire to a Reset UI button.
    /// </summary>
    public void ResetAssembly() // Call from Reset button later
    {
        foreach (var part in _spawnedParts)
            Destroy(part);
        _spawnedParts.Clear();
        
        if (snapZonesRoot    != null) snapZonesRoot.SetActive(false);
        if (visualGuidesRoot != null) visualGuidesRoot.SetActive(false);

        _spawned = false;
        
        // Reset all zones in AssemblyManager
        AssemblyManager.Instance.ResetAllZones();
    }

    // ─── Utilities ────────────────────────────────────────────────────────────

    private System.Collections.IEnumerator RefreshHintsAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        FindFirstObjectByType<AssemblyHintSystem>()?.RefreshHints();
    }
}