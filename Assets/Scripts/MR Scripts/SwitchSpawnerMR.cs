using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// MR (Meta Quest) version of SwitchSpawner.
/// Editor: left mouse click to spawn (with AR raycast or fallback).
/// Quest: right controller grip button to spawn (physics raycast against 
///        AR plane meshes, with fallback to fixed position).
/// </summary>
public class SwitchSpawnerMR : MonoBehaviour
{
    public SwitchSpawnLayout layout;
    public GameObject snapZonesRoot;

    [Header("Visual Guides Root")]
    public GameObject visualGuidesRoot;

    [Header("Quest Input")]
    [Tooltip("Assign: XRI RightHand/Select from the XRI Default Input Actions asset")]
    public InputActionReference spawnAction;

    [Header("Spawn Settings")]
    [Tooltip("Fallback distance in front of camera when no surface is hit")]
    public float fallbackSpawnDistance = 0.2f;

    [Tooltip("How far below eye level to place parts when using fallback spawn")]
    public float fallbackYOffset = -0.5f;

    // AR Foundation raycast — present in MR template only if AR Raycast Manager
    // is on XR Origin. If not, physics raycast is used instead.
    private ARRaycastManager _arRaycastManager;
    private static List<ARRaycastHit> _arHits = new List<ARRaycastHit>();

    private bool _spawned = false;
    private List<GameObject> _spawnedParts = new List<GameObject>();

    void Start()
    {
        _arRaycastManager = FindFirstObjectByType<ARRaycastManager>();
        // Hide both roots until the user spawns
        if (snapZonesRoot   != null) snapZonesRoot.SetActive(false);
        if (visualGuidesRoot != null) visualGuidesRoot.SetActive(false);

        // Subscribe to controller trigger input
        if (spawnAction != null)
        {
            spawnAction.action.Enable();
            spawnAction.action.performed += OnSpawnActionPerformed;
        }
        else
        {
            Debug.LogWarning("SwitchSpawnerMR: No spawnAction assigned. " +
                             "Assign Right Hand Trigger in the Inspector.");
        }
    }

    void OnDestroy()
    {
        if (spawnAction != null)
            spawnAction.action.performed -= OnSpawnActionPerformed;
    }

    // ─── Quest Controller Input ───────────────────────────────────────────────
    private void OnSpawnActionPerformed(InputAction.CallbackContext context)
    {
#if !UNITY_EDITOR
        if (_spawned) return;
        TrySpawnFromCameraForward();
#endif
    }

    // ─── Editor Mouse Input ───────────────────────────────────────────────────
    void Update()
    {
        if (_spawned) return;

#if UNITY_EDITOR
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Debug.Log("[SwitchSpawnerMR] Editor mouse click detected.");
        TrySpawnFromScreenPosition(Mouse.current.position.ReadValue());
#endif
    }

    // ─── Spawn Logic ──────────────────────────────────────────────────────────

    /// <summary>
    /// Tries to spawn using a screen-space position (editor mouse or screen center).
    /// Attempts AR Foundation raycast first, then physics raycast, then fallback.
    /// </summary>
    private void TrySpawnFromScreenPosition(Vector2 screenPos)
    {
        // 1. AR Foundation raycast (works if AR Raycast Manager is on XR Origin)
        if (_arRaycastManager != null &&
            _arRaycastManager.Raycast(screenPos, _arHits, TrackableType.PlaneWithinBounds))
        {
            Debug.Log("[SwitchSpawnerMR] AR plane hit via AR Raycast Manager.");
            SpawnAllParts(_arHits[0].pose);
            return;
        }

        // 2. Physics raycast (hits AR plane MeshColliders from ARPlaneColliderGenerator)
        Camera cam = Camera.main;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 5.0f))
            {
                Debug.Log($"[SwitchSpawnerMR] Physics hit: {hit.collider.gameObject.name}");
                Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
                Quaternion rot = forward.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(forward)
                    : Quaternion.identity;
                SpawnAllParts(new Pose(hit.point, rot));
                return;
            }
        }

        // 3. Fallback: fixed position in front of camera
        SpawnAtFallback();
    }

    /// <summary>
    /// Used by Quest controller trigger — raycasts from camera forward.
    /// </summary>
    private void TrySpawnFromCameraForward()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            SpawnAtFallback();
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        // 1. AR Foundation raycast from screen center
        if (_arRaycastManager != null &&
            _arRaycastManager.Raycast(screenCenter, _arHits, TrackableType.PlaneWithinBounds))
        {
            Debug.Log("[SwitchSpawnerMR] Quest: AR plane hit.");
            SpawnAllParts(_arHits[0].pose);
            return;
        }

        // 2. Physics raycast from camera forward
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 3.0f))
        {
            Debug.Log($"[SwitchSpawnerMR] Quest: Physics hit {hit.collider.gameObject.name}");
            Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            Quaternion rot = forward.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(forward)
                : Quaternion.identity;
            SpawnAllParts(new Pose(hit.point, rot));
            return;
        }

        // 3. Fallback
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

        Debug.Log($"[SwitchSpawnerMR] Fallback spawn at {pos}");
        SpawnAllParts(new Pose(pos, Quaternion.identity));
    }

    // ─── Core Spawn and Reset ─────────────────────────────────────────────────

    private void SpawnAllParts(Pose tablePose)
    {
        _spawned = true;

        // ── Move snap zones to tap position ──────────────────────────────────
        if (snapZonesRoot != null)
        {
            snapZonesRoot.transform.position = tablePose.position;
            snapZonesRoot.transform.rotation = tablePose.rotation;
            snapZonesRoot.SetActive(true);
        }

        // ── Move visual guides to tap position ───────────────────────────────
        // The guides' LOCAL positions encode the part offset from the switch origin.
        // Moving the root to tablePose.position offsets all guides correctly.
        if (visualGuidesRoot != null)
        {
            visualGuidesRoot.transform.position = tablePose.position;
            visualGuidesRoot.transform.rotation = tablePose.rotation;
            visualGuidesRoot.SetActive(true);
        }

        // ── Spawn part instances ──────────────────────────────────────────────
        foreach (var entry in layout.parts)
        {
            Vector3 worldOffset = tablePose.rotation * entry.localOffset;
            Vector3 spawnPos = tablePose.position + worldOffset + Vector3.up * 0.05f;
            Quaternion spawnRot = tablePose.rotation * Quaternion.Euler(entry.localRotation);

            GameObject part = Instantiate(entry.partPrefab, spawnPos, spawnRot);

            SwitchPart switchPart = part.GetComponent<SwitchPart>();
            if (switchPart != null)
                switchPart.moduleIndex = entry.moduleIndex;

            _spawnedParts.Add(part);
            Debug.Log($"[SwitchSpawnerMR] Spawned {entry.groupLabel} at {spawnPos}");
        }

        AssemblyManager.Instance.enabled = true;
        StartCoroutine(RefreshHintsAfterDelay());
    }

    public void ResetAssembly()
    {
        foreach (var part in _spawnedParts)
            Destroy(part);
        _spawnedParts.Clear();

        if (snapZonesRoot    != null) snapZonesRoot.SetActive(false);
        if (visualGuidesRoot != null) visualGuidesRoot.SetActive(false);
        _spawned = false;

        AssemblyManager.Instance.ResetAllZones();
    }

    private System.Collections.IEnumerator RefreshHintsAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        FindFirstObjectByType<AssemblyHintSystem>()?.RefreshHints();
    }
}