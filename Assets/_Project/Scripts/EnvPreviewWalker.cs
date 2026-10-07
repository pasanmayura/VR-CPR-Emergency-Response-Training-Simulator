using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EnvPreviewWalker : MonoBehaviour
{
    [Header("Movement")]
    public float eyeHeight = 1.6f;
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float lookSpeed = 2f;

    [Header("Crouching")]
    public float crouchEyeHeight = 0.9f;
    public float crouchBodyHeight = 1f;
    public float crouchWalkSpeed = 1f;
    public float crouchTransitionSpeed = 3f;
    public LayerMask obstacleLayers = ~0;

    private CharacterController body;
    private float yaw;
    private float pitch;
    private float fallSpeed;
    private bool isCrouching;

    private const float StandingBodyHeight = 1.6f;
    private const float BodyBottomOffset = 0.1f;

    void Awake()
    {
        crouchBodyHeight = Mathf.Clamp(
            crouchBodyHeight, 0.6f, StandingBodyHeight
        );
        crouchEyeHeight = Mathf.Clamp(
            crouchEyeHeight, 0.3f, eyeHeight
        );

        GameObject bodyGo = new GameObject("PreviewBody");
        bodyGo.transform.position =
            transform.position - Vector3.up * eyeHeight;

        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;

        if (pitch > 180f)
            pitch -= 360f;

        pitch = Mathf.Clamp(pitch, -80f, 80f);

        bodyGo.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        body = bodyGo.AddComponent<CharacterController>();
        body.height = StandingBodyHeight;
        body.radius = 0.25f;
        body.center = new Vector3(
            0f, StandingBodyHeight / 2f + BodyBottomOffset, 0f
        );
        body.stepOffset = 0.3f;

        transform.SetParent(bodyGo.transform);
        transform.localPosition = new Vector3(0f, eyeHeight, 0f);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Update()
    {
        Vector2 move = Vector2.zero;
        Vector2 look = Vector2.zero;

        bool run = false;
        bool looking = false;
        bool toggleCrouch = false;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed) move.y -= 1f;
            if (keyboard.dKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed) move.x -= 1f;

            run = keyboard.leftShiftKey.isPressed;
            looking = keyboard.leftAltKey.isPressed;
            toggleCrouch = keyboard.cKey.wasPressedThisFrame;
        }

        if (mouse != null)
        {
            looking |= mouse.rightButton.isPressed;
            look = mouse.delta.ReadValue() * 0.1f;
        }
#else
        move = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        run = Input.GetKey(KeyCode.LeftShift);
        looking = Input.GetMouseButton(1) ||
                  Input.GetKey(KeyCode.LeftAlt);
        toggleCrouch = Input.GetKeyDown(KeyCode.C);

        look = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );
#endif

        if (toggleCrouch)
        {
            if (!isCrouching)
                isCrouching = true;
            else if (CanStand())
                isCrouching = false;
        }

        // Stay crouched if there is an obstacle above.
        if (!isCrouching &&
            body.height < StandingBodyHeight - 0.01f &&
            !CanStand())
        {
            isCrouching = true;
        }

        UpdateCrouchHeight();

        if (looking)
        {
            yaw += look.x * lookSpeed;
            pitch = Mathf.Clamp(
                pitch - look.y * lookSpeed, -80f, 80f
            );
        }

        body.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        Vector3 direction =
            body.transform.forward * move.y +
            body.transform.right * move.x;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        bool lowered = isCrouching ||
                       body.height < StandingBodyHeight - 0.01f;

        float speed = lowered
            ? crouchWalkSpeed
            : (run ? runSpeed : walkSpeed);

        Vector3 velocity = direction * speed;

        fallSpeed = body.isGrounded
            ? -1f
            : fallSpeed - 9.81f * Time.deltaTime;

        velocity.y = fallSpeed;
        body.Move(velocity * Time.deltaTime);
    }

    void UpdateCrouchHeight()
    {
        float targetHeight = isCrouching
            ? crouchBodyHeight
            : StandingBodyHeight;

        body.height = Mathf.MoveTowards(
            body.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        // Keep the bottom of the collider at the same height.
        body.center = new Vector3(
            0f, body.height / 2f + BodyBottomOffset, 0f
        );

        float targetEyeHeight = isCrouching
            ? crouchEyeHeight
            : eyeHeight;

        float cameraHeight = Mathf.MoveTowards(
            transform.localPosition.y,
            targetEyeHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        transform.localPosition = new Vector3(0f, cameraHeight, 0f);
    }

    bool CanStand()
    {
        Vector3 bottom = body.transform.position +
                         Vector3.up * BodyBottomOffset;

        // Slightly smaller radius avoids detecting floor contact.
        float radius = body.radius - 0.02f;

        Vector3 lowerPoint = bottom + Vector3.up * body.radius;
        Vector3 upperPoint = bottom +
            Vector3.up * (StandingBodyHeight - body.radius);

        Collider[] obstacles = Physics.OverlapCapsule(
            lowerPoint,
            upperPoint,
            radius,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider obstacle in obstacles)
        {
            // Ignore our own controller and its child objects.
            if (obstacle == body ||
                obstacle.transform.IsChildOf(body.transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }
}