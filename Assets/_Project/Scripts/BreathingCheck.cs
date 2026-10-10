using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BreathingCheck : MonoBehaviour
{
    [Header("Instruction audio")]
    public AudioClip breathingInstructionClip;
    public AudioClip observationEndedClip;

    [Header("Observation timer audio")]
    public AudioClip timerClip;
    [Range(0f, 1f)]
    public float timerVolume = 0.5f;

    public UnityEvent onBreathingCompleted = new UnityEvent();

    public float ObservationSeconds { get; private set; }

    private bool started;
    private bool observing;
    private bool completed;
    private bool observationEnded;

    private float completionTime;
    private string message;
    private AudioSource instructionAudio;
    private AudioSource timerAudio;

    private void Awake()
    {
        instructionAudio = gameObject.AddComponent<AudioSource>();
        instructionAudio.playOnAwake = false;
        instructionAudio.loop = false;
        instructionAudio.spatialBlend = 0f;
        timerAudio = gameObject.AddComponent<AudioSource>();
        timerAudio.playOnAwake = false;
        timerAudio.loop = true;
        timerAudio.spatialBlend = 0f;
        timerAudio.clip = timerClip;
        timerAudio.volume = timerVolume;
    }

    public void BeginBreathingStep()
    {
        GameFlowManager gameFlowManager = FindObjectOfType<GameFlowManager>();
        if (gameFlowManager == null || !gameFlowManager.TrainingStarted)
            return;

        if (started || completed || !isActiveAndEnabled)
            return;

        started = true;
        ObservationSeconds = 0f;

        message =
            "Press B to begin checking for normal breathing. " +
            "Then press N when no normal breathing is identified.";
        if (breathingInstructionClip != null)
        {
            instructionAudio.clip = breathingInstructionClip;
            instructionAudio.Play();
        }
    }

    private void Update()
    {
        if (!started || completed)
            return;

        bool beginPressed = false;
        bool noBreathingPressed = false;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            beginPressed = keyboard.bKey.wasPressedThisFrame;
            noBreathingPressed = keyboard.nKey.wasPressedThisFrame;
        }

#elif ENABLE_LEGACY_INPUT_MANAGER
        beginPressed = Input.GetKeyDown(KeyCode.B);
        noBreathingPressed = Input.GetKeyDown(KeyCode.N);
#endif

        if (!observing && !observationEnded)
        {
            if (beginPressed)
            {
                observing = true;
                instructionAudio.Stop();
                if (timerClip != null)
                    timerAudio.Play();

                message =
                    "Look for chest movement and listen for normal " +
                    "breathing. Press N when you identify no normal " +
                    "breathing. Check for no longer than 10 seconds.";
            }

            return;
        }

        if (observing)
        {
            ObservationSeconds = Mathf.Min(
                10f,
                ObservationSeconds + Time.deltaTime);

            message =
                "Checking breathing: " +
                ObservationSeconds.ToString("F1") +
                "/10 seconds. No normal breathing is simulated. " +
                "Press N to record your finding.";

            if (ObservationSeconds >= 10f)
            {
                observing = false;
                observationEnded = true;
                timerAudio.Stop();
                if (!noBreathingPressed && observationEndedClip != null)
                {
                    instructionAudio.clip = observationEndedClip;
                    instructionAudio.Play();
                }

                message =
                    "Observation time ended. Press N to record " +
                    "NO NORMAL BREATHING and proceed to CPR.";
            }
        }

        if (noBreathingPressed)
            CompleteBreathingStep();
    }

    private void CompleteBreathingStep()
    {
        timerAudio.Stop();
        instructionAudio.Stop();
        completed = true;
        observing = false;
        completionTime = Time.time;

        message =
            "No normal breathing identified. " +
            "Next: position hands for CPR.";

        Debug.Log(
            "Breathing check completed: no normal breathing. " +
            "Observation time: " +
            ObservationSeconds.ToString("F1") + " seconds.",
            this);
    }

    private void LateUpdate()
    {
        // Show completion feedback before handing over to the next stage.
        if (!completed || !started)
            return;

        if (Time.time - completionTime < 2f)
            return;

        started = false;
        onBreathingCompleted.Invoke();
    }

    private void OnDisable()
    {
        if (timerAudio != null)
            timerAudio.Stop();
        if (instructionAudio != null)
            instructionAudio.Stop();
    }

    private void OnGUI()
    {
        if (!started || completed)
            return;

        CPRInteraction.ShowInstruction(
            "4. CHECK BREATHING",
            message);
    }
}
