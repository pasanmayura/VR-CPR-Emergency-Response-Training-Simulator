using UnityEngine;
using UnityEngine.Events;

public class EmergencyAssistance : MonoBehaviour
{
    public Collider phoneCollider;
    public Transform leftPalmContact;
    public Transform rightPalmContact;

    public float contactTolerance = 0.015f;

    public UnityEvent onAssistanceRequested = new UnityEvent();

    private bool available;
    private bool callScreenOpen;
    private bool completed;

    private void Awake()
    {
        CPRInteraction.CallButtonVisible = false;
    }

    private void Start()
    {
        if (phoneCollider == null ||
            leftPalmContact == null ||
            rightPalmContact == null)
        {
            Debug.LogError(
                "EmergencyAssistance: assign the phone collider " +
                "and both palm contact points.", this);

            enabled = false;
        }
    }

    public void BeginAssistanceStep()
    {
        if (!completed)
            available = true;
    }

    private void Update()
    {
        if (!available || completed || callScreenOpen)
            return;

        bool touching =
            CPRInteraction.Touching(
                leftPalmContact, phoneCollider, contactTolerance) ||
            CPRInteraction.Touching(
                rightPalmContact, phoneCollider, contactTolerance);

        if (touching)
        {
            callScreenOpen = true;
            CPRInteraction.CallButtonVisible = true;
        }
    }

    private void OnGUI()
    {
        if (!available || completed)
            return;

        CPRInteraction.ShowInstruction(
            "2. REQUEST ASSISTANCE",
            callScreenOpen
                ? "Phone reached. Use the green button to make " +
                  "the simulated call and request an AED."
                : "Select a hand with 1 or 2, then click the phone " +
                  "to move that hand onto it.");

        if (callScreenOpen && CPRInteraction.ShowCallButton())
        {
            completed = true;
            CPRInteraction.CallButtonVisible = false;

            Debug.Log("Assistance and AED requested.", this);
            onAssistanceRequested.Invoke();
        }
    }

    private void OnDisable()
    {
        CPRInteraction.CallButtonVisible = false;
    }
}