using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ResponsivenessCheck : MonoBehaviour
{
    public Collider shoulderZone;
    public Transform leftPalmContact;
    public Transform rightPalmContact;

    public float contactTolerance = 0.015f;

    public UnityEvent onResponsivenessCompleted = new UnityEvent();

    private bool talked;
    private bool completed;
    private bool readyForTap;
    private Transform tappingPalm;

    private string message = "Press T to ask: Are you okay?";

    private void Start()
    {
        if (shoulderZone == null ||
            leftPalmContact == null ||
            rightPalmContact == null)
        {
            Debug.LogError(
                "ResponsivenessCheck: assign the shoulder zone " +
                "and both palm contact points.", this);

            enabled = false;
        }
    }

    private void Update()
    {
        if (completed)
            return;

        bool talkPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            talkPressed = Keyboard.current.tKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        talkPressed = Input.GetKeyDown(KeyCode.T);
#endif

        bool leftTouching = CPRInteraction.Touching(
            leftPalmContact, shoulderZone, contactTolerance);

        bool rightTouching = CPRInteraction.Touching(
            rightPalmContact, shoulderZone, contactTolerance);

        if (talkPressed && !talked)
        {
            talked = true;
            message =
                "No response to voice. Move either hand onto the " +
                "shoulder, then move it away to simulate a tap.";
        }

        if (!talked)
            return;

        // Require hands to start outside the shoulder zone.
        if (!readyForTap)
        {
            if (!leftTouching && !rightTouching)
                readyForTap = true;

            return;
        }

        if (tappingPalm == null)
        {
            if (leftTouching)
                tappingPalm = leftPalmContact;
            else if (rightTouching)
                tappingPalm = rightPalmContact;

            if (tappingPalm != null)
                message = "Shoulder touched. Move that hand away.";

            return;
        }

        if (!shoulderZone.enabled ||
            !shoulderZone.gameObject.activeInHierarchy)
            return;

        if (CPRInteraction.Touching(
                tappingPalm, shoulderZone, contactTolerance))
            return;

        completed = true;
        Debug.Log("Responsiveness check completed.", this);
        onResponsivenessCompleted.Invoke();
    }

    private void OnGUI()
    {
        if (!completed)
            CPRInteraction.ShowInstruction(
                "1. CHECK RESPONSIVENESS", message);
    }
}