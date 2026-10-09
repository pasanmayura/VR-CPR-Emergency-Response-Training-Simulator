using UnityEngine;
using TMPro;

public static class CPRInteraction
{
    public static bool CallButtonVisible;

    private static TMP_Text instructionText;

    public static void RegisterInstructionText(TMP_Text text)
    {
        instructionText = text;
    }

    public static void UnregisterInstructionText(TMP_Text text)
    {
        if (instructionText == text)
            instructionText = null;
    }

    public static Rect CallButtonRect
    {
        get
        {
            return new Rect(
                16, 110,
                Mathf.Min(560f, Screen.width - 32f), 48);
        }
    }

    public static bool Touching(
        Transform palm, Collider zone, float tolerance)
    {
        if (palm == null || zone == null ||
            !zone.enabled || !zone.gameObject.activeInHierarchy)
            return false;

        Vector3 closest = zone.ClosestPoint(palm.position);
        float distance = Mathf.Max(0f, tolerance);

        return (closest - palm.position).sqrMagnitude
               <= distance * distance;
    }

    public static bool PointerOverCallButton(Vector2 pointer)
    {
        Vector2 guiPosition = new Vector2(
            pointer.x, Screen.height - pointer.y);

        return CallButtonVisible &&
               CallButtonRect.Contains(guiPosition);
    }

    public static void ShowInstruction(string title, string message)
    {
        if (instructionText == null)
            return;

        string content = title + "\n\n" + message;

        if (instructionText.text != content)
            instructionText.text = content;
    }

    public static bool ShowCallButton()
    {
        Color oldBackground = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.35f);

        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;

        bool clicked = GUI.Button(
            CallButtonRect,
            "CALL EMERGENCY ASSISTANCE — SIMULATION",
            style);

        GUI.backgroundColor = oldBackground;
        return clicked;
    }
}