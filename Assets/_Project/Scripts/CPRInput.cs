using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Input and instruction-text helpers shared by the CPR scripts.
//
// WasPressed: reads a named CPR input action (XR controller button + keyboard
// fallback, see Assets/_Project/Input/CPRInputActions). If no action is
// assigned in the Inspector, the old hard-coded key(s) are used instead, so
// scenes without XR (e.g. CPR_Gameplay) keep working exactly as before.
//
// Text: returns the VR wording of an instruction when the scene has an
// XR Origin, and the original desktop wording otherwise.
public static class CPRInput
{
    public static bool IsVR { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialise()
    {
        DetectVR();
        SceneManager.sceneLoaded += (scene, mode) => DetectVR();
    }

    private static void DetectVR()
    {
        IsVR = Object.FindObjectOfType<XROrigin>() != null;
    }

    public static string Text(string desktopText, string vrText)
    {
        return IsVR ? vrText : desktopText;
    }

#if ENABLE_INPUT_SYSTEM
    public static bool WasPressed(
        InputActionReference actionReference,
        params Key[] fallbackKeys)
    {
        InputAction action =
            actionReference != null ? actionReference.action : null;

        if (action != null)
        {
            if (!action.enabled)
                action.Enable();

            return action.WasPressedThisFrame();
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return false;

        foreach (Key key in fallbackKeys)
        {
            if (keyboard[key].wasPressedThisFrame)
                return true;
        }

        return false;
    }
#endif
}
