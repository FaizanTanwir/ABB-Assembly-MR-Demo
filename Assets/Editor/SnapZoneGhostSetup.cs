using UnityEngine;
using UnityEditor;

public class SnapZoneGhostSetup
{
    [MenuItem("ABB Tools/Add Ghost Meshes to All Snap Zones")]
    static void AddGhostMeshes()
    {
        // Find the ghost material — must exist at this path
        Material ghostMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Materials/SnapZoneGhostMaterial.mat");

        if (ghostMat == null)
        {
            Debug.LogError("Create SnapZoneGhostMaterial.mat in Assets/Materials/ first.");
            return;
        }

        AssemblySnapZone[] zones = Object.FindObjectsByType<AssemblySnapZone>(
            FindObjectsSortMode.None);

        int added = 0;
        foreach (var zone in zones)
        {
            // Skip if already has a renderer
            if (zone.GetComponent<MeshRenderer>() != null) continue;

            // Add a cube primitive mesh as placeholder ghost
            MeshFilter mf = zone.gameObject.AddComponent<MeshFilter>();
            MeshRenderer mr = zone.gameObject.AddComponent<MeshRenderer>();

            // Use Unity's built-in cube mesh
            mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            mr.sharedMaterial = ghostMat;

            // Scale the ghost to match the zone's box collider if present
            BoxCollider bc = zone.GetComponent<BoxCollider>();
            if (bc != null)
            {
                zone.transform.localScale = bc.size;
            }

            EditorUtility.SetDirty(zone.gameObject);
            added++;
        }

        Debug.Log($"Added ghost meshes to {added} snap zones.");
    }
}