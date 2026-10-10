using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Prevents tracked head and controller movement from clipping through solid
/// room colliders (walls, furniture, mannequin, props).
///
/// Normal locomotion already uses the XR Origin CharacterController, but an
/// XR Device Simulator (and real room-scale tracking) updates the camera and
/// controller poses directly. This guard corrects those poses when they would
/// pass into solid geometry.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
public class XRWallCollisionGuard : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public Camera headCamera;

    [Header("Collision")]
    [Min(0.05f)]
    public float headRadius = 0.18f;

    [Min(0.02f)]
    public float controllerRadius = 0.06f;

    [Min(0f)]
    public float clearance = 0.02f;

    [Tooltip("Walkable surfaces whose names contain these words are ignored so the player is not blocked by the floor.")]
    public string[] walkableNameParts = { "Floor", "Ground", "Outside" };

    private Vector3 previousHeadPosition;
    private readonly List<TrackedPart> trackedParts = new List<TrackedPart>();
    private bool initialized;

    private sealed class TrackedPart
    {
        public Transform transform;
        public float radius;
        public Vector3 previousPosition;
        public bool correctsOrigin;
    }

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
        trackedParts.Clear();

        trackedParts.Add(new TrackedPart
        {
            transform = headCamera.transform,
            radius = headRadius,
            previousPosition = headCamera.transform.position,
            correctsOrigin = true
        });

        ActionBasedController[] controllers =
            GetComponentsInChildren<ActionBasedController>(true);

        foreach (ActionBasedController controller in controllers)
        {
            if (controller == null)
                continue;

            trackedParts.Add(new TrackedPart
            {
                transform = controller.transform,
                radius = controllerRadius,
                previousPosition = controller.transform.position,
                correctsOrigin = false
            });
        }

        previousHeadPosition = headCamera.transform.position;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        for (int i = 0; i < trackedParts.Count; i++)
            ResolveTrackedPart(trackedParts[i]);

        previousHeadPosition = headCamera.transform.position;
    }

    private void ResolveTrackedPart(TrackedPart part)
    {
        if (part == null || part.transform == null)
            return;

        Vector3 currentPosition = part.transform.position;
        Vector3 movement = currentPosition - part.previousPosition;

        // Controllers need full 3D blocking so they cannot pass through tabletops.
        // The head still uses horizontal-only correction so looking up/down stays free.
        Vector3 castMovement = part.correctsOrigin
            ? Vector3.ProjectOnPlane(movement, Vector3.up)
            : movement;

        float distance = castMovement.magnitude;
        if (distance <= 0.0001f)
        {
            // Still push out if the part is already overlapping solids.
            if (TryResolveOverlap(part, currentPosition, out Vector3 pushedOut))
            {
                ApplyPartCorrection(part, currentPosition, pushedOut);
                part.previousPosition = part.transform.position;
            }
            else
            {
                part.previousPosition = currentPosition;
            }

            return;
        }

        Vector3 direction = castMovement / distance;
        RaycastHit[] hits = Physics.SphereCastAll(
            part.previousPosition,
            Mathf.Max(0.02f, part.radius),
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
            if (TryResolveOverlap(part, currentPosition, out Vector3 pushedOut))
            {
                ApplyPartCorrection(part, currentPosition, pushedOut);
                part.previousPosition = part.transform.position;
            }
            else
            {
                part.previousPosition = currentPosition;
            }

            return;
        }

        float allowedDistance = Mathf.Max(
            0f,
            nearestHit.distance - Mathf.Max(0f, clearance));

        Vector3 allowedPosition =
            part.previousPosition + direction * allowedDistance;

        if (part.correctsOrigin)
            allowedPosition.y = currentPosition.y;

        ApplyPartCorrection(part, currentPosition, allowedPosition);
        part.previousPosition = part.transform.position;
    }

    private void ApplyPartCorrection(
        TrackedPart part,
        Vector3 currentPosition,
        Vector3 allowedPosition)
    {
        Vector3 correction = allowedPosition - currentPosition;

        if (part.correctsOrigin)
        {
            correction.y = 0f;
            xrOrigin.transform.position += correction;

            // Controllers move with the origin, so keep their previous points
            // consistent after an origin correction.
            for (int i = 0; i < trackedParts.Count; i++)
            {
                TrackedPart other = trackedParts[i];
                if (other == null || other == part || other.correctsOrigin)
                    continue;

                other.previousPosition += correction;
            }
        }
        else
        {
            part.transform.position = allowedPosition;
        }
    }

    private bool TryResolveOverlap(
        TrackedPart part,
        Vector3 currentPosition,
        out Vector3 resolvedPosition)
    {
        resolvedPosition = currentPosition;

        Collider[] overlaps = Physics.OverlapSphere(
            currentPosition,
            Mathf.Max(0.02f, part.radius),
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        Vector3 push = Vector3.zero;
        bool foundOverlap = false;

        foreach (Collider overlap in overlaps)
        {
            if (!IsBlockingCollider(overlap))
                continue;

            Vector3 closest = overlap.ClosestPoint(currentPosition);
            Vector3 away = currentPosition - closest;

            if (away.sqrMagnitude < 0.000001f)
                away = (currentPosition - overlap.bounds.center).normalized;
            else
                away.Normalize();

            float penetration =
                Mathf.Max(0.02f, part.radius) + Mathf.Max(0f, clearance) -
                Vector3.Distance(currentPosition, closest);

            if (penetration <= 0f)
                continue;

            if (part.correctsOrigin)
                away = Vector3.ProjectOnPlane(away, Vector3.up).normalized;

            if (away.sqrMagnitude < 0.000001f)
                continue;

            push += away * penetration;
            foundOverlap = true;
        }

        if (!foundOverlap)
            return false;

        resolvedPosition = currentPosition + push;
        return true;
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
