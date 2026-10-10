using UnityEngine;
using TMPro;

public static class CPRInteraction
{
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

    public static void ShowInstruction(string title, string message)
    {
        if (instructionText == null)
            return;

        string content = title + "\n\n" + message;

        if (instructionText.text != content)
            instructionText.text = content;
    }

}