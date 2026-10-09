using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ResponsivenessCheck : MonoBehaviour
{
    [Header("Player proximity")]
    public Transform playerCamera;

    [Min(0.1f)]
    public float activationDistance = 2f;

    [Header("Shoulder contact")]
    public Collider shoulderZone;
    public Transform leftPalmContact;
    public Transform rightPalmContact;

    public float contactTolerance = 0.015f;

#if ENABLE_INPUT_SYSTEM
    [Header("Input (optional: empty = T key)")]
    public InputActionReference talkAction;
#endif

    [Header("Feedback")]
    [Min(0f)]
    public float resultDisplayDuration = 2f;

    [Header("Completion event")]
    public UnityEvent onResponsivenessCompleted = new UnityEvent();

    private bool assessmentStarted;
    private bool talked;
    private bool completed;
    private bool readyForTap;
    private bool feedbackFinished;

    private Transform tappingPalm;
    private float finishTime;

    private string message =
        "A person needs emergency assistance. Approach them.";

    private void Start()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (playerCamera == null ||
            shoulderZone == null ||
            leftPalmContact == null ||
            rightPalmContact == null)
        {
            Debug.LogError(
                "ResponsivenessCheck: assign the player camera, " +
                "shoulder zone, and both palm contact points.",
                this);

            enabled = false;
        }
    }

    private void Update()
    {
        // Display the shoulder-tap result before starting the next step.
        if (completed)
        {
            if (!feedbackFinished && Time.time >= finishTime)
            {
                feedbackFinished = true;
                onResponsivenessCompleted.Invoke();
            }

            return;
        }

        // Unlock the assessment when the player approaches the casualty.
        if (!assessmentStarted)
        {
            if (!ShoulderZoneActive())
                return;

            Vector3 nearestPoint =
                shoulderZone.ClosestPoint(playerCamera.position);

            float distance =
                Vector3.Distance(playerCamera.position, nearestPoint);

            if (distance > Mathf.Max(0.1f, activationDistance))
                return;

            assessmentStarted = true;
            message = CPRInput.Text(
                "Press T to ask: Are you okay?",
                "Press B on the RIGHT controller (keyboard: O) " +
                "to ask: Are you okay?");
        }

        bool talkPressed = false;

#if ENABLE_INPUT_SYSTEM
        talkPressed = CPRInput.WasPressed(talkAction, Key.T);
#elif ENABLE_LEGACY_INPUT_MANAGER
        talkPressed = Input.GetKeyDown(KeyCode.T);
#endif

        if (talkPressed && !talked)
        {
            talked = true;

            message =
                "No response to voice. Move either hand onto the " +
                "shoulder, then move it away to simulate a tap.";
        }

        if (!talked || !ShoulderZoneActive())
            return;

        bool leftTouching = CPRInteraction.Touching(
            leftPalmContact,
            shoulderZone,
            contactTolerance);

        bool rightTouching = CPRInteraction.Touching(
            rightPalmContact,
            shoulderZone,
            contactTolerance);

        // Both hands must leave the shoulder before a new tap can begin.
        if (!readyForTap)
        {
            if (!leftTouching && !rightTouching)
                readyForTap = true;

            return;
        }

        // Remember which hand touched the shoulder.
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

        // Complete the tap when the same hand moves away.
        if (CPRInteraction.Touching(
                tappingPalm,
                shoulderZone,
                contactTolerance))
        {
            return;
        }

        completed = true;
        message = "No response detected to the shoulder tap.";

        finishTime =
            Time.time + Mathf.Max(0f, resultDisplayDuration);

        Debug.Log(
            "No response detected to the shoulder tap.",
            this);
    }

    private bool ShoulderZoneActive()
    {
        return shoulderZone != null &&
               shoulderZone.enabled &&
               shoulderZone.gameObject.activeInHierarchy;
    }

    private void OnGUI()
    {
        if (!enabled || feedbackFinished)
            return;

        CPRInteraction.ShowInstruction(
            assessmentStarted
                ? "1. CHECK RESPONSIVENESS"
                : "APPROACH THE CASUALTY",
            message);
    }
}