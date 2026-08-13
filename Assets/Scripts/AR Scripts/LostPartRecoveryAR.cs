using UnityEngine;

/// <summary>
/// AR-Mobile-specific lost part recovery.
/// Replaces AssemblyHintSystem.RecoverLostParts() in AR-SampleScene.
/// Detects parts that fell through the floor (Y threshold) OR
/// were thrown outside the room (distance threshold).
/// Wire the RecoverButton OnClick to this component instead of
/// AssemblyHintSystem.RecoverLostParts.
/// </summary>
public class ARLostPartRecovery : MonoBehaviour
{
    [Header("Assign the SnapZonesRoot (SnapZones-Transformed)")]
    public GameObject snapZonesRoot;

    [Header("Fall-through detection")]
    [Tooltip("Parts this far below the assembly origin Y are considered fallen through the floor")]
    public float fallThroughYThreshold = -0.10f; // 10 cm below origin

    [Header("Distance detection")]
    [Tooltip("Parts further than this from the assembly origin are considered lost sideways")]
    public float distanceThreshold = 2.0f;

    [Header("Recovery position")]
    [Tooltip("Height above the assembly origin where recovered parts are placed")]
    public float recoveryHeight = 0.20f;

    [Tooltip("Maximum random XZ spread when placing recovered parts, to avoid exact stacking")]
    public float recoverySpread = 0.08f;

    public void RecoverLostParts()
    {
        if (snapZonesRoot == null)
        {
            Debug.LogWarning("[ARLostPartRecovery] snapZonesRoot is not assigned.");
            return;
        }

        Vector3 origin = snapZonesRoot.transform.position;
        SwitchPart[] allParts = 
            FindObjectsByType<SwitchPart>(FindObjectsSortMode.None);

        int recovered = 0;

        foreach (var part in allParts)
        {
            Rigidbody rb = part.GetComponent<Rigidbody>();

            // Skip parts that are snapped (kinematic = placed in assembly)
            if (rb == null || rb.isKinematic) continue;

            Vector3 pos    = part.transform.position;
            float   yOffset = pos.y - origin.y;
            float   dist    = Vector3.Distance(pos, origin);

            bool fellThrough = yOffset < fallThroughYThreshold;
            bool tooFar      = dist    > distanceThreshold;

            if (fellThrough || tooFar)
            {
                // Place above origin with random spread to avoid stacking
                part.transform.position = origin
                    + Vector3.up * recoveryHeight
                    + new Vector3(
                        Random.Range(-recoverySpread, recoverySpread),
                        0f,
                        Random.Range(-recoverySpread, recoverySpread));

                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                recovered++;
            }
        }

        Debug.Log($"[ARLostPartRecovery] Recovered {recovered} part(s). " +
                  $"Origin={origin}, fallThreshold={fallThroughYThreshold}m below, " +
                  $"distThreshold={distanceThreshold}m.");
    }
}