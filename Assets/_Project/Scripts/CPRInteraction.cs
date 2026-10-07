using UnityEngine;

public static class CPRInteraction
{
    public static bool CallButtonVisible;

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
        float width = Mathf.Min(560f, Screen.width - 32f);

        Color oldBackground = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.15f, 0.19f, 0.24f);

        GUI.Box(new Rect(16, 16, width, 86), GUIContent.none);
        GUI.backgroundColor = oldBackground;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 16;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.cyan;

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 14;
        textStyle.wordWrap = true;
        textStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(28, 23, width - 24, 24),
                  title, titleStyle);

        GUI.Label(new Rect(28, 49, width - 24, 46),
                  message, textStyle);
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