// EnvPreviewWalker.cs
// Add this to the Main Camera in Env_TrainingRoom (NOT in an Editor folder).
// Press Play, then:  W A S D = walk,  Shift = walk faster,  hold Right Mouse = look around.
// For previewing the environment only. The VR player rig is set up separately by the XR team.

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EnvPreviewWalker : MonoBehaviour
{
    public float eyeHeight = 1.6f;
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float lookSpeed = 2f;

    CharacterController body;
    float yaw, pitch, fallSpeed;

    void Awake()
    {
        // Create a body with a CharacterController so we walk on floors and can't pass through walls
        var bodyGo = new GameObject("PreviewBody");
        bodyGo.transform.position = transform.position - Vector3.up * eyeHeight;
        yaw = transform.eulerAngles.y;
        bodyGo.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        body = bodyGo.AddComponent<CharacterController>();
        body.height = 1.6f;
        body.radius = 0.25f;
        body.center = new Vector3(0f, 0.9f, 0f);
        body.stepOffset = 0.3f;

        transform.SetParent(bodyGo.transform);
        transform.localPosition = new Vector3(0f, eyeHeight, 0f);
        transform.localRotation = Quaternion.identity;
    }

    void Update()
    {
        Vector2 move = Vector2.zero, look = Vector2.zero;
        bool run = false, looking = false;

#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        var m = Mouse.current;
        if (k != null)
        {
            if (k.wKey.isPressed) move.y += 1f;
            if (k.sKey.isPressed) move.y -= 1f;
            if (k.dKey.isPressed) move.x += 1f;
            if (k.aKey.isPressed) move.x -= 1f;
            run = k.leftShiftKey.isPressed;
        }
        if (m != null)
        {
            looking = m.rightButton.isPressed;
            look = m.delta.ReadValue() * 0.1f;
        }
#else
        move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        run = Input.GetKey(KeyCode.LeftShift);
        looking = Input.GetMouseButton(1);
        look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif

        if (looking)
        {
            yaw += look.x * lookSpeed;
            pitch = Mathf.Clamp(pitch - look.y * lookSpeed, -80f, 80f);
        }
        body.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        Vector3 dir = body.transform.forward * move.y + body.transform.right * move.x;
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        Vector3 velocity = dir * (run ? runSpeed : walkSpeed);

        fallSpeed = body.isGrounded ? -1f : fallSpeed - 9.81f * Time.deltaTime;
        velocity.y = fallSpeed;

        body.Move(velocity * Time.deltaTime);
    }
}
