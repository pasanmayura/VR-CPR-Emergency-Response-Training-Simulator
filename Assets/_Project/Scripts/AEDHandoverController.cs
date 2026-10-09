using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class AEDHandoverController : MonoBehaviour
{
    [Header("Training delays")]
    [Min(0.1f)]
    public float shockMessageDuration = 3f;

    [Min(0.1f)]
    public float ambulanceArrivalDelay = 15f;

    [Min(0.1f)]
    public float handoverDuration = 5f;

    [Header("Optional ambulance visual")]
    public GameObject ambulanceVisual;

    [Header("Events")]
    public UnityEvent onSimulatedRecovery = new UnityEvent();
    public UnityEvent onAmbulanceArrived = new UnityEvent();
    public UnityEvent onScenarioCompleted = new UnityEvent();

    public bool AmbulanceArrived { get; private set; }
    public bool ScenarioCompleted { get; private set; }

    private bool endingStarted;

    public void BeginPostShockEnding()
    {
        if (endingStarted)
            return;

        endingStarted = true;
        StartCoroutine(PostShockEnding());
    }

    private IEnumerator PostShockEnding()
    {
        yield return new WaitForSeconds(
            Mathf.Max(0.1f, shockMessageDuration));

        CPRInteraction.ShowInstruction(
            "10. RECOVERY — SIMULATION",
            "Scripted scenario outcome: the person shows signs " +
            "of life and is breathing normally. " +
            "Stay beside them and monitor their breathing. " +
            "Leave the AED switched on and the pads attached.");

        onSimulatedRecovery.Invoke();

        yield return new WaitForSeconds(
            Mathf.Max(0.1f, ambulanceArrivalDelay));

        AmbulanceArrived = true;

        if (ambulanceVisual != null)
            ambulanceVisual.SetActive(true);

        CPRInteraction.ShowInstruction(
            "11. AMBULANCE ARRIVED — SIMULATION",
            "The ambulance team has arrived. " +
            "Simulated handover: the person was unresponsive, " +
            "CPR was performed, one simulated AED shock was " +
            "delivered, and normal breathing returned " +
            "in this scenario.");

        onAmbulanceArrived.Invoke();

        yield return new WaitForSeconds(
            Mathf.Max(0.1f, handoverDuration));

        CompleteHandover();
    }

    public void CompleteHandover()
    {
        if (!AmbulanceArrived || ScenarioCompleted)
            return;

        ScenarioCompleted = true;

        CPRInteraction.ShowInstruction(
            "12. TRAINING COMPLETE",
            "Simulated handover completed. " +
            "The ambulance team has taken over.");

        Debug.Log(
            "CPR/AED training scenario completed.",
            this);

        onScenarioCompleted.Invoke();
    }
}