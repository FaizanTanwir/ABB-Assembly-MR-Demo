using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class AssemblyHintSystem : MonoBehaviour
{
    [Header("Hint and counter TextMeshPro UI")]
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI counterText; // NEW: assign a second TMP for "X/45"

    [Header("Gold highlight color for available parts")]
    public Color availableColor = new Color(1f, 0.85f, 0f, 1f);

    [Header("Lost part recovery")]
    [Tooltip("Parts further than this distance from the assembly origin are considered lost")]
    public float lostPartDistance = 3.0f;

    private MaterialPropertyBlock _propBlock;
    private int _totalZones;

    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        _totalZones = 0;
    }

    void Start()
    {
        if (AssemblyManager.Instance != null)
            _totalZones = AssemblyManager.Instance.allZones.Count(z => !z.isOptional);

        UpdateCounter(0);
    }

    public void RefreshHints()
    {
        if (AssemblyManager.Instance == null) return;

        List<AssemblySnapZone> activeZones = AssemblyManager.Instance.allZones
            .Where(z => z.isActive && !z.isSatisfied)
            .ToList();

        int satisfiedCount = AssemblyManager.Instance.allZones
            .Count(z => z.isSatisfied && !z.isOptional);

        UpdateCounter(satisfiedCount);

        // Gather all currently spawned, unsnapped parts
        SwitchPart[] allParts = FindObjectsByType<SwitchPart>(FindObjectsSortMode.None);

        // Reset highlights on all unsnapped parts
        foreach (var part in allParts)
        {
            if (!IsSnapped(part))
                part.SetHighlight(HighlightState.None);
        }

        if (activeZones.Count == 0)
        {
            if (hintText != null)
                hintText.text = satisfiedCount >= _totalZones
                    ? "✓ Assembly Complete!"
                    : "All available parts placed.";
            return;
        }

        // Highlight parts that match any active zone
        HashSet<string> activeTypeNames = new HashSet<string>();
        int highlightCount = 0;

        foreach (var zone in activeZones)
        {
            activeTypeNames.Add(zone.acceptedPartType.ToString());

            foreach (var part in allParts)
            {
                if (IsSnapped(part)) continue;

                bool typeMatch = part.partType == zone.acceptedPartType;
                bool moduleMatch = zone.ignoreModuleIndex ||
                                   part.moduleIndex == zone.acceptedModuleIndex;

                if (typeMatch && moduleMatch)
                {
                    ApplyGoldHighlight(part);
                    highlightCount++;
                }
            }
        }

        if (hintText != null)
        {
            string partList = string.Join(", ", activeTypeNames);
            hintText.text = highlightCount > 0
                ? $"Place next (highlighted in gold):\n{partList}"
                : $"Find and place:\n{partList}";
        }
    }

    private void UpdateCounter(int satisfied)
    {
        if (counterText == null) return;
        float pct = _totalZones > 0 ? (satisfied / (float)_totalZones) * 100f : 0f;
        counterText.text = $"{satisfied}/{_totalZones} parts  ({pct:0}%)";
    }

    // ── Lost part recovery ────────────────────────────────────────────────────

    /// <summary>
    /// Call from a UI button. Teleports any part further than lostPartDistance
    /// back to its original spawn offset position.
    /// </summary>
    public void RecoverLostParts()
    {
        SwitchSpawnerMR spawner = FindFirstObjectByType<SwitchSpawnerMR>();
        if (spawner == null || spawner.SnapZonesRoot == null) return;

        Vector3 assemblyOrigin = spawner.SnapZonesRoot.transform.position;

        SwitchPart[] allParts = FindObjectsByType<SwitchPart>(FindObjectsSortMode.None);
        int recovered = 0;

        foreach (var part in allParts)
        {
            if (IsSnapped(part)) continue;

            float dist = Vector3.Distance(part.transform.position, assemblyOrigin);
            if (dist > lostPartDistance)
            {
                // Move part back above the assembly origin
                part.transform.position = assemblyOrigin + Vector3.up * 0.10f
                                          + Random.insideUnitSphere * 0.05f;
                var rb = part.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity        = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                recovered++;
            }
        }

        Debug.Log($"[RecoverLostParts] Recovered {recovered} parts.");
    }

    private bool IsSnapped(SwitchPart part)
    {
        var rb = part.GetComponent<Rigidbody>();
        return rb != null && rb.isKinematic;
    }

    private void ApplyGoldHighlight(SwitchPart part)
    {
        foreach (var r in part.GetComponentsInChildren<Renderer>())
        {
            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", availableColor);
            r.SetPropertyBlock(_propBlock);
        }
    }
}