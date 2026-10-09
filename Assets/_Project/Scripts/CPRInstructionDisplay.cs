using UnityEngine;
using TMPro;

public class CPRInstructionDisplay : MonoBehaviour
{
    public TMP_Text instructionText;

    private void OnEnable()
    {
        if (instructionText == null)
        {
            Debug.LogError(
                "Assign InstructionText to CPRInstructionDisplay.",
                this);
            return;
        }

        CPRInteraction.RegisterInstructionText(instructionText);
    }

    private void OnDisable()
    {
        CPRInteraction.UnregisterInstructionText(instructionText);
    }
}