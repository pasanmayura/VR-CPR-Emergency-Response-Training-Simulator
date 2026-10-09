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
    public UnityEvent onShockDelivered = new UnityEvent();

    public bool PoweredOn { get; private set; }
    public bool BothPadsAttached { get; private set; }
    public bool ShockAdvised { get; private set; }
    public bool ShockDelivered { get; private set; }

    private bool stageStarted;
    private float clearTimer;
    private float analysisTimer;

    private string displayedTitle;
    private string displayedMessage;

    private const string PlacementInstructions =
        "Place Pad 1 on the person's upper-right chest, " +
        "below the collarbone. Place Pad 2 on their left side, " +
        "just below the armpit.";

    private bool PlacementReferencesValid()
    {
        return pad1 != null &&
               pad2 != null &&
               pad1 != pad2 &&
               pad1Socket != null &&
               pad2Socket != null &&
               pad1Socket != pad2Socket;
    }

    private bool SafetyReferencesValid()
    {
        return leftHandContact != null &&
               rightHandContact != null &&
               leftHandContact != rightHandContact &&
               mannequinContactZone != null;
    }

    private bool SafetyDetectionAvailable()
    {
        return SafetyReferencesValid() &&
               mannequinContactZone.enabled &&
               mannequinContactZone.gameObject.activeInHierarchy &&
               leftHandContact.gameObject.activeInHierarchy &&
               rightHandContact.gameObject.activeInHierarchy;
    }

    private void Start()
    {
        if (!PlacementReferencesValid())
        {
            Debug.LogError(
                "AEDController: assign both pads and " +
                "their two separate placement sockets.",
                this);
        }

        if (!SafetyReferencesValid())
        {
            Debug.LogError(
                "AEDController: assign both hand contact points " +
                "and the mannequin contact zone.",
                this);
        }
    }

    public void BeginAEDStage()
    {
        GameFlowManager gameFlowManager = FindObjectOfType<GameFlowManager>();
        if (gameFlowManager == null || !gameFlowManager.TrainingStarted)
            return;

        if (stageStarted)
            return;

        stageStarted = true;

        ShowFeedback(
            "7. USE THE AED",
            "Bring the AED beside the person. " +
            "Press its green power button.");

        Debug.Log("AED stage started.", this);
    }

    public void ActivateAED()
    {
        if (!stageStarted || ShockDelivered)
            return;

        if (!PoweredOn)
        {
            PowerOn();
            return;
        }

        if (ShockAdvised)
        {
            DeliverShock();
        }
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
        if (!PoweredOn || ShockDelivered)
            return;

        if (!PlacementReferencesValid())
        {
            BothPadsAttached = false;
            ResetAnalysis();

            ShowFeedback(
                "7. AED SETUP INCOMPLETE",
                "Check both pad references and their placement sockets.");

            return;
        }

        bool firstPlaced = CorrectPadInSocket(pad1Socket, pad1);
        bool secondPlaced = CorrectPadInSocket(pad2Socket, pad2);

        BothPadsAttached = firstPlaced && secondPlaced;

        if (!BothPadsAttached)
        {
            ResetAnalysis();
            ShowPlacementFeedback(firstPlaced, secondPlaced);
            return;
        }

        if (!SafetyDetectionAvailable())
        {
            ResetAnalysis();
            ShowSafetySetupFeedback();
            return;
        }

        bool handsClear = HandsAreClear();

        if (ShockAdvised)
        {
            ShowFeedback(
                handsClear
                    ? "8. SHOCK ADVISED — SIMULATION"
                    : "8. STAND CLEAR — SHOCK BLOCKED",
                handsClear
                    ? "Keep everyone clear. Do not touch the person. " +
                      "Press the red shock button."
                    : "Move both hands away from the person. " +
                      "Keep everyone clear before pressing " +
                      "the red shock button.");

            return;
        }

        if (!handsClear)
        {
            ResetAnalysis();

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
                "Analysing heart rhythm. Do not touch the person.");

            return;
        }

        // Scripted training result, not a real rhythm measurement.
        ShockAdvised = true;

        ShowFeedback(
            "8. SHOCK ADVISED — SIMULATION",
            "Keep everyone clear. Do not touch the person. " +
            "Press the red shock button.");

        Debug.Log(
            "Simulated AED analysis completed: shock advised.",
            this);
    }

    public void DeliverShock()
    {
        if (!stageStarted || !PoweredOn || ShockDelivered)
            return;

        if (!PlacementReferencesValid())
        {
            BothPadsAttached = false;
            ResetAnalysis();

            ShowFeedback(
                "7. AED SETUP INCOMPLETE",
                "Check both pad references and their placement sockets.");

            return;
        }

        // Recheck actual socket selections at the button press.
        bool firstPlaced = CorrectPadInSocket(pad1Socket, pad1);
        bool secondPlaced = CorrectPadInSocket(pad2Socket, pad2);

        BothPadsAttached = firstPlaced && secondPlaced;

        if (!BothPadsAttached)
        {
            ResetAnalysis();
            ShowPlacementFeedback(firstPlaced, secondPlaced);

            Debug.Log(
                "Simulated shock blocked: both pads must be attached.",
                this);

            return;
        }

        if (!SafetyDetectionAvailable())
        {
            ResetAnalysis();
            ShowSafetySetupFeedback();
            return;
        }

        if (!HandsAreClear())
        {
            if (!ShockAdvised)
                ResetAnalysis();

            ShowFeedback(
                "8. STAND CLEAR — SHOCK BLOCKED",
                "Move both hands away from the person. " +
                "Keep everyone clear before pressing " +
                "the red shock button.");

            Debug.Log(
                "Simulated shock blocked: a hand is near the mannequin.",
                this);

            return;
        }

        if (!ShockAdvised)
        {
            Debug.Log(
                "Simulated shock blocked: analysis is not complete.",
                this);

            return;
        }

        // Set this before invoking the event to prevent repeat shocks.
        ShockDelivered = true;
        ShockAdvised = false;

        ShowFeedback(
            "9. SHOCK DELIVERED — SIMULATION",
            "One simulated shock has been delivered.");

        Debug.Log("Simulated AED shock delivered.", this);

        onShockDelivered.Invoke();
    }

    private void ResetAnalysis()
    {
        clearTimer = 0f;
        analysisTimer = 0f;
        ShockAdvised = false;
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

    private void ShowSafetySetupFeedback()
    {
        ShowFeedback(
            "8. AED SETUP INCOMPLETE",
            "Stand-clear detection is unavailable. " +
            "Check the hand references and contact zone.");
    }

    private void ShowPlacementFeedback(
        bool firstPlaced,
        bool secondPlaced)
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
        if (socket == null || expectedPad == null ||
            !socket.isActiveAndEnabled ||
            !expectedPad.isActiveAndEnabled)
        {
            return false;
        }

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
        {
            return;
        }

        displayedTitle = title;
        displayedMessage = message;

        CPRInteraction.ShowInstruction(title, message);
    }
}