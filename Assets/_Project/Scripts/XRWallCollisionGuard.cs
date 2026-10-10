using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Prevents tracked-head movement from bypassing room walls and the metal desk.
///
/// Normal locomotion already uses the XR Origin CharacterController, but an
/// XR Device Simulator (and real room-scale movement) updates the camera pose
/// directly. This guard checks that camera movement against the room walls and
/// moves the XR Origin back to the last reachable position when necessary.
/// </summary>
[DisallowMultipleComponent]
public class XRWallCollisionGuard : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public Camera headCamera;

    [Header("Collision")]
    [Min(0.05f)]
    public float headRadius = 0.18f;

    [Min(0f)]
    public float wallClearance = 0.02f;

    [Tooltip("Colliders under objects whose names begin with this text block the tracked head.")]
    public string wallNamePrefix = "Wall_";

    [Tooltip("Colliders under objects whose names begin with this text block the user's torso.")]
    public string deskNamePrefix = "metal_office_desk_v2";

    [Min(0.05f)]
    public float deskCollisionRadius = 0.25f;

    [Min(0f)]
    public float deskCollisionHeightOffset = 0.9f;

    private Vector3 previousHeadPosition;
    private bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnXROrigins()
    {
        XROrigin[] origins = FindObjectsOfType<XROrigin>();

        foreach (XROrigin origin in origins)
        {
            if (origin.GetComponent<XRWallCollisionGuard>() == null)
                origin.gameObject.AddComponent<XRWallCollisionGuard>();
        }
    }

    private void Awake()
    {
        if (xrOrigin == null)
            xrOrigin = GetComponent<XROrigin>();

        if (xrOrigin != null && xrOrigin.Camera != null)
            headCamera = xrOrigin.Camera;
        else if (headCamera == null)
            headCamera = Camera.main;

        if (xrOrigin == null || headCamera == null)
        {
            Debug.LogError(
                "XRWallCollisionGuard requires an XR Origin and its head camera.",
                this);

            enabled = false;
        }
    }

    private void Start()
    {
        previousHeadPosition = headCamera.transform.position;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        Vector3 currentHeadPosition = headCamera.transform.position;
        Vector3 movement = currentHeadPosition - previousHeadPosition;

        // Walls are vertical, so height changes should not be blocked.
        Vector3 horizontalMovement =
            Vector3.ProjectOnPlane(movement, Vector3.up);

        float distance = horizontalMovement.magnitude;
        if (distance <= 0.0001f)
        {
            previousHeadPosition = currentHeadPosition;
            return;
        }

        Vector3 direction = horizontalMovement / distance;
        RaycastHit[] hits = Physics.SphereCastAll(
            previousHeadPosition,
            Mathf.Max(0.05f, headRadius),
            direction,
            distance + Mathf.Max(0f, wallClearance),
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        RaycastHit nearestBlockingHit = default(RaycastHit);
        bool foundBlocker = false;
        float nearestDistance = float.PositiveInfinity;

        foreach (RaycastHit hit in hits)
        {
            if (!IsRoomWall(hit.collider))
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestBlockingHit = hit;
                foundBlocker = true;
            }
        }

        Vector3 deskCastOrigin =
            previousHeadPosition - Vector3.up * deskCollisionHeightOffset;
        RaycastHit[] deskHits = Physics.SphereCastAll(
            deskCastOrigin,
            Mathf.Max(0.05f, deskCollisionRadius),
            direction,
            distance + Mathf.Max(0f, wallClearance),
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in deskHits)
        {
            if (!IsDesk(hit.collider))
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestBlockingHit = hit;
                foundBlocker = true;
            }
        }

        if (!foundBlocker)
        {
            previousHeadPosition = currentHeadPosition;
            return;
        }

        float allowedDistance = Mathf.Max(
            0f,
            nearestBlockingHit.distance - Mathf.Max(0f, wallClearance));

        Vector3 allowedHeadPosition =
            previousHeadPosition + direction * allowedDistance;

        // Preserve the tracked vertical position while correcting horizontal
        // penetration by moving the complete XR Origin.
        allowedHeadPosition.y = currentHeadPosition.y;

        Vector3 correction = allowedHeadPosition - currentHeadPosition;
        correction.y = 0f;

        xrOrigin.transform.position += correction;
        previousHeadPosition = headCamera.transform.position;
    }

    private bool IsRoomWall(Collider collider)
    {
        if (collider == null || string.IsNullOrEmpty(wallNamePrefix))
            return false;

        if (collider.transform.IsChildOf(xrOrigin.transform))
            return false;

        Transform current = collider.transform;

        while (current != null)
        {
            if (current.name.StartsWith(
                    wallNamePrefix,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsDesk(Collider collider)
    {
        if (collider == null
            || string.IsNullOrEmpty(deskNamePrefix)
            || collider.transform.IsChildOf(xrOrigin.transform))
            return false;

        Transform current = collider.transform;

        while (current != null)
        {
            if (current.name.StartsWith(
                    deskNamePrefix,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
