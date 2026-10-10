using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Prevents tracked head and controller movement from clipping through solid
/// room colliders (walls, furniture, cupboard/shelf, mannequin, props).
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
    public float controllerRadius = 0.08f;

    [Min(0f)]
    public float clearance = 0.03f;

    [Tooltip("Walkable surfaces whose names contain these words are ignored so the player is not blocked by the floor.")]
    public string[] walkableNameParts = { "Floor", "Ground", "Outside" };

    private readonly List<TrackedPart> trackedParts = new List<TrackedPart>();
    private SphereCollider probeCollider;
    private Transform probeTransform;
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
            return;
        }

        CreateProbeCollider();
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

        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        for (int i = 0; i < trackedParts.Count; i++)
            ResolveTrackedPart(trackedParts[i]);
    }

    private void CreateProbeCollider()
    {
        GameObject probe = new GameObject("CollisionProbe");
        probe.hideFlags = HideFlags.HideAndDontSave;
        probeTransform = probe.transform;
        probeTransform.SetParent(transform, false);

        probeCollider = probe.AddComponent<SphereCollider>();
        probeCollider.isTrigger = true;
        probeCollider.radius = 1f;

        Rigidbody rigidbody = probe.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        rigidbody.detectCollisions = false;
    }

    private void ResolveTrackedPart(TrackedPart part)
    {
        if (part == null || part.transform == null)
            return;

        Vector3 currentPosition = part.transform.position;
        Vector3 movement = currentPosition - part.previousPosition;

        // Controllers need full 3D blocking so they cannot pass through shelves.
        // The head still uses horizontal-only correction so looking up/down stays free.
        Vector3 castMovement = part.correctsOrigin
            ? Vector3.ProjectOnPlane(movement, Vector3.up)
            : movement;

        float distance = castMovement.magnitude;
        Vector3 allowedPosition = currentPosition;
        bool corrected = false;

        if (distance > 0.0001f)
        {
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

            if (foundBlocker)
            {
                float allowedDistance = Mathf.Max(
                    0f,
                    nearestHit.distance - Mathf.Max(0f, clearance));

                allowedPosition =
                    part.previousPosition + direction * allowedDistance;

                if (part.correctsOrigin)
                    allowedPosition.y = currentPosition.y;

                corrected = true;
            }
        }

        // Always push out if already inside a solid (common with cupboard/shelf boxes).
        Vector3 candidate = corrected ? allowedPosition : currentPosition;
        if (TryResolveOverlap(part, candidate, out Vector3 pushedOut))
        {
            allowedPosition = pushedOut;
            corrected = true;
        }

        if (corrected)
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
            if (correction.sqrMagnitude < 0.0000001f)
                return;

            xrOrigin.transform.position += correction;

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

        if (probeCollider == null || probeTransform == null)
            return false;

        float radius = Mathf.Max(0.02f, part.radius);
        probeTransform.position = currentPosition;
        probeCollider.radius = radius;

        Collider[] overlaps = Physics.OverlapSphere(
            currentPosition,
            radius + Mathf.Max(0f, clearance),
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        Vector3 totalPush = Vector3.zero;
        bool foundOverlap = false;

        foreach (Collider overlap in overlaps)
        {
            if (!IsBlockingCollider(overlap))
                continue;

            Vector3 direction;
            float separation;

            bool computed = Physics.ComputePenetration(
                probeCollider,
                currentPosition,
                Quaternion.identity,
                overlap,
                overlap.transform.position,
                overlap.transform.rotation,
                out direction,
                out separation);

            if (!computed || separation <= 0f)
            {
                // Fallback for rare collider types that fail ComputePenetration.
                if (!TryPushOutOfBounds(
                        overlap.bounds,
                        currentPosition,
                        radius,
                        part.correctsOrigin,
                        out direction,
                        out separation))
                {
                    continue;
                }
            }

            if (part.correctsOrigin)
            {
                direction = Vector3.ProjectOnPlane(direction, Vector3.up);
                if (direction.sqrMagnitude < 0.000001f)
                    continue;

                direction.Normalize();
            }

            totalPush += direction * (separation + Mathf.Max(0f, clearance));
            foundOverlap = true;
        }

        if (!foundOverlap)
            return false;

        // Iterate once more from the pushed position for deep cupboard interiors.
        Vector3 afterPush = currentPosition + totalPush;
        probeTransform.position = afterPush;
        probeCollider.radius = radius;

        Collider[] secondPass = Physics.OverlapSphere(
            afterPush,
            radius + Mathf.Max(0f, clearance),
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        foreach (Collider overlap in secondPass)
        {
            if (!IsBlockingCollider(overlap))
                continue;

            Vector3 direction;
            float separation;

            if (!Physics.ComputePenetration(
                    probeCollider,
                    afterPush,
                    Quaternion.identity,
                    overlap,
                    overlap.transform.position,
                    overlap.transform.rotation,
                    out direction,
                    out separation))
            {
                continue;
            }

            if (separation <= 0f)
                continue;

            if (part.correctsOrigin)
            {
                direction = Vector3.ProjectOnPlane(direction, Vector3.up);
                if (direction.sqrMagnitude < 0.000001f)
                    continue;

                direction.Normalize();
            }

            afterPush += direction * (separation + Mathf.Max(0f, clearance));
        }

        resolvedPosition = afterPush;
        return true;
    }

    private static bool TryPushOutOfBounds(
        Bounds bounds,
        Vector3 point,
        float radius,
        bool horizontalOnly,
        out Vector3 direction,
        out float separation)
    {
        direction = Vector3.zero;
        separation = 0f;

        if (!bounds.Contains(point) &&
            (bounds.ClosestPoint(point) - point).sqrMagnitude >
            radius * radius)
        {
            return false;
        }

        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents + Vector3.one * radius;

        float dx = point.x - center.x;
        float dy = point.y - center.y;
        float dz = point.z - center.z;

        float px = extents.x - Mathf.Abs(dx);
        float py = extents.y - Mathf.Abs(dy);
        float pz = extents.z - Mathf.Abs(dz);

        if (horizontalOnly)
            py = float.PositiveInfinity;

        if (px <= py && px <= pz)
        {
            direction = dx >= 0f ? Vector3.right : Vector3.left;
            separation = px;
        }
        else if (py <= px && py <= pz)
        {
            direction = dy >= 0f ? Vector3.up : Vector3.down;
            separation = py;
        }
        else
        {
            direction = dz >= 0f ? Vector3.forward : Vector3.back;
            separation = pz;
        }

        return separation > 0f;
    }

    private bool IsBlockingCollider(Collider collider)
    {
        if (collider == null || collider.isTrigger)
            return false;

        if (collider == probeCollider)
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

    private void OnDestroy()
    {
        if (probeTransform != null)
            Destroy(probeTransform.gameObject);
    }
}
