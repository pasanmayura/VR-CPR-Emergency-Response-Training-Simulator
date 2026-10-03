using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public enum TrainingStage
    {
        Start,
        Tutorial,
        Emergency,
        CheckResponsiveness,
        CallForHelp,
        CPRPosition,
        CPR,
        AED,
        Assessment,
        Complete
    }

    public TrainingStage CurrentStage { get; private set; } = TrainingStage.Start;

    public void SetStage(TrainingStage newStage)
    {
        CurrentStage = newStage;

        Debug.Log("Training Stage: " + CurrentStage);
    }
}