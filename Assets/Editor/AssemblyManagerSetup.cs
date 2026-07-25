using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AssemblyManagerSetup
{
    [MenuItem("ABB Tools/Populate AssemblyManager Zone List")]
    static void PopulateZones()
    {
        AssemblyManager manager = Object.FindFirstObjectByType<AssemblyManager>();
        if (manager == null)
        {
            Debug.LogError("No AssemblyManager found in scene.");
            return;
        }

        AssemblySnapZone[] allZones = Object.FindObjectsByType<AssemblySnapZone>(
            FindObjectsSortMode.None);

        manager.allZones = new List<AssemblySnapZone>(allZones);
        EditorUtility.SetDirty(manager);
        Debug.Log($"Populated AssemblyManager with {allZones.Length} zones.");
    }
}