using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Prevents tracked-head movement from bypassing solid room colliders.
///
/// Normal locomotion already uses the XR Origin CharacterController, but an
/// XR Device Simulator (and real room-scale movement) updates the camera pose
/// directly. This guard checks that camera movement against solid room objects
/// (walls, furniture, mannequin, props) and moves the XR Origin back to the
/// last reachable position when necessary.
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
    public float clearance = 0.02f;

    [Tooltip("Walkable surfaces whose names contain these words are ignored so the player is not blocked by the floor.")]
    public string[] walkableNameParts = { "Floor", "Ground", "Outside" };

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

        // Only correct horizontal clipping; vertical head movement stays free.
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
            distance + Mathf.Max(0f, clearance),
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        RaycastHit nearestHit = default(RaycastHit);
        bool foundBlocker = false;
        float nearestDistance = float.PositiveInfinity;

        foreach (RaycastHit hit in hits)
        {
            if (!IsBlockingCollider(hit.collider))
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestHit = hit;
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
            nearestHit.distance - Mathf.Max(0f, clearance));

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

    private bool IsBlockingCollider(Collider collider)
    {
        if (collider == null || collider.isTrigger)
            return false;

        if (collider.transform.IsChildOf(xrOrigin.transform))
            return false;

        if (IsWalkableSurface(collider.transform))
            return false;

        return true;
    }

    private bool IsWalkableSurface(Transform transform)
    {
        if (walkableNameParts == null || walkableNameParts.Length == 0)
            return false;

        Transform current = transform;

        while (current != null)
        {
            string objectName = current.name;

            for (int i = 0; i < walkableNameParts.Length; i++)
            {
                string part = walkableNameParts[i];
                if (string.IsNullOrEmpty(part))
                    continue;

                if (objectName.IndexOf(
                        part,
                        System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            current = current.parent;
        }

        return false;
    }
}
