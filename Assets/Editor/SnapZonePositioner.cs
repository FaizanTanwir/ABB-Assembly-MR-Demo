using UnityEngine;
using UnityEditor;

public class SnapZonePositioner : EditorWindow
{
    [MenuItem("ABB Tools/Print Part World Centers")]
    static void PrintPartCenters()
    {
        // Select the root ABB_Switch GameObject in the scene before running
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("Select the ABB_Switch root GameObject first.");
            return;
        }

        MeshRenderer[] renderers = selected.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            Vector3 center = r.bounds.center;
            Debug.Log($"{r.gameObject.name}: center = {center}, " +
                      $"size = {r.bounds.size}");
        }
    }
}