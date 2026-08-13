using System.Collections;
using UnityEngine;

/// <summary>
/// AR-Mobile-specific lost part recovery.
/// Replaces AssemblyHintSystem.RecoverLostParts() in AR-SampleScene.
/// Detects parts that fell through the floor (Y threshold) OR
/// were thrown outside the room (distance threshold).
/// Wire the RecoverButton OnClick to this component instead of
/// AssemblyHintSystem.RecoverLostParts.
/// Creates a permanent static floor collider at spawn Y so recovered parts
/// land reliably regardless of ARCore plane updates or camera movement.
/// </summary>
public class LostPartRecoveryAR : MonoBehaviour
{
    [Header("Assign the SnapZonesRoot (SnapZones-Transformed)")]
    public GameObject snapZonesRoot;

    [Header("Static floor collider")]
    [Tooltip("Width and depth of the invisible static floor in metres")]
    public float floorSize = 8f;

    [Header("Fall-through detection")]
    [Tooltip("Parts this far below the assembly origin Y are considered fallen through the floor")]
    public float fallThroughYThreshold = -0.08f; // 8 cm below origin

    [Header("Distance detection")]
    [Tooltip("Parts further than this from the assembly origin are considered lost sideways")]
    public float distanceThreshold = 2.0f;

    [Header("Recovery position")]
    [Tooltip("Height above the assembly origin where recovered parts are placed")]
    public float recoveryHeight = 0.2f;

    [Tooltip("XZ spread to avoid exact stacking on recovery")]
    public float recoverySpread = 0.05f;

    // Set by InitFloor — the Y of the detected plane at spawn time
    private float _floorY = float.NaN;

    // The static floor GameObject created at spawn time
    private GameObject _staticFloor;
    private Vector3 _spawnCenter = Vector3.zero;

    /// <summary>
    /// Called once by SwitchSpawnerAR immediately after SpawnAllParts.
    /// Creates a permanent BoxCollider at the detected plane position.
    /// </summary>
    public void InitFloor(Vector3 detectedPlanePose)
    {
        if (_staticFloor != null) return;

        _floorY      = detectedPlanePose.y;
        _spawnCenter = detectedPlanePose;

        _staticFloor = new GameObject("[AR_StaticFloor]");

        // Place on SwitchPart layer so it collides with all switch part
        // prefabs regardless of the Default↔SwitchPart matrix setting.
        // SwitchPart↔SwitchPart IS enabled (only Grabbed variant is disabled).
        int layer = LayerMask.NameToLayer("SwitchPart");
        _staticFloor.layer = (layer >= 0) ? layer : 0;

        BoxCollider bc = _staticFloor.AddComponent<BoxCollider>();
        bc.size   = new Vector3(floorSize, 0.04f, floorSize); // 4cm thick
        bc.center = Vector3.zero;
        bc.isTrigger = false;

        // Centre the floor under the spawn area, 2cm below detected Y
        _staticFloor.transform.position = new Vector3(
            _spawnCenter.x,
            _floorY - 0.02f,
            _spawnCenter.z);

        _staticFloor.transform.rotation = Quaternion.identity;

        Debug.Log($"[LostPartRecoveryAR] Static floor created." +
                  $" Layer={_staticFloor.layer}" +
                  $" Y={_floorY - 0.02f:F3}m" +
                  $" Centre=({_spawnCenter.x:F2}, {_spawnCenter.z:F2})" +
                  $" Size={floorSize}m x {floorSize}m.");
    }

    public void RecoverLostParts()
    {
        if (snapZonesRoot == null)
        {
            Debug.LogWarning("[LostPartRecoveryAR] snapZonesRoot is not assigned.");
            return;
        }

        Vector3 origin = snapZonesRoot.transform.position;

        // Use the static floor Y as the reference baseline.
        // Fall back to origin.y if InitFloor was never called.
        float baseY = float.IsNaN(_floorY) ? origin.y : _floorY;
        SwitchPart[] allParts = 
            FindObjectsByType<SwitchPart>(FindObjectsSortMode.None);

        int recovered = 0;

        foreach (var part in allParts)
        {
            Rigidbody rb = part.GetComponent<Rigidbody>();

            // Skip parts that are snapped (kinematic = placed in assembly)
            if (rb == null || rb.isKinematic) continue;

             float yOffset = part.transform.position.y - baseY;
            float dist    = Vector3.Distance(part.transform.position, origin);

            bool fellThrough = yOffset < fallThroughYThreshold;
            bool tooFar      = dist    > distanceThreshold;

            if (fellThrough || tooFar)
            {
                // Place above floor Y (not origin Y) so parts land on the static floor
                Vector3 target = new Vector3(
                    origin.x + Random.Range(-recoverySpread, recoverySpread),
                    baseY + recoveryHeight,
                    origin.z + Random.Range(-recoverySpread, recoverySpread));

                StartCoroutine(TeleportAndRelease(rb, target));
                recovered++;
            }
        }

        Debug.Log($"[LostPartRecoveryAR] Recovered {recovered} part(s). " +
                  $"Base Y = {baseY:F3}m. " +
                  $"Static floor present: {_staticFloor != null}.");
    }
    /// <summary>
    /// Makes the part kinematic, teleports it, waits one physics frame
    /// so the static floor BVH is fully registered, then re-enables physics.
    /// Without this the part can tunnel through in the same frame it is placed.
    /// </summary>
    private IEnumerator TeleportAndRelease(Rigidbody rb, Vector3 target)
    {
        rb.isKinematic = true;
        rb.transform.position = target;

        yield return new WaitForFixedUpdate();   // physics frame 1: floor is present
        yield return new WaitForFixedUpdate();   // physics frame 2: positions settled

        rb.isKinematic     = false;
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void OnDestroy()
    {
        if (_staticFloor != null)
            Destroy(_staticFloor);
    }
}