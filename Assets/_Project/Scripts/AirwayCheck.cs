using UnityEngine;
using UnityEngine.Events;

public class AirwayCheck : MonoBehaviour
{
    [Header("Contact zones")]
    public Collider foreheadZone;
    public Collider chinZone;

    public Transform leftPalmContact;
    public Transform rightPalmContact;

    public float contactTolerance = 0.015f;
    public float placementHoldDuration = 1f;

    [Header("Instruction audio")]
    public AudioClip foreheadInstructionClip;
    public AudioClip chinInstructionClip;
    public AudioClip keepForeheadInstructionClip;

    [Header("Feedback")]
    [Min(0f)]
    public float resultDisplayDuration = 2f;

    [Header("Completion event")]
    public UnityEvent onAirwayCompleted = new UnityEvent();

    private bool started;
    private bool completed;
    private bool foreheadEstablished;
    private bool foreheadReminderActive;
    private bool feedbackFinished;

    private float holdTimer;
    private float finishTime;
    private string message;
    private AudioSource instructionAudio;
    private const string ForeheadInstruction =
        "Move the left controller to the person's forehead and keep it there.";
    private const string ChinInstruction =
        "Then move the right controller beneath the chin to complete the simulated airway-opening position.";
    private const string KeepForeheadInstruction =
        "Keep the left hand on the forehead. Don't move it.";

    private void Awake()
    {
        instructionAudio = gameObject.AddComponent<AudioSource>();
        instructionAudio.playOnAwake = false;
        instructionAudio.loop = false;
        instructionAudio.spatialBlend = 0f;
    }

    private void Start()
    {
        if (foreheadZone == null ||
            chinZone == null ||
            leftPalmContact == null ||
            rightPalmContact == null)
        {
            Debug.LogError(
                "AirwayCheck: assign both zones and " +
                "both palm contact points.",
                this);

            enabled = false;
        }
    }

    public void BeginAirwayStep()
    {
        GameFlowManager gameFlowManager = FindObjectOfType<GameFlowManager>();
        if (gameFlowManager == null || !gameFlowManager.TrainingStarted)
            return;

        if (started || !isActiveAndEnabled)
            return;

        started = true;

        message = ForeheadInstruction;
        PlayInstruction(foreheadInstructionClip);
    }

    private void Update()
    {
        if (!started)
            return;

        // Show the result before starting the breathing step.
        if (completed)
        {
            if (!feedbackFinished && Time.time >= finishTime)
            {
                feedbackFinished = true;
                onAirwayCompleted.Invoke();
            }

            return;
        }

        bool leftOnForehead = CPRInteraction.Touching(
            leftPalmContact,
            foreheadZone,
            contactTolerance);

        bool rightOnChin = CPRInteraction.Touching(
            rightPalmContact,
            chinZone,
            contactTolerance);

        if (!leftOnForehead)
        {
            // Play only on loss of established contact, not every frame away.
            if (foreheadEstablished)
            {
                foreheadReminderActive = !rightOnChin;
                PlayInstruction(foreheadReminderActive
                    ? keepForeheadInstructionClip
                    : foreheadInstructionClip);
            }
            foreheadEstablished = false;
            holdTimer = 0f;

            message = foreheadReminderActive
                ? KeepForeheadInstruction
                : ForeheadInstruction;

            return;
        }

        if (!foreheadEstablished)
        {
            foreheadEstablished = true;
            foreheadReminderActive = false;
            PlayInstruction(rightOnChin ? null : chinInstructionClip);
            holdTimer = 0f;

            message = ChinInstruction;

            return;
        }

        if (!rightOnChin)
        {
            holdTimer = 0f;

            message = ChinInstruction;

            return;
        }

        instructionAudio.Stop();
        holdTimer += Time.deltaTime;

        float requiredDuration =
            Mathf.Max(0.1f, placementHoldDuration);

        float remaining =
            Mathf.Max(0f, requiredDuration - holdTimer);

        message =
            "Both hands in position. Keep them steady for " +
            remaining.ToString("F1") + " seconds.";

        if (holdTimer >= requiredDuration)
        {
            completed = true;

            message =
                "Head tilted and chin lifted to open the airway.\n" +
                "Simulated action completed. Next: check breathing.";

            finishTime =
                Time.time + Mathf.Max(0f, resultDisplayDuration);

            Debug.Log(
                "Simulated head tilt and chin lift completed.",
                this);
        }
    }

    private void PlayInstruction(AudioClip clip)
    {
        instructionAudio.Stop();
        instructionAudio.clip = clip;
        if (clip != null)
            instructionAudio.Play();
    }

    private void OnDisable()
    {
        if (instructionAudio != null)
            instructionAudio.Stop();
    }

    private void OnGUI()
    {
        if (!enabled || !started || feedbackFinished)
            return;

        CPRInteraction.ShowInstruction(
            "3. OPEN AIRWAY",
            message);
    }
}
