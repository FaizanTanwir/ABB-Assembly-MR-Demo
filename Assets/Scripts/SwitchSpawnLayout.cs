using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SwitchSpawnLayout", 
                 menuName = "ABB/Switch Spawn Layout")]
public class SwitchSpawnLayout : ScriptableObject
{
    [System.Serializable]
    public class PartSpawnEntry
    {
        [Header("What to spawn")]
        public GameObject partPrefab;
        
        [Header("Module identity — set this per instance")]
        public int moduleIndex; // 0=Driver, 1=Receiver1, 2=Receiver2, 3=Receiver3, -1=Global
        
        [Header("Where to spawn (relative to tap point)")]
        public Vector3 localOffset;
        public Vector3 localRotation;
        
        [Header("Label for Inspector readability")]
        public string groupLabel; // e.g. "Driver-BottomHousing", "Receiver1-LeftHousing"
    }
    
    public List<PartSpawnEntry> parts;
}