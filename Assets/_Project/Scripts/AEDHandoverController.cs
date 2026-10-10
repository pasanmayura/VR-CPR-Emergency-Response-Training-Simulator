using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class AEDHandoverController : MonoBehaviour
{
    [Header("Instruction audio")]
    public AudioClip shockDeliveredClip;
    public AudioClip recoveryClip;
    public AudioClip ambulanceArrivedClip;
    public AudioClip trainingCompleteClip;
    private AudioSource instructionAudio;

    private void Awake()
    {
        instructionAudio = gameObject.AddComponent<AudioSource>();
        instructionAudio.playOnAwake = false;
        instructionAudio.loop = false;
        instructionAudio.spatialBlend = 0f;
    }

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
        if (endingStarted || !isActiveAndEnabled)
            return;

        endingStarted = true;
        StartCoroutine(PostShockEnding());
    }

    private IEnumerator PostShockEnding()
    {
        PlayInstruction(shockDeliveredClip);
        yield return WaitForInstruction(shockMessageDuration);

        CPRInteraction.ShowInstruction(
            "10. RECOVERY — SIMULATION",
            "Scripted scenario outcome: the person shows signs " +
            "of life and is breathing normally. " +
            "Stay beside them and monitor their breathing. " +
            "Leave the AED switched on and the pads attached.");

        PlayInstruction(recoveryClip);
        onSimulatedRecovery.Invoke();

        yield return WaitForInstruction(ambulanceArrivalDelay);

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

        PlayInstruction(ambulanceArrivedClip);
        onAmbulanceArrived.Invoke();

        yield return WaitForInstruction(handoverDuration);

        CompleteHandover();
    }

    public void CompleteHandover()
    {
        if (!AmbulanceArrived || ScenarioCompleted)
            return;

        ScenarioCompleted = true;
        PlayInstruction(trainingCompleteClip);

        CPRInteraction.ShowInstruction(
            "12. TRAINING COMPLETE",
            "Simulated handover completed. " +
            "The ambulance team has taken over.");

        Debug.Log(
            "CPR/AED training scenario completed.",
            this);

        onScenarioCompleted.Invoke();
    }

    private void PlayInstruction(AudioClip clip)
    {
        instructionAudio.Stop();
        instructionAudio.clip = clip;
        if (clip != null)
            instructionAudio.Play();
    }

    private IEnumerator WaitForInstruction(float minimumDuration)
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, minimumDuration));
        yield return new WaitUntil(() => !instructionAudio.isPlaying);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (instructionAudio != null)
            instructionAudio.Stop();
    }
}
