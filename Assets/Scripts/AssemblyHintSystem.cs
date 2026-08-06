using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class AssemblyHintSystem : MonoBehaviour
{
    [Header("Assign a TextMeshPro UI text for hints (can be world-space or screen-space)")]
    public TextMeshProUGUI hintText;

    [Header("Gold highlight color for available parts")]
    public Color availableColor = new Color(1f, 0.85f, 0f, 1f);

    private MaterialPropertyBlock _propBlock;

    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
    }

    public void RefreshHints()
    {
        if (AssemblyManager.Instance == null) return;

        List<AssemblySnapZone> activeZones = AssemblyManager.Instance.allZones
            .Where(z => z.isActive && !z.isSatisfied)
            .ToList();

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
                hintText.text = "✓ Assembly Complete!";
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