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

    [Header("Initial instruction audio")]
    public AudioClip initialInstructionClip;
    public Transform trainingRoom;
    public Vector3 trainingRoomSize = new Vector3(8f, 3f, 8f);

    [Header("Shoulder contact")]
    public Collider shoulderZone;
    public Transform leftPalmContact;
    public Transform rightPalmContact;

    public float contactTolerance = 0.015f;

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
    private GameFlowManager gameFlowManager;
    private AudioSource instructionAudio;
    private bool initialInstructionPlayed;

    private string message =
        "Approach the person and check for a response.";

    private void Start()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        gameFlowManager = FindObjectOfType<GameFlowManager>();
        if (gameFlowManager == null)
        {
            Debug.LogError(
                "ResponsivenessCheck: a GameFlowManager is required before " +
                "the responsiveness step can begin.",
                this);
            enabled = false;
            return;
        }

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
            return;
        }

        if (initialInstructionClip != null)
        {
            instructionAudio = gameObject.AddComponent<AudioSource>();
            instructionAudio.playOnAwake = false;
            instructionAudio.loop = false;
            instructionAudio.spatialBlend = 0f;
            instructionAudio.clip = initialInstructionClip;
        }
    }

    private void Update()
    {
        if (!gameFlowManager.TrainingStarted)
            return;

        if (!assessmentStarted && !initialInstructionPlayed &&
            instructionAudio != null && trainingRoom != null &&
            gameFlowManager.GuideIntroductionFinished)
        {
            Vector3 localPosition = trainingRoom.InverseTransformPoint(playerCamera.position);
            Bounds roomBounds = new Bounds(
                Vector3.up * trainingRoomSize.y * 0.5f, trainingRoomSize);
            if (roomBounds.Contains(localPosition))
            {
                initialInstructionPlayed = true;
                instructionAudio.Play();
            }
        }

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
            if (instructionAudio != null)
                instructionAudio.Stop();
            message = "Press T to ask: Are you okay?";
        }

        bool talkPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            talkPressed = Keyboard.current.tKey.wasPressedThisFrame;
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

    private void OnDisable()
    {
        if (instructionAudio != null)
            instructionAudio.Stop();
    }

    private void OnGUI()
    {
        if (!enabled ||
            feedbackFinished ||
            gameFlowManager == null ||
            !gameFlowManager.TrainingStarted)
            return;

        CPRInteraction.ShowInstruction(
            assessmentStarted
                ? "1. CHECK RESPONSIVENESS"
                : "CHECK RESPONSIVENESS",
            message);
    }
}
