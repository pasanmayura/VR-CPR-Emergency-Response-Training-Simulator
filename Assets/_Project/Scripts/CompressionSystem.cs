using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[System.Serializable]
public class CompressionRecord
{
    public float timeSinceStageStarted;

    public Vector3 leftPalmPosition;
    public Vector3 rightPalmPosition;

    public bool correctPosition;
    public bool accepted;

    public bool hasRhythmMeasurement;
    public float intervalSeconds;
    public float ratePerMinute;
    public bool rhythmInRange;
}

[System.Serializable]
public class CompressionRecordedEvent : UnityEvent<CompressionRecord>
{
}

public class CompressionSystem : MonoBehaviour
{
    [Header("Hand placement references")]
    public Collider chestZone;
    public Collider upperHandZone;
    public Transform leftPalmContact;
    public Transform rightPalmContact;

    [Header("Hand animation")]
    public HandController handController;

    [Header("Practice settings")]
    [Min(2)] public int targetCompressions = 30;
    [Min(1)] public int requiredCorrectRhythm = 25;
    public float contactTolerance = 0.015f;

    [Header("Rhythm settings")]
    public float minimumRate = 100f;
    public float maximumRate = 120f;

    [Header("Assessment events")]
    public CompressionRecordedEvent onCompressionRecorded =
        new CompressionRecordedEvent();

    public UnityEvent onCompressionStageCompleted =
        new UnityEvent();

    [Header("Recorded attempts")]
    [SerializeField]
    private List<CompressionRecord> records =
        new List<CompressionRecord>();

    public int CompressionCount { get; private set; }
    public int IncorrectPositionAttempts { get; private set; }
    public int RhythmMeasurements { get; private set; }
    public int CorrectRhythmCount { get; private set; }
    public int AttemptNumber { get; private set; }

    public float LatestRate { get; private set; }
    public float AverageRate { get; private set; }
    public bool Completed { get; private set; }

    public IReadOnlyList<CompressionRecord> Records
    {
        get { return records; }
    }

    private bool started;
    private bool configurationValid;
    private float stageStartTime;
    private float firstCompressionTime;
    private float lastCompressionTime;
    private string message;

    private int Target
    {
        get { return Mathf.Max(2, targetCompressions); }
    }

    private void Awake()
    {
        configurationValid =
            chestZone != null &&
            upperHandZone != null &&
            leftPalmContact != null &&
            rightPalmContact != null &&
            chestZone != upperHandZone;

        if (!configurationValid)
        {
            Debug.LogError(
                "CompressionSystem: assign two separate zones " +
                "and both palm contact points.", this);

            enabled = false;
            return;
        }

        // The first compression has no rhythm measurement.
        if (requiredCorrectRhythm < 1 ||
            requiredCorrectRhythm > Target - 1 ||
            minimumRate <= 0f ||
            maximumRate < minimumRate)
        {
            configurationValid = false;

            Debug.LogError(
                "CompressionSystem: required correct rhythm must " +
                "be between 1 and targetCompressions - 1. " +
                "Check the minimum and maximum rates too.", this);

            enabled = false;
            return;
        }

        if (handController == null)
        {
            Debug.LogWarning(
                "CompressionSystem: assign HandController " +
                "to display compression movement.", this);
        }
    }

    public void BeginCompressionStage()
    {
        GameFlowManager flow = FindObjectOfType<GameFlowManager>();

        if (flow == null || !flow.TrainingStarted)
            return;

        if (!configurationValid || !enabled || started || Completed)
            return;

        started = true;
        stageStartTime = Time.time;
        AttemptNumber = 1;

        records.Clear();
        ResetAttemptCounters();

        message =
            "Both controllers are positioned correctly.\n" +
            "Press and release SPACE once for each beat. " +
            "Get at least " + requiredCorrectRhythm +
            " correctly timed compressions to pass.";
    }

    private void ResetAttemptCounters()
    {
        CompressionCount = 0;
        IncorrectPositionAttempts = 0;
        RhythmMeasurements = 0;
        CorrectRhythmCount = 0;

        LatestRate = 0f;
        AverageRate = 0f;

        firstCompressionTime = 0f;
        lastCompressionTime = 0f;
    }

    private void Update()
    {
        if (!started || Completed)
            return;

        bool compressionPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            compressionPressed =
                Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        compressionPressed = Input.GetKeyDown(KeyCode.Space);
#endif

        if (compressionPressed)
            RecordCompression();
    }

    private void RecordCompression()
    {
        float now = Time.time;

        bool correctPosition =
            CPRInteraction.Touching(
                leftPalmContact, chestZone, contactTolerance) &&
            CPRInteraction.Touching(
                rightPalmContact, upperHandZone, contactTolerance);

        CompressionRecord record = new CompressionRecord
        {
            timeSinceStageStarted = now - stageStartTime,
            leftPalmPosition = leftPalmContact.position,
            rightPalmPosition = rightPalmContact.position,
            correctPosition = correctPosition,
            accepted = correctPosition
        };

        if (!correctPosition)
        {
            IncorrectPositionAttempts++;

            message =
                "Not counted. Keep the left controller at the " +
                "chest position and the right above it. " +
                "Let the compression animation finish.";

            records.Add(record);
            onCompressionRecorded.Invoke(record);
            return;
        }

        if (CompressionCount == 0)
        {
            firstCompressionTime = now;
        }
        else
        {
            float interval = now - lastCompressionTime;

            if (interval > 0f)
            {
                LatestRate = 60f / interval;

                record.hasRhythmMeasurement = true;
                record.intervalSeconds = interval;
                record.ratePerMinute = LatestRate;
                record.rhythmInRange =
                    LatestRate >= minimumRate &&
                    LatestRate <= maximumRate;

                RhythmMeasurements++;

                if (record.rhythmInRange)
                    CorrectRhythmCount++;
            }
        }

        CompressionCount++;
        lastCompressionTime = now;

        if (handController != null)
            handController.PlayCompressionVisual();

        if (CompressionCount > 1)
        {
            float elapsed = now - firstCompressionTime;

            if (elapsed > 0f)
                AverageRate =
                    60f * (CompressionCount - 1) / elapsed;
        }

        records.Add(record);

        if (!record.hasRhythmMeasurement)
            message = "First compression counted. Follow the beat.";
        else if (LatestRate < minimumRate)
            message = "Too slow. Increase your rhythm.";
        else if (LatestRate > maximumRate)
            message = "Too fast. Slow your rhythm.";
        else
            message = "Correct rhythm. Keep going.";

        bool reachedTarget = CompressionCount >= Target;
        bool passed =
            reachedTarget &&
            CorrectRhythmCount >= requiredCorrectRhythm;

        if (passed)
        {
            Completed = true;
            started = false;

            message =
                "Compression practice passed.\n" +
                "Correct rhythm: " + CorrectRhythmCount +
                "/" + RhythmMeasurements +
                ". Average: " + AverageRate.ToString("F0") + "/min.";

            CPRInteraction.ShowInstruction(
                "6. COMPRESSIONS — PASSED", message);
        }

        // Report the final compression before resetting a failed attempt.
        onCompressionRecorded.Invoke(record);

        if (!reachedTarget)
            return;

        Debug.Log(
            "Compression attempt " + AttemptNumber +
            ": " + (passed ? "PASSED" : "FAILED") +
            ". Correct rhythm: " + CorrectRhythmCount +
            "/" + RhythmMeasurements +
            ". Average: " + AverageRate.ToString("F0") + "/min." +
            " Incorrect-position attempts: " +
            IncorrectPositionAttempts, this);

        if (passed)
        {
            onCompressionStageCompleted.Invoke();
            return;
        }

        int previousCorrectCount = CorrectRhythmCount;

        AttemptNumber++;
        ResetAttemptCounters();

        // Keep all previous records for assessment.
        message =
            "Attempt failed: " + previousCorrectCount +
            " correctly timed compressions. You need at least " +
            requiredCorrectRhythm + ".\n" +
            "Start again. Press and release SPACE with each beat.";
    }

    private void OnGUI()
    {
        if (!started || Completed)
            return;

        string rhythmText = RhythmMeasurements > 0
            ? LatestRate.ToString("F0") + "/min"
            : "Waiting for second compression";

        CPRInteraction.ShowInstruction(
            "6. COMPRESSIONS — " + CompressionCount + "/" +
            Target + " | Attempt " + AttemptNumber,

            "SPACE = compression | Rhythm: " + rhythmText +
            "\nCorrect rhythm: " + CorrectRhythmCount + "/" +
            requiredCorrectRhythm + " required\n" + message);
    }
}