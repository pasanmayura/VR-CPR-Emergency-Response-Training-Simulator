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

    [Header("Instruction audio")]
    public AudioClip leftHandInstructionClip;
    public AudioClip rightHandInstructionClip;

    public UnityEvent onHandsPositioned = new UnityEvent();

    public bool Completed { get; private set; }

    private bool started;
    private bool leftEstablished;
    private float holdTimer;
    private string message;
    private AudioSource instructionAudio;
    private const string LeftHandInstruction =
        "Move the left controller to the centre of the chest, over the lower half of the breastbone.";
    private const string RightHandInstruction =
        "Keep it there, then place the right controller above the left controller.";

    private void Awake()
    {
        instructionAudio = gameObject.AddComponent<AudioSource>();
        instructionAudio.playOnAwake = false;
        instructionAudio.loop = false;
        instructionAudio.spatialBlend = 0f;
    }

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
        GameFlowManager gameFlowManager = FindObjectOfType<GameFlowManager>();
        if (gameFlowManager == null || !gameFlowManager.TrainingStarted)
            return;

        if (started || Completed || !isActiveAndEnabled)
            return;

        started = true;
        leftEstablished = false;
        holdTimer = 0f;

        chestZone.enabled = true;
        upperHandZone.enabled = false;

        message = LeftHandInstruction;
        if (leftHandInstructionClip != null)
        {
            instructionAudio.clip = leftHandInstructionClip;
            instructionAudio.Play();
        }
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
            if (leftEstablished)
                instructionAudio.Stop();
            leftEstablished = false;
            holdTimer = 0f;
            upperHandZone.enabled = false;

            message = LeftHandInstruction;
            return;
        }

        if (!leftEstablished)
        {
            leftEstablished = true;
            instructionAudio.Stop();
            holdTimer = 0f;
            upperHandZone.enabled = true;

            message = RightHandInstruction;
            if (rightHandInstructionClip != null)
            {
                instructionAudio.clip = rightHandInstructionClip;
                instructionAudio.Play();
            }
            return;
        }

        bool rightOnTop = CPRInteraction.Touching(
            rightPalmContact,
            upperHandZone,
            contactTolerance);

        if (!rightOnTop)
        {
            holdTimer = 0f;

            message = RightHandInstruction;
            return;
        }

        instructionAudio.Stop();
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

    private void OnDisable()
    {
        if (instructionAudio != null)
            instructionAudio.Stop();
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
