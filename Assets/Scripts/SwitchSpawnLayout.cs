using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SwitchSpawnLayout", 
                 menuName = "ABB/Switch Spawn Layout")]
public class SwitchSpawnLayout : ScriptableObject
{
    [System.Serializable]
    public class PartSpawnEntry
    {
        public GameObject partPrefab;
        public Vector3 localOffset;    // Offset from tap point
        public Vector3 localRotation;  // Initial rotation
        public string groupLabel;      // "Driver", "Receiver1", etc.
    }
    
    public List<PartSpawnEntry> parts;
}