using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ForcePassthrough : MonoBehaviour
{
    void Start()
    {
        // Find AR Camera Manager and enable it directly
        ARCameraManager camManager = FindFirstObjectByType<ARCameraManager>();
        if (camManager != null)
        {
            camManager.enabled = true;
            Debug.Log("[ForcePassthrough] AR Camera Manager enabled.");
        }
        else
        {
            Debug.LogWarning("[ForcePassthrough] AR Camera Manager not found.");
        }
    }
}