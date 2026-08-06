using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AssemblyManager : MonoBehaviour
{
    public static AssemblyManager Instance { get; private set; }
    
    [Header("All Snap Zones in the Scene")]
    public List<AssemblySnapZone> allZones;
    
    // Quick lookup by zoneId
    private Dictionary<string, AssemblySnapZone> _zoneLookup;
    
    void Awake()
    {
        Instance = this;
        _zoneLookup = new Dictionary<string, AssemblySnapZone>();
        foreach (var zone in allZones)
            {
                if (!string.IsNullOrEmpty(zone.zoneId))
                    _zoneLookup[zone.zoneId] = zone;
                else
                    Debug.LogWarning($"Zone on {zone.gameObject.name} has no zoneId set.");
            }
    }
    
    void Start()
    {
        // Evaluate all zones at start — 
        // zones with no prerequisites activate immediately
        foreach (var zone in allZones)
            EvaluateZone(zone);
    }
    
    public void OnZoneSatisfied(string satisfiedZoneId)
    {
        // When a zone is satisfied, re-evaluate ALL zones
        // because satisfying one may unlock others
        foreach (var zone in allZones)
        {
            if (!zone.isSatisfied)
                EvaluateZone(zone);
        }
        
        CheckAssemblyComplete();

        FindFirstObjectByType<AssemblyHintSystem>()?.RefreshHints();
    }
    
    public void EvaluateZone(AssemblySnapZone zone)
    {
        if (zone.isSatisfied) return;
        
        // AND check: every zone in prerequisiteZoneIds must be satisfied
        bool andMet = zone.prerequisiteZoneIds.All(id =>
        {
            if (_zoneLookup.TryGetValue(id, out var prereq))
                return prereq.isSatisfied;
            Debug.LogWarning($"Zone '{zone.zoneId}' references unknown prerequisite '{id}'");
            return true; // Unknown prereq treated as met to avoid deadlock
        });

        // OR check: at least one in orPrerequisiteZoneIds must be satisfied
        // Empty list means no OR condition — treat as met
        bool orMet = zone.orPrerequisiteZoneIds.Count == 0 ||
                     zone.orPrerequisiteZoneIds.Any(id =>
                         _zoneLookup.TryGetValue(id, out var prereq) && prereq.isSatisfied);

        zone.SetZoneActive(andMet && orMet);
    }
    
    private void CheckAssemblyComplete()
    {
        // Only non-optional zones count toward completion
        bool allComplete = allZones
            .Where(z => !z.isOptional)
            .All(z => z.isSatisfied);

        if (allComplete)
        {
            Debug.Log("OT200E03 Assembly Complete!");
            // Trigger completion feedback — add UI/sound later
            OnAssemblyComplete();
        }
    }
    
    private void OnAssemblyComplete()
    {
        // Placeholder — you will add celebration UI here later
        // For now, just log
        Debug.Log("All components secured.");
    }

        // ADDING THIS MISSING METHOD (IT IS BEING CALLED FROM SwitchSpawner.cs):
    public void ResetAllZones()
    // Re-evaluate to make sure start zones stay open and locked ones stay closed
    {
        // First pass: reset all state
        foreach (var zone in allZones)
        {
            zone.isSatisfied = false;
            zone.SetZoneActive(false); // Reset visual to inactive before re-evaluating 
        }

        // Second pass: re-evaluate which zones should now be active
        foreach (var zone in allZones)
        {
            EvaluateZone(zone);
        }
        Debug.Log("-> AssemblyManager: All snap zones have been reset.");
    }
}