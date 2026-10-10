using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(10000)]
public class HandController : MonoBehaviour
{
    [Header("Hand bones")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("Palm contact points")]
    public Transform leftPalmContact;
    public Transform rightPalmContact;

    [Header("Gameplay camera")]
    public Camera interactionCamera;

    [Header("Camera hand resting points")]
    public Transform leftHandRest;
    public Transform rightHandRest;

    [Header("Movement settings")]
    public float moveSpeed = 1f;
    public float surfaceOffset = 0f;
    public float maxClickDistance = 100f;
    public LayerMask clickableLayers = Physics.DefaultRaycastLayers;

    [Header("Desktop testing")]
    public bool disableHandAnimators = true;

    [Header("Compression visual")]
    public float compressionVisualDistance = 0.02f;
    public float compressionVisualDuration = 0.24f;
    public Vector3 compressionDirection = Vector3.down;

    private enum HandSelection
    {
        None,
        Left,
        Right
    }

    private HandSelection selectedHand = HandSelection.None;

    private bool initialized;
    private bool showControls;

    private bool leftHandResting = true;
    private bool rightHandResting = true;

    private Vector3 leftTarget;
    private Vector3 rightTarget;
    private Vector3 leftPalmPosition;
    private Vector3 rightPalmPosition;

    private Quaternion leftOriginalRotation;
    private Quaternion rightOriginalRotation;
    private Quaternion leftRestRotationOffset;
    private Quaternion rightRestRotationOffset;

    private bool compressionVisualPlaying;
    private float compressionVisualStartTime;

    private string feedback =
        "Select a hand: press 1 for LEFT or 2 for RIGHT.";

    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle statusStyle;
    private GUIStyle buttonStyle;
    private GUIStyle panelStyle;

    private void Start()
    {
        if (leftHand == null ||
            rightHand == null ||
            leftPalmContact == null ||
            rightPalmContact == null ||
            interactionCamera == null ||
            leftHandRest == null ||
            rightHandRest == null)
        {
            StopWithError(
                "Assign both hand bones, both palm contact points, " +
                "Main Camera, and both camera resting points.");
            return;
        }

        if (leftHand == rightHand ||
            leftHand.IsChildOf(rightHand) ||
            rightHand.IsChildOf(leftHand))
        {
            StopWithError(
                "Assign separate J_Left and J_Right bones. " +
                "Neither hand should be inside the other.");
            return;
        }

        if (leftPalmContact == leftHand ||
            !leftPalmContact.IsChildOf(leftHand))
        {
            StopWithError(
                "LeftPalmContact must be beneath J_Left, " +
                "normally inside J_Left_Hand.");
            return;
        }

        if (rightPalmContact == rightHand ||
            !rightPalmContact.IsChildOf(rightHand))
        {
            StopWithError(
                "RightPalmContact must be beneath J_Right, " +
                "normally inside J_Right_Hand.");
            return;
        }

        if (!leftHand.IsChildOf(transform) ||
            !rightHand.IsChildOf(transform))
        {
            StopWithError(
                "Attach HandController to the hand prefab root " +
                "containing both hands.");
            return;
        }

        if (leftHandRest == rightHandRest ||
            !leftHandRest.IsChildOf(interactionCamera.transform) ||
            !rightHandRest.IsChildOf(interactionCamera.transform))
        {
            StopWithError(
                "Create two separate resting points as children " +
                "of the assigned Main Camera.");
            return;
        }

        if (leftHandRest.IsChildOf(leftHand) ||
            leftHandRest.IsChildOf(rightHand) ||
            rightHandRest.IsChildOf(leftHand) ||
            rightHandRest.IsChildOf(rightHand))
        {
            StopWithError(
                "Resting points must be independent of the hand bones.");
            return;
        }

        if (disableHandAnimators)
        {
            foreach (Animator animator in
                     GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }
        }

        // Keep the existing hand orientation for mannequin interactions.
        leftOriginalRotation = leftHand.rotation;
        rightOriginalRotation = rightHand.rotation;

        // Rest-point local rotations act as adjustments to the
        // model's original orientation relative to the starting camera.
        leftRestRotationOffset =
            Quaternion.Inverse(interactionCamera.transform.rotation) *
            leftOriginalRotation;

        rightRestRotationOffset =
            Quaternion.Inverse(interactionCamera.transform.rotation) *
            rightOriginalRotation;

        leftPalmPosition = leftHandRest.position;
        rightPalmPosition = rightHandRest.position;

        leftTarget = leftPalmPosition;
        rightTarget = rightPalmPosition;

        leftHandResting = true;
        rightHandResting = true;

        selectedHand = HandSelection.None;
        showControls = false;
        initialized = true;
    }

    private void StopWithError(string message)
    {
        Debug.LogError("HandController: " + message, this);
        enabled = false;
    }

    public void PlayCompressionVisual()
    {
        if (!initialized || !isActiveAndEnabled)
            return;

        compressionVisualStartTime = Time.time;
        compressionVisualPlaying = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        bool clicked = false;
        bool shiftHeld = false;
        bool resetPressed = false;

        Vector2 pointerPosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame)
                SelectHand(HandSelection.Left);

            if (keyboard.digit2Key.wasPressedThisFrame)
                SelectHand(HandSelection.Right);

            if (keyboard.escapeKey.wasPressedThisFrame)
                SelectHand(HandSelection.None);

            resetPressed = keyboard.rKey.wasPressedThisFrame;

            shiftHeld =
                keyboard.leftShiftKey.isPressed ||
                keyboard.rightShiftKey.isPressed;
        }

        if (mouse != null)
        {
            clicked = mouse.leftButton.wasPressedThisFrame;
            pointerPosition = mouse.position.ReadValue();
        }

#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SelectHand(HandSelection.Left);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SelectHand(HandSelection.Right);

        if (Input.GetKeyDown(KeyCode.Escape))
            SelectHand(HandSelection.None);

        resetPressed = Input.GetKeyDown(KeyCode.R);

        shiftHeld =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        clicked = Input.GetMouseButtonDown(0);
        pointerPosition = Input.mousePosition;
#endif

        if (resetPressed)
        {
            ResetBothHands();
            return;
        }

        if (!clicked)
            return;

        Vector2 guiPosition = new Vector2(
            pointerPosition.x,
            Screen.height - pointerPosition.y
        );

        // Prevent moving hands when clicking visible interface areas.
        if (InstructionPanelRect.Contains(guiPosition) ||
            ControlsButtonRect.Contains(guiPosition) ||
            HandStatusRect.Contains(guiPosition) ||
            (showControls && ControlsPanelRect.Contains(guiPosition)))
        {
            return;
        }

        // Normal clicks remain available for UI buttons.
        if (!shiftHeld)
            return;

        if (selectedHand == HandSelection.None)
        {
            feedback = "Select a hand first: 1 = LEFT, 2 = RIGHT.";
            return;
        }

        SetDestination(pointerPosition);
    }

    private void SelectHand(HandSelection selection)
    {
        selectedHand = selection;

        if (selection == HandSelection.None)
        {
            // Stop placed hands without returning them.
            // Resting hands continue following the camera.
            if (!leftHandResting)
                leftTarget = leftPalmPosition;

            if (!rightHandResting)
                rightTarget = rightPalmPosition;

            feedback = "No hand selected. Press 1 or 2 to select.";
            return;
        }

        feedback =
            SelectionName + " selected. SHIFT + click to move; " +
            "R to return both hands.";
    }

    public void ResetBothHands()
    {
        if (!initialized)
            return;

        leftHandResting = true;
        rightHandResting = true;

        leftTarget = leftHandRest.position;
        rightTarget = rightHandRest.position;

        compressionVisualPlaying = false;

        feedback = "Returning both hands to the camera.";
    }

    private void SetDestination(Vector2 pointerPosition)
    {
        Ray ray = interactionCamera.ScreenPointToRay(pointerPosition);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            maxClickDistance,
            clickableLayers,
            QueryTriggerInteraction.Collide
        );

        float nearestDistance = float.PositiveInfinity;
        RaycastHit nearestHit = default(RaycastHit);
        bool found = false;

        foreach (RaycastHit hit in hits)
        {
            // Ignore colliders belonging to this hand rig.
            if (hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestHit = hit;
                found = true;
            }
        }

        if (!found)
        {
            feedback = "No surface found. Click an object with a collider.";
            return;
        }

        Vector3 target =
            nearestHit.point + nearestHit.normal * surfaceOffset;

        if (selectedHand == HandSelection.Left)
        {
            leftHandResting = false;
            leftTarget = target;
        }
        else
        {
            rightHandResting = false;
            rightTarget = target;
        }

        feedback =
            "Moving " + SelectionName + " hand to " +
            nearestHit.collider.name + ".";
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        if (leftHandResting)
        {
            leftTarget = leftHandRest.position;
            leftHand.rotation =
                leftHandRest.rotation * leftRestRotationOffset;
        }
        else
        {
            leftHand.rotation = leftOriginalRotation;
        }

        if (rightHandResting)
        {
            rightTarget = rightHandRest.position;
            rightHand.rotation =
                rightHandRest.rotation * rightRestRotationOffset;
        }
        else
        {
            rightHand.rotation = rightOriginalRotation;
        }

        float step = Mathf.Max(0f, moveSpeed) * Time.deltaTime;

        leftPalmPosition = Vector3.MoveTowards(
            leftPalmPosition, leftTarget, step
        );

        rightPalmPosition = Vector3.MoveTowards(
            rightPalmPosition, rightTarget, step
        );

        Vector3 visualOffset = Vector3.zero;

        if (compressionVisualPlaying)
        {
            float duration = Mathf.Max(
                0.05f, compressionVisualDuration
            );

            float progress =
                (Time.time - compressionVisualStartTime) / duration;

            if (progress >= 1f)
            {
                compressionVisualPlaying = false;
            }
            else
            {
                float movement = Mathf.Sin(
                    Mathf.Clamp01(progress) * Mathf.PI
                );

                visualOffset =
                    compressionDirection.normalized *
                    Mathf.Max(0f, compressionVisualDistance) *
                    movement;
            }
        }

        Vector3 leftOffset = leftHandResting
            ? Vector3.zero
            : visualOffset;

        Vector3 rightOffset = rightHandResting
            ? Vector3.zero
            : visualOffset;

        // Position the palms after rotating the hand bones.
        leftHand.position +=
            leftPalmPosition + leftOffset - leftPalmContact.position;

        rightHand.position +=
            rightPalmPosition + rightOffset - rightPalmContact.position;
    }

    private string SelectionName
    {
        get
        {
            if (selectedHand == HandSelection.Left)
                return "LEFT";

            if (selectedHand == HandSelection.Right)
                return "RIGHT";

            return "NONE";
        }
    }

    private Rect InstructionPanelRect
    {
        get
        {
            return new Rect(
                16f, 16f,
                Mathf.Min(560f, Screen.width - 32f),
                86f
            );
        }
    }

    private Rect ControlsButtonRect
    {
        get
        {
            return new Rect(
                Screen.width - 126f,
                Screen.height - 46f,
                110f, 30f
            );
        }
    }

    private Rect HandStatusRect
    {
        get
        {
            return new Rect(
                Screen.width - 272f,
                Screen.height - 46f,
                138f, 30f
            );
        }
    }

    private Rect ControlsPanelRect
    {
        get
        {
            float width = Mathf.Min(340f, Screen.width - 32f);

            return new Rect(
                Screen.width - width - 16f,
                Screen.height - 330f,
                width, 274f
            );
        }
    }

    private void CreateStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 16;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 13;
        textStyle.wordWrap = true;
        textStyle.normal.textColor = Color.white;

        statusStyle = new GUIStyle(GUI.skin.box);
        statusStyle.fontSize = 12;
        statusStyle.alignment = TextAnchor.MiddleCenter;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 13;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = Texture2D.whiteTexture;
    }

    private void OnGUI()
    {
        if (!initialized)
            return;

        CreateStyles();

        statusStyle.normal.textColor =
            selectedHand == HandSelection.Left ? Color.cyan :
            selectedHand == HandSelection.Right ? Color.yellow :
            Color.white;

        GUI.Box(
            HandStatusRect,
            "Hand: " + SelectionName,
            statusStyle
        );

        if (GUI.Button(
            ControlsButtonRect,
            showControls ? "Hide controls" : "Controls",
            buttonStyle))
        {
            showControls = !showControls;
        }

        if (!showControls)
            return;

        Rect panel = ControlsPanelRect;

        Color previousBackground = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.08f, 0.10f, 0.13f, 0.94f);

        GUI.Box(panel, GUIContent.none, panelStyle);

        GUI.backgroundColor = previousBackground;

        GUI.Label(
            new Rect(
                panel.x + 14f, panel.y + 10f,
                panel.width - 60f, 24f
            ),
            "CONTROLS",
            titleStyle
        );

        if (GUI.Button(
            new Rect(panel.xMax - 38f, panel.y + 8f, 26f, 26f),
            "X",
            buttonStyle))
        {
            showControls = false;
        }

        GUI.Label(
            new Rect(
                panel.x + 14f, panel.y + 44f,
                panel.width - 28f, 110f
            ),
            "HANDS\n" +
            "1 / 2: select left / right hand\n" +
            "Shift + left-click: move selected hand\n" +
            "R: return both hands to the camera\n" +
            "Esc: deselect hand",
            textStyle
        );

        GUI.Label(
            new Rect(
                panel.x + 14f, panel.y + 158f,
                panel.width - 28f, 70f
            ),
            "MOVEMENT\n" +
            "WASD: walk | Shift: faster | C: crouch / stand\n" +
            "Hold Left Alt + touchpad: look around",
            textStyle
        );

        GUI.Label(
            new Rect(
                panel.x + 14f, panel.y + 232f,
                panel.width - 28f, 36f
            ),
            feedback,
            textStyle
        );
    }
}