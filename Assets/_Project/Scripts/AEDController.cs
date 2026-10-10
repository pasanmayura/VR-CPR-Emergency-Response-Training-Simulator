using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class AEDController : MonoBehaviour
{
    [Header("Instruction audio")]
    public AudioClip retrieveAEDInstructionClip;
    public AudioClip carryAEDInstructionClip;
    public AudioClip pad1InstructionClip;
    public AudioClip pad2InstructionClip;
    public AudioClip pad1AttachedInstructionClip;
    public AudioClip pad2AttachedInstructionClip;
    public AudioClip analysingInstructionClip;
    public AudioClip shockAdvisedInstructionClip;
    private bool analysisInstructionPlayed;
    private bool shockInstructionPlayed;
    private int narratedPlacementState = -1;
    private bool carryInstructionPlayed;
    private AudioSource instructionAudio;
    private Rigidbody aedBody;
    private XRGrabInteractable aedGrab;
    private bool initialKinematic;
    private bool initialGravity;
    private bool waitingForFirstRelease;

    private void Awake()
    {
        instructionAudio = gameObject.AddComponent<AudioSource>();
        instructionAudio.playOnAwake = false;
        instructionAudio.loop = false;
        instructionAudio.spatialBlend = 0f;

        aedBody = GetComponent<Rigidbody>();
        aedGrab = GetComponent<XRGrabInteractable>();
        if (aedGrab != null)
        {
            aedGrab.selectEntered.AddListener(OnAEDGrabbed);
            aedGrab.selectExited.AddListener(OnAEDReleased);
        }
        if (aedBody != null && aedGrab != null)
        {
            initialKinematic = aedBody.isKinematic;
            initialGravity = aedBody.useGravity;
            aedBody.isKinematic = true;
            aedBody.useGravity = false;
            waitingForFirstRelease = true;
            aedGrab.selectExited.AddListener(RestorePhysicsAfterFirstGrab);
        }
    }

    private void OnAEDGrabbed(SelectEnterEventArgs args)
    {
        if (!isActiveAndEnabled || !stageStarted || PoweredOn || carryInstructionPlayed)
            return;

        carryInstructionPlayed = true;
        ShowFeedback(
            "7. BRING THE AED",
            "Release Shift and then G.\n" +
            "Now bring it beside the person. " +
            "Then press Shift to put the AED down.");
        instructionAudio.Stop();
        if (carryAEDInstructionClip != null)
        {
            instructionAudio.clip = carryAEDInstructionClip;
            instructionAudio.Play();
        }
    }

    private void RestorePhysicsAfterFirstGrab(SelectExitEventArgs args)
    {
        if (!waitingForFirstRelease || aedBody == null)
            return;

        waitingForFirstRelease = false;
        aedBody.isKinematic = initialKinematic;
        aedBody.useGravity = initialGravity;
    }

    private void OnAEDReleased(SelectExitEventArgs args)
    {
        // A completed grab-and-release advances directly to attaching pads.
        // Ignore canceled selections (for example, disabling the interactor).
        if (args.isCanceled || !isActiveAndEnabled || !stageStarted ||
            !carryInstructionPlayed || PoweredOn || aedGrab.isSelected)
            return;

        PowerOn();
    }

    private void OnDestroy()
    {
        if (aedGrab != null)
        {
            aedGrab.selectEntered.RemoveListener(OnAEDGrabbed);
            aedGrab.selectExited.RemoveListener(OnAEDReleased);
            aedGrab.selectExited.RemoveListener(RestorePhysicsAfterFirstGrab);
        }
    }

    private void OnDisable()
    {
        if (instructionAudio != null)
            instructionAudio.Stop();
    }

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
        "Attach the electrode pads. Move a controller close to Pad 1 and " +
        "press G + Shift to grab it. Place it on the person's upper-right chest, " +
        "below the collarbone, in the highlighted area, then press Shift.";

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

        if (stageStarted || !isActiveAndEnabled)
            return;

        stageStarted = true;

        ShowFeedback(
            "7. USE THE AED",
            "Use the AED on the other table. " +
            "Move either controller close to it and press G + Shift to grab it.");

        if (retrieveAEDInstructionClip != null)
        {
            instructionAudio.clip = retrieveAEDInstructionClip;
            instructionAudio.Play();
        }

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
        instructionAudio.Stop();

        ShowFeedback(
            "7. AED POWERED ON",
            "AED switched on. " + PlacementInstructions);

        if (pad1InstructionClip != null)
        {
            instructionAudio.clip = pad1InstructionClip;
            instructionAudio.Play();
        }

        Debug.Log("AED powered on — simulation.", this);

        onPoweredOn.Invoke();
    }

    private void Update()
    {
        if (!PoweredOn || ShockDelivered)
            return;

        bool shockPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            shockPressed = Keyboard.current.enterKey.wasPressedThisFrame ||
                           Keyboard.current.numpadEnterKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        shockPressed = Input.GetKeyDown(KeyCode.Return) ||
                       Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        if (shockPressed)
        {
            DeliverShock();
            if (ShockDelivered)
                return;
        }

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

        int placementState = (firstPlaced ? 1 : 0) | (secondPlaced ? 2 : 0);
        if (placementState != narratedPlacementState)
        {
            // Leave the initial Pad 1 instruction playing while neither is attached.
            if (placementState != 0 || narratedPlacementState > 0)
            {
                instructionAudio.Stop();
                AudioClip clip = placementState == 1
                    ? pad1AttachedInstructionClip
                    : placementState == 2 ? pad2AttachedInstructionClip : null;
                if (clip != null)
                {
                    instructionAudio.clip = clip;
                    instructionAudio.Play();
                }
            }
            narratedPlacementState = placementState;
        }

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
            if (!handsClear)
            {
                instructionAudio.Stop();
                shockInstructionPlayed = false;
            }
            else if (!shockInstructionPlayed)
            {
                PlayStageInstruction(shockAdvisedInstructionClip);
                shockInstructionPlayed = true;
            }
            ShowFeedback(
                handsClear
                    ? "8. SHOCK ADVISED — SIMULATION"
                    : "8. STAND CLEAR — SHOCK BLOCKED",
                handsClear
                    ? "Keep everyone clear. Do not touch the person. " +
                      "Press Enter to deliver the simulated shock."
                    : "Move both hands away from the person. " +
                      "Keep everyone clear before pressing " +
                      "Enter to deliver the simulated shock.");

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

        if (!analysisInstructionPlayed)
        {
            PlayStageInstruction(analysingInstructionClip);
            analysisInstructionPlayed = true;
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
        PlayStageInstruction(shockAdvisedInstructionClip);
        shockInstructionPlayed = true;

        ShowFeedback(
            "8. SHOCK ADVISED — SIMULATION",
            "Keep everyone clear. Do not touch the person. " +
            "Press Enter to deliver the simulated shock.");

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
                "Enter to deliver the simulated shock.");

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
        instructionAudio.Stop();
        ShockAdvised = false;

        ShowFeedback(
            "9. SHOCK DELIVERED — SIMULATION",
            "One simulated shock has been delivered.");

        Debug.Log("Simulated AED shock delivered.", this);

        onShockDelivered.Invoke();
    }

    private void ResetAnalysis()
    {
        if (instructionAudio.clip == analysingInstructionClip ||
            instructionAudio.clip == shockAdvisedInstructionClip)
            instructionAudio.Stop();
        analysisInstructionPlayed = false;
        shockInstructionPlayed = false;
        clearTimer = 0f;
        analysisTimer = 0f;
        ShockAdvised = false;
    }

    private void PlayStageInstruction(AudioClip clip)
    {
        instructionAudio.Stop();
        instructionAudio.clip = clip;
        if (clip != null)
            instructionAudio.Play();
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
                "Repeat with Pad 2, Place Pad 2 on the person's upper-right chest, " +
                "highlighted area, below the collarbone.");
        }
        else if (secondPlaced)
        {
            ShowFeedback(
                "7. ELECTRODE PADS — 1/2",
                "Pad 2 is correctly attached. " +
                "Repeat with Pad 1, placing it on the person's left side " +
                "highlighted area, just below the armpit.");
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
