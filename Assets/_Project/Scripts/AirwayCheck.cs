using UnityEngine;
using UnityEngine.Events;

public class AirwayCheck : MonoBehaviour
{
    public Collider foreheadZone;
    public Collider chinZone;

    public Transform leftPalmContact;
    public Transform rightPalmContact;

    public float contactTolerance = 0.015f;
    public float placementHoldDuration = 1f;

    public UnityEvent onAirwayCompleted = new UnityEvent();

    private bool started;
    private bool completed;
    private bool foreheadEstablished;

    private float holdTimer;
    private float completionTime;
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
                "both palm contact points.", this);

            enabled = false;
        }
    }

    public void BeginAirwayStep()
    {
        if (started || !enabled)
            return;

        started = true;
        completed = false;
        foreheadEstablished = false;
        holdTimer = 0f;

        message =
            "Press 1 to select LEFT. Hold SHIFT and click " +
            "the forehead. Keep the left hand there.";
    }

    private void Update()
    {
        if (!started || completed)
            return;

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
                "Press 1 to select LEFT. SHIFT + left-click " +
                "the forehead and wait for contact.";
            return;
        }

        if (!foreheadEstablished)
        {
            foreheadEstablished = true;
            holdTimer = 0f;

            message =
                "LEFT hand in position. Press 2 to select RIGHT. " +
                "SHIFT + left-click beneath the chin.";
            return;
        }

        if (!rightOnChin)
        {
            holdTimer = 0f;

            message =
                "Keep LEFT on the forehead. Press 2, then " +
                "SHIFT + left-click beneath the chin.";
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
            completionTime = Time.time;

            message =
                "Head tilted and chin lifted (simulated). " +
                "Next: check breathing.";

            Debug.Log(
                "Simulated head tilt and chin lift completed.",
                this);

            onAirwayCompleted.Invoke();
        }
    }

    private void OnGUI()
    {
        if (!started || completed)
            return;

        CPRInteraction.ShowInstruction(
            "3. OPEN AIRWAY",
            message);
    }
}