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
            _zoneLookup[zone.zoneId] = zone;
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
    }
    
    private void EvaluateZone(AssemblySnapZone zone)
    {
        if (zone.isSatisfied) return;
        
        // A zone is active if ALL its prerequisites are satisfied
        bool prerequisitesMet = zone.prerequisiteZoneIds.All(prereqId =>
        {
            if (_zoneLookup.TryGetValue(prereqId, out var prereqZone))
                return prereqZone.isSatisfied;
            return true; // Missing prerequisite treated as met (safe fallback)
        });
        
        zone.SetZoneActive(prerequisitesMet);
    }
    
    private void CheckAssemblyComplete()
    {
        bool allComplete = allZones.All(z => z.isSatisfied);
        if (allComplete)
        {
            Debug.Log("Assembly Complete!");
            // Trigger completion feedback — add UI/sound later
            OnAssemblyComplete();
        }
    }
    
    private void OnAssemblyComplete()
    {
        // Placeholder — you will add celebration UI here later
        // For now, just log
        Debug.Log("OT200E03 Assembly Complete! All components secured.");
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