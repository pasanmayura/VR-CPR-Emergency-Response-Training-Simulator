using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class AEDPad : MonoBehaviour
{
    public AEDController aedController;

    private XRGrabInteractable grab;
    private Rigidbody padBody;

    private Transform storageParent;
    private Vector3 storagePosition;
    private Quaternion storageRotation;

    private bool detached;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        padBody = GetComponent<Rigidbody>();

        storageParent = transform.parent;
        storagePosition = transform.localPosition;
        storageRotation = transform.localRotation;

        padBody.useGravity = false;
        padBody.isKinematic = true;

        grab.retainTransformParent = false;
        grab.throwOnDetach = false;
        grab.enabled = false;

        grab.selectEntered.AddListener(OnPadGrabbed);

        if (aedController == null)
        {
            Debug.LogError(
                "AEDPad: assign the parent AED controller.", this);
        }
        // Prevent the stored pad from pushing against the AED
        // or the other pad.
        if (aedController != null)
        {
            Collider[] padColliders =
                GetComponentsInChildren<Collider>();

            Collider[] aedColliders =
                aedController.GetComponentsInChildren<Collider>();

            foreach (Collider padCollider in padColliders)
            {
                foreach (Collider aedCollider in aedColliders)
                {
                    if (padCollider != aedCollider)
                    {
                        Physics.IgnoreCollision(
                            padCollider, aedCollider, true);
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (!grab.enabled &&
            aedController != null &&
            aedController.PoweredOn)
        {
            grab.enabled = true;
        }
    }

    private void LateUpdate()
    {
        // Keep the stored pad moving with the AED.
        if (detached || storageParent == null)
            return;

        padBody.position =
            storageParent.TransformPoint(storagePosition);

        padBody.rotation =
            storageParent.rotation * storageRotation;
    }

    private void OnPadGrabbed(SelectEnterEventArgs args)
    {
        detached = true;

        // Preserve its world position and size when detached.
        transform.SetParent(null, true);

        Debug.Log(name + " removed from the AED.", this);
    }

    private void OnDestroy()
    {
        if (grab != null)
            grab.selectEntered.RemoveListener(OnPadGrabbed);
    }


}