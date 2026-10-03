using UnityEngine;

public class AssessmentManager : MonoBehaviour
{
    public bool EmergencyRecognized { get; private set; }
    public bool ResponsivenessChecked { get; private set; }
    public bool AssistanceRequested { get; private set; }
    public bool CPRHandPositionCorrect { get; private set; }
    public bool CPRCompleted { get; private set; }
    public bool AEDRetrieved { get; private set; }
    public bool AEDProcedureCompleted { get; private set; }

    public void MarkEmergencyRecognized()
    {
        EmergencyRecognized = true;
    }

    public void MarkResponsivenessChecked()
    {
        ResponsivenessChecked = true;
    }

    public void MarkAssistanceRequested()
    {
        AssistanceRequested = true;
    }

    public void MarkCPRHandPositionCorrect()
    {
        CPRHandPositionCorrect = true;
    }

    public void MarkCPRCompleted()
    {
        CPRCompleted = true;
    }

    public void MarkAEDRetrieved()
    {
        AEDRetrieved = true;
    }

    public void MarkAEDProcedureCompleted()
    {
        AEDProcedureCompleted = true;
    }

    public bool IsTrainingComplete()
    {
        return EmergencyRecognized
            && ResponsivenessChecked
            && AssistanceRequested
            && CPRHandPositionCorrect
            && CPRCompleted
            && AEDRetrieved
            && AEDProcedureCompleted;
    }

    public int GetCompletedTaskCount()
    {
        int count = 0;

        if (EmergencyRecognized) count++;
        if (ResponsivenessChecked) count++;
        if (AssistanceRequested) count++;
        if (CPRHandPositionCorrect) count++;
        if (CPRCompleted) count++;
        if (AEDRetrieved) count++;
        if (AEDProcedureCompleted) count++;

        return count;
    }

    public void ResetAssessment()
    {
        EmergencyRecognized = false;
        ResponsivenessChecked = false;
        AssistanceRequested = false;
        CPRHandPositionCorrect = false;
        CPRCompleted = false;
        AEDRetrieved = false;
        AEDProcedureCompleted = false;
    }
}