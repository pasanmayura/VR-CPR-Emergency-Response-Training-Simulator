using UnityEngine;
using UnityEngine.Events;

public class CPRHandPlacement : MonoBehaviour
{
    [Header("Interaction zones")]
    public Collider chestZone;
    public Collider upperHandZone;

    [Header("Palm contact points")]
    public Transform leftPalmContact;
    public Transform rightPalmContact;

    [Header("Placement settings")]
    public float contactTolerance = 0.015f;
    public float placementHoldDuration = 1f;

    public UnityEvent onHandsPositioned = new UnityEvent();

    public bool Completed { get; private set; }

    private bool started;
    private bool leftEstablished;
    private float holdTimer;
    private string message;

    private void Start()
    {
        if (chestZone == null ||
            upperHandZone == null ||
            leftPalmContact == null ||
            rightPalmContact == null)
        {
            Debug.LogError(
                "CPRHandPlacement: assign both zones " +
                "and both palm contact points.",
                this);

            enabled = false;
            return;
        }

        if (chestZone == upperHandZone)
        {
            Debug.LogError(
                "CPRHandPlacement: use two separate colliders.",
                this);

            enabled = false;
            return;
        }

        chestZone.enabled = false;
        upperHandZone.enabled = false;
    }

    public void BeginHandPlacement()
    {
        if (started || Completed || !enabled)
            return;

        started = true;
        leftEstablished = false;
        holdTimer = 0f;

        chestZone.enabled = true;
        upperHandZone.enabled = false;

        message = CPRInput.Text(
            "Press 1 to select LEFT. SHIFT + left-click " +
            "the chest zone on the lower half of the breastbone.",
            "Place your LEFT controller on the chest zone, " +
            "on the lower half of the breastbone.");
    }

    private void Update()
    {
        if (!started || Completed)
            return;

        bool leftOnChest = CPRInteraction.Touching(
            leftPalmContact,
            chestZone,
            contactTolerance);

        if (!leftOnChest)
        {
            leftEstablished = false;
            holdTimer = 0f;
            upperHandZone.enabled = false;

            message = CPRInput.Text(
                "Press 1. SHIFT + left-click the chest zone. " +
                "Keep the LEFT hand there.",
                "Place your LEFT controller on the chest zone. " +
                "Keep the LEFT hand there.");
            return;
        }

        if (!leftEstablished)
        {
            leftEstablished = true;
            holdTimer = 0f;
            upperHandZone.enabled = true;

            message = CPRInput.Text(
                "LEFT hand in position. Press 2. SHIFT + left-click " +
                "directly above it to position RIGHT.",
                "LEFT hand in position. Place your RIGHT controller " +
                "directly on top of it.");
            return;
        }

        bool rightOnTop = CPRInteraction.Touching(
            rightPalmContact,
            upperHandZone,
            contactTolerance);

        if (!rightOnTop)
        {
            holdTimer = 0f;

            message = CPRInput.Text(
                "Keep LEFT on the chest. Press 2, then " +
                "SHIFT + left-click the upper zone above LEFT.",
                "Keep LEFT on the chest and place the RIGHT " +
                "controller on top of it.");
            return;
        }

        holdTimer += Time.deltaTime;

        message = "Both hands positioned. Keep them steady.";

        if (holdTimer >= Mathf.Max(0.1f, placementHoldDuration))
        {
            Completed = true;

            Debug.Log(
                "CPR hand placement completed. " +
                "Ready for the compression stage.",
                this);

            onHandsPositioned.Invoke();
        }
    }

    private void OnGUI()
    {
        if (!started || Completed)
            return;

        CPRInteraction.ShowInstruction(
            "5. POSITION HANDS FOR CPR",
            message);
    }
}