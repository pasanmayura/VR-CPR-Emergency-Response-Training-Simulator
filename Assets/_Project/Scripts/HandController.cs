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

    [Header("Movement settings")]
    public float moveSpeed = 1f;
    public float surfaceOffset = 0f;
    public float maxClickDistance = 100f;
    public LayerMask clickableLayers = Physics.DefaultRaycastLayers;

    [Header("Desktop testing")]
    public bool disableHandAnimators = true;

    private enum HandSelection
    {
        None,
        Left,
        Right
    }

    private HandSelection selectedHand = HandSelection.None;
    private bool initialized;

    private Vector3 leftTarget;
    private Vector3 rightTarget;

    private Vector3 leftPalmPosition;
    private Vector3 rightPalmPosition;

    private Vector3 leftInitialPosition;
    private Vector3 rightInitialPosition;

    private string feedback =
        "Select a hand: press 1 for LEFT or 2 for RIGHT.";

    [Header("Compression visual")]
    public float compressionVisualDistance = 0.02f;
    public float compressionVisualDuration = 0.24f;
    public Vector3 compressionDirection = Vector3.down;

    private bool compressionVisualPlaying;
    private float compressionVisualStartTime;

    public void PlayCompressionVisual()
    {
        if (!initialized || !isActiveAndEnabled)
            return;

        compressionVisualStartTime = Time.time;
        compressionVisualPlaying = true;
    }
    private void Start()
    {
        if (leftHand == null ||
            rightHand == null ||
            leftPalmContact == null ||
            rightPalmContact == null ||
            interactionCamera == null)
        {
            StopWithError(
                "Assign both hand bones, both palm contact points, " +
                "and Main Camera.");
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

        if (disableHandAnimators)
        {
            foreach (Animator animator in
                     GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }
        }

        leftPalmPosition = leftPalmContact.position;
        rightPalmPosition = rightPalmContact.position;

        // Remember where each palm was when the simulation started.
        leftInitialPosition = leftPalmPosition;
        rightInitialPosition = rightPalmPosition;

        leftTarget = leftPalmPosition;
        rightTarget = rightPalmPosition;

        selectedHand = HandSelection.None;
        initialized = true;
    }

    private void StopWithError(string message)
    {
        Debug.LogError("HandController: " + message, this);
        enabled = false;
    }

    private void Update()
    {
        if (!initialized)
            return;

        bool clicked = false;
        bool shiftHeld = false;
        bool returnPressed = false;

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

            returnPressed = keyboard.rKey.wasPressedThisFrame;

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

        returnPressed = Input.GetKeyDown(KeyCode.R);

        shiftHeld =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        clicked = Input.GetMouseButtonDown(0);
        pointerPosition = Input.mousePosition;
#endif

        if (returnPressed)
        {
            ReturnSelectedHand();
            return;
        }

        if (!clicked)
            return;

        // The emergency call button uses a normal left-click.
        if (CPRInteraction.PointerOverCallButton(pointerPosition))
            return;

        Vector2 guiPosition = new Vector2(
            pointerPosition.x,
            Screen.height - pointerPosition.y);

        // Avoid moving hands when clicking the instruction panels.
        if (InstructionPanelRect.Contains(guiPosition) ||
            HandPanelRect.Contains(guiPosition))
            return;

        if (!shiftHeld)
        {
            feedback =
                "Hold SHIFT and left-click to move a hand.";
            return;
        }

        if (selectedHand == HandSelection.None)
        {
            feedback =
                "No hand selected. Press 1 for LEFT or 2 for RIGHT.";
            return;
        }

        SetDestination(pointerPosition);
    }

    private void SelectHand(HandSelection selection)
    {
        selectedHand = selection;

        if (selection == HandSelection.None)
        {
            // Stop movement without returning or withdrawing hands.
            leftPalmPosition = leftPalmContact.position;
            rightPalmPosition = rightPalmContact.position;

            leftTarget = leftPalmPosition;
            rightTarget = rightPalmPosition;

            feedback =
                "No hand selected. Press 1 for LEFT or 2 for RIGHT.";
            return;
        }

        feedback =
            SelectionName + " selected. SHIFT + click to move; " +
            "R to return.";
    }

    private void ReturnSelectedHand()
    {
        if (selectedHand == HandSelection.None)
        {
            feedback =
                "Select a hand first: 1 = LEFT or 2 = RIGHT.";
            return;
        }

        if (selectedHand == HandSelection.Left)
            leftTarget = leftInitialPosition;
        else
            rightTarget = rightInitialPosition;

        feedback =
            "Returning " + SelectionName +
            " hand to its initial position.";
    }

    private void SetDestination(Vector2 pointerPosition)
    {
        Ray ray = interactionCamera.ScreenPointToRay(pointerPosition);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            maxClickDistance,
            clickableLayers,
            QueryTriggerInteraction.Collide);

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
            feedback =
                "No surface found. Shift-click an object with a collider.";
            return;
        }

        Vector3 target =
            nearestHit.point + nearestHit.normal * surfaceOffset;

        if (selectedHand == HandSelection.Left)
            leftTarget = target;
        else
            rightTarget = target;

        feedback =
            "Moving " + SelectionName +
            " hand to " + nearestHit.collider.name + ".";
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        float step = Mathf.Max(0f, moveSpeed) * Time.deltaTime;

        leftPalmPosition = Vector3.MoveTowards(
            leftPalmPosition, leftTarget, step);

        rightPalmPosition = Vector3.MoveTowards(
            rightPalmPosition, rightTarget, step);

        Vector3 visualOffset = Vector3.zero;

        if (compressionVisualPlaying)
        {
            float duration = Mathf.Max(
                0.05f, compressionVisualDuration);

            float progress =
                (Time.time - compressionVisualStartTime) / duration;

            if (progress >= 1f)
            {
                compressionVisualPlaying = false;
            }
            else
            {
                // Smoothly move down, then return to the resting position.
                float movement = Mathf.Sin(
                    Mathf.Clamp01(progress) * Mathf.PI);

                visualOffset =
                    compressionDirection.normalized *
                    Mathf.Max(0f, compressionVisualDistance) *
                    movement;
            }
        }

        leftHand.position +=
            leftPalmPosition + visualOffset - leftPalmContact.position;

        rightHand.position +=
            rightPalmPosition + visualOffset - rightPalmContact.position;
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
                16, 16,
                Mathf.Min(560f, Screen.width - 32f), 86);
        }
    }

    private Rect HandPanelRect
    {
        get
        {
            float width = Mathf.Min(330f, Screen.width - 32f);

            return new Rect(
                Screen.width - width - 16,
                174, width, 162);
        }
    }

    private void OnGUI()
    {
        if (!initialized)
            return;

        Rect panel = HandPanelRect;
        GUI.Box(panel, GUIContent.none);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize = 15;

        titleStyle.normal.textColor =
            selectedHand == HandSelection.Left ? Color.cyan :
            selectedHand == HandSelection.Right ? Color.yellow :
            Color.white;

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 13;
        textStyle.wordWrap = true;
        textStyle.normal.textColor = Color.white;

        GUI.Label(
            new Rect(panel.x + 10, panel.y + 6, panel.width - 20, 22),
            "SELECTED HAND: " + SelectionName,
            titleStyle);

        GUI.Label(
            new Rect(panel.x + 10, panel.y + 30, panel.width - 20, 62),
            "1 = LEFT | 2 = RIGHT | Esc = deselect\n" +
            "SHIFT + left-click = move | R = return\n" +
            "Left-click alone = UI buttons",
            textStyle);

        GUI.Label(
            new Rect(panel.x + 10, panel.y + 96, panel.width - 20, 38),
            feedback,
            textStyle);

        if (selectedHand != HandSelection.None)
        {
            float remaining =
                selectedHand == HandSelection.Left
                ? Vector3.Distance(leftPalmContact.position, leftTarget)
                : Vector3.Distance(rightPalmContact.position, rightTarget);

            GUI.Label(
                new Rect(
                    panel.x + 10, panel.y + 136,
                    panel.width - 20, 22),
                "Palm distance: " + remaining.ToString("F3") + " units",
                textStyle);
        }
    }
}