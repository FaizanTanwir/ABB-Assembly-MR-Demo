using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class VisualGuideGenerator
{
    // ── Manual zone-to-part-name mapping ─────────────────────────────────────
    // Left side:  zone ID (must match AssemblySnapZone.zoneId exactly)
    // Right side: part name in the ABB_Switch hierarchy (the name you gave it)
    // Add or correct entries here if a guide ends up in the wrong place.
    private static readonly Dictionary<string, string> ZoneToPartName =
        new Dictionary<string, string>
    {
        // Driver Module
        { "BottomHousing_0",         "Bottom-Housing" },
        { "TopHousing_0",            "Top-Housing" },
        { "TopCover_0",              "Top-Cover" },
        { "OperatingShaft_0",        "Operating-Shaft" },
        { "ShaftReceiverCollar_0",   "Shaft-Receiver-Collar" },
        { "ClampingBolt_0",          "Clamping-Bolt" },
        { "InternalFastener_0_Left", "Internal-Fastener-L" },
        { "InternalFastener_0_Right","Internal-Fastener-R" },
        { "InterlockingNut_0_A",     "Interlocking-Nut-A" },
        { "InterlockingNut_0_B",     "Interlocking-Nut-B" },
        { "InterlockingNut_0_C",     "Interlocking-Nut-C" },
        { "InterlockingNut_0_D",     "Interlocking-Nut-D" },
        { "RedStrip_0",              "Red-Strip" },
        { "WhiteStrip_0",            "White-Strip" },
        { "ExternalFastener_0_Left", "External-Fastener-Drv-L" },
        { "ExternalFastener_0_Right","External-Fastener-Drv-R" },
        { "MountingClip_0_Left",     "Mounting-Clips-Drv-L" },
        { "MountingClip_0_Right",    "Mounting-Clips-Drv-R" },

        // Receiver Module 1
        { "RightHousing_1",          "Right-Housing-Rcv1" },
        { "LeftHousing_1",           "Left-Housing-Rcv1" },
        { "InterlockingDriveRing_1", "Interlocking-Drive-Ring-Rcv1" },
        { "ContactCover_1",          "Contact-Cover-Rcv1" },
        { "ContactWindow_1",         "Contact-Window-Rcv1" },
        { "TerminalLug_1_Left",      "Terminal-Lugs-Rcv1-L" },
        { "TerminalLug_1_Right",     "Terminal-Lugs-Rcv1-R" },

        // Receiver Module 2
        { "RightHousing_2",          "Right-Housing-Rcv2" },
        { "LeftHousing_2",           "Left-Housing-Rcv2" },
        { "InterlockingDriveRing_2", "Interlocking-Drive-Ring-Rcv2" },
        { "ContactCover_2",          "Contact-Cover-Rcv2" },
        { "ContactWindow_2",         "Contact-Window-Rcv2" },
        { "TerminalLug_2_Left",      "Terminal-Lugs-Rcv2-L" },
        { "TerminalLug_2_Right",     "Terminal-Lugs-Rcv2-R" },

        // Receiver Module 3
        { "RightHousing_3",          "Right-Housing-Rcv3" },
        { "LeftHousing_3",           "Left-Housing-Rcv3" },
        { "InterlockingDriveRing_3", "Interlocking-Drive-Ring-Rcv3" },
        { "ContactCover_3",          "Contact-Cover-Rcv3" },
        { "ContactWindow_3",         "Contact-Window-Rcv3" },
        { "TerminalLug_3_Left",      "Terminal-Lugs-Rcv3-L" },
        { "TerminalLug_3_Right",     "Terminal-Lugs-Rcv3-R" },
        { "ExternalFastener_3_Left", "External-Fastener-Rcv3-L" },
        { "ExternalFastener_3_Right","External-Fastener-Rcv3-R" },
        { "MountingClip_3_Left",     "Mounting-Clips-Rcv3-L" },
        { "MountingClip_3_Right",    "Mounting-Clips-Rcv3-R" },

        // Modular Rods
        { "ModularRod_A",            "Modular-Rod-A" },
        { "ModularRod_B",            "Modular-Rod-B" },
    };

    [MenuItem("ABB Tools/Generate Visual Snap Guides (Name-Based)")]
    static void Generate()
    {
        GameObject switchRef = GameObject.Find("ABB_Switch");
        if (switchRef == null)
        {
            Debug.LogError("Enable 'ABB_Switch' in the scene first.");
            return;
        }

        Material ghostMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Materials/SnapZoneGhostMaterial.mat");
        if (ghostMat == null)
        {
            Debug.LogError("SnapZoneGhostMaterial.mat not found at Assets/Materials/.");
            return;
        }

        // Build lookup: GameObject name → bounds center in world space
        Dictionary<string, Bounds> nameToBounds = new Dictionary<string, Bounds>();
        foreach (MeshRenderer r in switchRef.GetComponentsInChildren<MeshRenderer>())
        {
            string n = r.gameObject.name;
            if (!nameToBounds.ContainsKey(n))
                nameToBounds[n] = r.bounds;
        }

        // Find or create VisualGuides root
        GameObject guidesRoot = GameObject.Find("VisualGuides");
        if (guidesRoot != null)
        {
            Object.DestroyImmediate(guidesRoot);
            Debug.Log("[VisualGuideGenerator] Cleared old VisualGuides.");
        }
        guidesRoot = new GameObject("VisualGuides");
        guidesRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        AssemblySnapZone[] allZones =
            Object.FindObjectsByType<AssemblySnapZone>(FindObjectsSortMode.None);

        int matched = 0;
        int unmatched = 0;

        foreach (AssemblySnapZone zone in allZones)
        {
            string partName;
            Bounds b = default;
            bool hasMapping = ZoneToPartName.TryGetValue(zone.zoneId, out partName);
            bool hasBounds  = hasMapping && nameToBounds.TryGetValue(partName, out b);

            if (!hasBounds)
            {
                Debug.LogWarning($"[VisualGuideGenerator] No match for zone '{zone.zoneId}'. " +
                                 $"Mapping name: '{(hasMapping ? partName : "NOT IN MAP")}'. " +
                                 $"Guide placed at origin.");
                b = new Bounds(Vector3.zero, Vector3.one * 0.01f);
                unmatched++;
            }
            else
            {
                matched++;
            }

            // Create guide GameObject
            GameObject guide = new GameObject($"Guide_{zone.zoneId}");
            guide.transform.SetParent(guidesRoot.transform);
            guide.transform.position = b.center;
            guide.transform.rotation = Quaternion.identity;

            // Scale: use bounds size but enforce a readable minimum
            // bounds.size is in world units (already accounts for 0.002 switch scale)
            Vector3 size = b.size;
            float minSize = 0.015f; // 1.5cm minimum — visible but not huge
            size = new Vector3(
                Mathf.Max(size.x, minSize),
                Mathf.Max(size.y, minSize),
                Mathf.Max(size.z, minSize));
            guide.transform.localScale = size;

            MeshFilter   mf = guide.AddComponent<MeshFilter>();
            MeshRenderer mr = guide.AddComponent<MeshRenderer>();
            mf.sharedMesh      = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            mr.sharedMaterial  = ghostMat;
            mr.enabled         = false; // starts hidden

            VisualSnapGuide vsg  = guide.AddComponent<VisualSnapGuide>();
            vsg.linkedZoneId     = zone.zoneId;
            zone.visualGuide     = vsg;
            EditorUtility.SetDirty(zone);
        }

        EditorUtility.SetDirty(guidesRoot);

        Debug.Log($"[VisualGuideGenerator] Done. " +
                  $"Matched: {matched} | Failed (placed at origin): {unmatched}. " +
                  $"Fix names in the ZoneToPartName dictionary for any failures.");
    }

    [MenuItem("ABB Tools/Clear Visual Snap Guides")]
    static void Clear()
    {
        GameObject root = GameObject.Find("VisualGuides");
        if (root != null) Object.DestroyImmediate(root);

        foreach (AssemblySnapZone z in
            Object.FindObjectsByType<AssemblySnapZone>(FindObjectsSortMode.None))
        {
            z.visualGuide = null;
            EditorUtility.SetDirty(z);
        }
        Debug.Log("[VisualGuideGenerator] Cleared.");
    }
}