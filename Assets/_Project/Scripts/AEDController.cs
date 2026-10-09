using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class AEDController : MonoBehaviour
{
    [Header("Electrode pads")]
    public XRGrabInteractable pad1;
    public XRGrabInteractable pad2;

    [Header("Placement sockets")]
    public XRSocketInteractor pad1Socket;
    public XRSocketInteractor pad2Socket;

    [Header("Stand-clear check")]
    public Transform leftHandContact;
    public Transform rightHandContact;
    public Collider mannequinContactZone;

    [Min(0f)]
    public float handContactTolerance = 0.08f;

    [Min(0f)]
    public float clearHoldDuration = 0.5f;

    [Header("Simulation timing")]
    [Min(0.1f)]
    public float analysisDuration = 5f;

    [Header("Events")]
    public UnityEvent onPoweredOn = new UnityEvent();

    public bool PoweredOn { get; private set; }
    public bool BothPadsAttached { get; private set; }
    public bool ShockAdvised { get; private set; }

    private bool stageStarted;
    private bool placementReferencesValid;
    private bool safetyReferencesValid;

    private float clearTimer;
    private float analysisTimer;

    private string displayedTitle;
    private string displayedMessage;

    private const string PlacementInstructions =
        "Place Pad 1 on the person's upper-right chest, " +
        "below the collarbone. Place Pad 2 on their left side, " +
        "just below the armpit.";

    private void Start()
    {
        placementReferencesValid =
            pad1 != null &&
            pad2 != null &&
            pad1 != pad2 &&
            pad1Socket != null &&
            pad2Socket != null &&
            pad1Socket != pad2Socket;

        safetyReferencesValid =
            leftHandContact != null &&
            rightHandContact != null &&
            leftHandContact != rightHandContact &&
            mannequinContactZone != null;

        if (!placementReferencesValid)
        {
            Debug.LogError(
                "AEDController: assign both pads and " +
                "their two separate sockets.", this);
        }

        if (!safetyReferencesValid)
        {
            Debug.LogError(
                "AEDController: assign both tracked hand " +
                "contact points and the mannequin contact zone.",
                this);
        }
    }

    public void BeginAEDStage()
    {
        if (stageStarted)
            return;

        stageStarted = true;

        Debug.Log("AED stage started.", this);

        ShowFeedback(
            "7. USE THE AED",
            "Bring the AED beside the person. " +
            "Press its green power button.");
    }

    public void PowerOn()
    {
        if (!stageStarted || PoweredOn)
            return;

        PoweredOn = true;

        ShowFeedback(
            "7. AED POWERED ON",
            "AED switched on. " + PlacementInstructions);

        Debug.Log("AED powered on — simulation.", this);

        onPoweredOn.Invoke();
    }

    private void Update()
    {
        if (!PoweredOn || !placementReferencesValid)
            return;

        bool firstPlaced = CorrectPadInSocket(
            pad1Socket, pad1);

        bool secondPlaced = CorrectPadInSocket(
            pad2Socket, pad2);

        BothPadsAttached = firstPlaced && secondPlaced;

        if (!BothPadsAttached)
        {
            // Removing a pad cancels the current analysis.
            clearTimer = 0f;
            analysisTimer = 0f;
            ShockAdvised = false;

            ShowPlacementFeedback(firstPlaced, secondPlaced);
            return;
        }

        if (!safetyReferencesValid ||
            !mannequinContactZone.enabled ||
            !mannequinContactZone.gameObject.activeInHierarchy)
        {
            ShowFeedback(
                "8. AED SETUP INCOMPLETE",
                "Stand-clear detection is unavailable. " +
                "Check the hand references and contact zone.");
            return;
        }

        bool handsClear = HandsAreClear();

        if (ShockAdvised)
        {
            ShowFeedback(
                "8. SHOCK ADVISED — SIMULATION",
                handsClear
                    ? "Keep everyone clear. Do not touch the person."
                    : "Move both hands away. Do not touch the person.");

            return;
        }

        if (!handsClear)
        {
            clearTimer = 0f;
            analysisTimer = 0f;

            ShowFeedback(
                "8. STAND CLEAR",
                "Both pads are attached. Move both hands away " +
                "from the person. Analysis requires everyone clear.");

            return;
        }

        clearTimer += Time.deltaTime;

        if (clearTimer < clearHoldDuration)
        {
            ShowFeedback(
                "8. STAND CLEAR",
                "Both pads are attached. Keep both hands clear.");
            return;
        }

        analysisTimer += Time.deltaTime;

        if (analysisTimer < Mathf.Max(0.1f, analysisDuration))
        {
            ShowFeedback(
                "8. ANALYSING — SIMULATION",
                "Analysing heart rhythm. " +
                "Do not touch the person.");

            return;
        }

        // This is a scripted training result,
        // not a measurement of a real heart rhythm.
        ShockAdvised = true;

        ShowFeedback(
            "8. SHOCK ADVISED — SIMULATION",
            "Keep everyone clear. Do not touch the person.");

        Debug.Log(
            "Simulated AED analysis completed: shock advised.",
            this);
    }

    private bool HandsAreClear()
    {
        bool leftTouching = CPRInteraction.Touching(
            leftHandContact,
            mannequinContactZone,
            handContactTolerance);

        bool rightTouching = CPRInteraction.Touching(
            rightHandContact,
            mannequinContactZone,
            handContactTolerance);

        return !leftTouching && !rightTouching;
    }

    private void ShowPlacementFeedback(
        bool firstPlaced, bool secondPlaced)
    {
        if (firstPlaced)
        {
            ShowFeedback(
                "7. ELECTRODE PADS — 1/2",
                "Pad 1 is correctly attached. " +
                "Place Pad 2 on the person's left side, " +
                "just below the armpit.");
        }
        else if (secondPlaced)
        {
            ShowFeedback(
                "7. ELECTRODE PADS — 1/2",
                "Pad 2 is correctly attached. " +
                "Place Pad 1 on the person's upper-right chest, " +
                "below the collarbone.");
        }
        else
        {
            ShowFeedback(
                "7. ELECTRODE PADS — 0/2",
                PlacementInstructions);
        }
    }

    private bool CorrectPadInSocket(
        XRSocketInteractor socket,
        XRGrabInteractable expectedPad)
    {
        if (!socket.isActiveAndEnabled ||
            !expectedPad.isActiveAndEnabled)
            return false;

        foreach (IXRSelectInteractable selectedPad
                 in socket.interactablesSelected)
        {
            if (ReferenceEquals(selectedPad, expectedPad))
                return true;
        }

        return false;
    }

    private void ShowFeedback(string title, string message)
    {
        if (title == displayedTitle &&
            message == displayedMessage)
            return;

        displayedTitle = title;
        displayedMessage = message;

        CPRInteraction.ShowInstruction(title, message);
    }
}