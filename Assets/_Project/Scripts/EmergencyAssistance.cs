using UnityEngine;
using UnityEngine.Events;

public class EmergencyAssistance : MonoBehaviour
{
    [Header("Phone contact")]
    public Collider phoneCollider;
    public Transform leftPalmContact;
    public Transform rightPalmContact;

    public float contactTolerance = 0.015f;

    [Header("Feedback")]
    [Min(0f)]
    public float resultDisplayDuration = 3f;

    [Header("Completion event")]
    public UnityEvent onAssistanceRequested = new UnityEvent();

    private bool available;
    private bool callScreenOpen;
    private bool completed;
    private bool feedbackFinished;

    private float finishTime;

    private void Awake()
    {
        CPRInteraction.CallButtonVisible = false;
    }

    private void Start()
    {
        ResolveMissingReferences();

        if (phoneCollider == null ||
            leftPalmContact == null ||
            rightPalmContact == null)
        {
            Debug.LogError(
                "EmergencyAssistance: assign the phone collider " +
                "and both palm contact points.",
                this);

            enabled = false;
        }
    }

    private void ResolveMissingReferences()
    {
        if (leftPalmContact == null)
        {
            GameObject leftContact = GameObject.Find("LeftPalmContact");
            if (leftContact != null)
                leftPalmContact = leftContact.transform;
        }

        if (rightPalmContact == null)
        {
            GameObject rightContact = GameObject.Find("RightPalmContact");
            if (rightContact != null)
                rightPalmContact = rightContact.transform;
        }

        if (phoneCollider == null)
        {
            Collider[] colliders = FindObjectsOfType<Collider>(true);
            foreach (Collider candidate in colliders)
            {
                if (candidate.gameObject.name == "Phone_Placeholder")
                {
                    phoneCollider = candidate;
                    break;
                }
            }
        }
    }

    public void BeginAssistanceStep()
    {
        if (!completed)
            available = true;
    }

    private void Update()
    {
        if (!available)
            return;

        // Keep the call confirmation visible before the next step.
        if (completed)
        {
            if (!feedbackFinished && Time.time >= finishTime)
            {
                feedbackFinished = true;
                available = false;
                onAssistanceRequested.Invoke();
            }

            return;
        }

        if (callScreenOpen)
            return;

        bool touching =
            CPRInteraction.Touching(
                leftPalmContact,
                phoneCollider,
                contactTolerance) ||
            CPRInteraction.Touching(
                rightPalmContact,
                phoneCollider,
                contactTolerance);

        if (touching)
        {
            callScreenOpen = true;
            CPRInteraction.CallButtonVisible = true;
        }
    }

    private void OnGUI()
    {
        if (!enabled || !available || feedbackFinished)
            return;

        if (completed)
        {
            CPRInteraction.ShowInstruction(
                "2. ASSISTANCE REQUESTED",
                "Dialled 1990. Assistance is on the way.\n" +
                "This is a simulated call.");

            return;
        }

        CPRInteraction.ShowInstruction(
            "2. REQUEST ASSISTANCE",
            callScreenOpen
                ? "Phone reached. Click the green call button " +
                  "to dial 1990 and request emergency assistance."
                : "Call for emergency assistance. Press 1 or 2 " +
                  "to select a hand, then hold Shift and click " +
                  "the phone to tap it.");

        if (callScreenOpen && CPRInteraction.ShowCallButton())
        {
            completed = true;
            callScreenOpen = false;
            CPRInteraction.CallButtonVisible = false;

            finishTime =
                Time.time + Mathf.Max(0f, resultDisplayDuration);

            Debug.Log(
                "Simulated call: dialled 1990. " +
                "Assistance is on the way.",
                this);
        }
    }

    private void OnDisable()
    {
        CPRInteraction.CallButtonVisible = false;
    }
}