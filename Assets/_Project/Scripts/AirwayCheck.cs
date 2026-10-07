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

    [Header("Feedback")]
    [Min(0f)]
    public float resultDisplayDuration = 2f;

    [Header("Completion event")]
    public UnityEvent onAirwayCompleted = new UnityEvent();

    private bool started;
    private bool completed;
    private bool foreheadEstablished;
    private bool feedbackFinished;

    private float holdTimer;
    private float finishTime;
    private string message;

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
        if (started || !enabled)
            return;

        started = true;

        message =
            "Press 1 to select the LEFT hand. Hold Shift and " +
            "click the forehead. Keep the left hand there.";
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
            foreheadEstablished = false;
            holdTimer = 0f;

            message =
                "Press 1 to select the LEFT hand. Hold Shift " +
                "and click the forehead. Keep that hand there.";

            return;
        }

        if (!foreheadEstablished)
        {
            foreheadEstablished = true;
            holdTimer = 0f;

            message =
                "Left hand in position. Press 2 to select the " +
                "RIGHT hand, then hold Shift and click beneath the chin.";

            return;
        }

        if (!rightOnChin)
        {
            holdTimer = 0f;

            message =
                "Keep the left hand on the forehead. Press 2, " +
                "then hold Shift and click beneath the chin.";

            return;
        }

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

    private void OnGUI()
    {
        if (!enabled || !started || feedbackFinished)
            return;

        CPRInteraction.ShowInstruction(
            "3. OPEN AIRWAY",
            message);
    }
}