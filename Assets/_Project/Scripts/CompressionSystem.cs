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
public class CompressionRecordedEvent :
    UnityEvent<CompressionRecord>
{
}

public class CompressionSystem : MonoBehaviour
{
    [Header("Hand placement references")]
    public Collider chestZone;
    public Collider upperHandZone;

    public Transform leftPalmContact;
    public Transform rightPalmContact;

#if ENABLE_INPUT_SYSTEM
    [Header("Input (optional: empty = Space key)")]
    public InputActionReference compressAction;
#endif

    [Header("Hand animation")]
    public HandController handController;

    [Header("Practice settings")]
    public int targetCompressions = 30;
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
                "and both palm contact points.",
                this);

            enabled = false;
            return;
        }

        if (handController == null)
        {
            Debug.LogWarning(
                "CompressionSystem: assign HandController " +
                "to display compression movement.",
                this);
        }
    }

    public void BeginCompressionStage()
    {
        if (!configurationValid ||
            !enabled ||
            started ||
            Completed)
            return;

        started = true;
        stageStartTime = Time.time;

        CompressionCount = 0;
        IncorrectPositionAttempts = 0;
        RhythmMeasurements = 0;
        CorrectRhythmCount = 0;

        LatestRate = 0f;
        AverageRate = 0f;

        firstCompressionTime = 0f;
        lastCompressionTime = 0f;

        records.Clear();

        message = CPRInput.Text(
            "Keep both hands positioned. Press and release SPACE " +
            "once per compression. Aim for 100–120/min.",
            "Keep both hands positioned. Pull and release a TRIGGER " +
            "(keyboard: F) once per compression. Aim for 100–120/min.");
    }

    private void Update()
    {
        if (!started || Completed)
            return;

        bool compressionPressed = false;

#if ENABLE_INPUT_SYSTEM
        compressionPressed =
            CPRInput.WasPressed(compressAction, Key.Space);

#elif ENABLE_LEGACY_INPUT_MANAGER
        compressionPressed = Input.GetKeyDown(KeyCode.Space);
#endif

        if (compressionPressed)
            RecordCompression();
    }

    private void RecordCompression()
    {
        float now = Time.time;

        bool leftCorrect = CPRInteraction.Touching(
            leftPalmContact,
            chestZone,
            contactTolerance);

        bool rightCorrect = CPRInteraction.Touching(
            rightPalmContact,
            upperHandZone,
            contactTolerance);

        bool correctPosition = leftCorrect && rightCorrect;

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
                "Not counted. Keep LEFT on the chest and RIGHT " +
                "in the upper zone. Let the hands return " +
                "before the next compression.";

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

        // Animate both hands for an accepted compression.
        if (handController != null)
            handController.PlayCompressionVisual();

        if (CompressionCount > 1)
        {
            float elapsed = now - firstCompressionTime;

            if (elapsed > 0f)
            {
                AverageRate =
                    60f * (CompressionCount - 1) / elapsed;
            }
        }

        records.Add(record);

        if (!record.hasRhythmMeasurement)
        {
            message = CPRInput.Text(
                "First compression counted. Keep pressing " +
                "and releasing SPACE at a steady rhythm.",
                "First compression counted. Keep pulling " +
                "and releasing the TRIGGER at a steady rhythm.");
        }
        else if (LatestRate < minimumRate)
        {
            message = "Too slow. Increase your rhythm.";
        }
        else if (LatestRate > maximumRate)
        {
            message = "Too fast. Slow your rhythm.";
        }
        else
        {
            message = "Rhythm within target range. Keep going.";
        }

        bool reachedTarget =
            CompressionCount >= Mathf.Max(1, targetCompressions);

        if (reachedTarget)
        {
            Completed = true;
            started = false;

            message =
                "Compression practice block completed.\n" +
                "Average rhythm: " + AverageRate.ToString("F0") + "/min.";

            CPRInteraction.ShowInstruction(
                "6. COMPRESSIONS — " +
                CompressionCount + "/" +
                Mathf.Max(1, targetCompressions),
                message);
        }

        onCompressionRecorded.Invoke(record);

        if (reachedTarget)
        {
            Debug.Log(
                "Compression practice block completed. " +
                "Count: " + CompressionCount +
                ", average rhythm: " +
                AverageRate.ToString("F0") + "/min" +
                ", rhythm intervals in range: " +
                CorrectRhythmCount + "/" + RhythmMeasurements +
                ", incorrect-position attempts: " +
                IncorrectPositionAttempts,
                this);

            onCompressionStageCompleted.Invoke();
        }
    }

    private void OnGUI()
    {
        if (!started || Completed)
            return;

        string rhythmText = RhythmMeasurements > 0
            ? LatestRate.ToString("F0") + "/min"
            : "Waiting for second compression";

        CPRInteraction.ShowInstruction(
            "6. COMPRESSIONS — " +
            CompressionCount + "/" +
            Mathf.Max(1, targetCompressions),

            CPRInput.Text("SPACE", "TRIGGER (F)") +
            " = compression | Rhythm: " +
            rhythmText + "\n" + message);
    }
}