using UnityEngine;

// A 3D sound from outside the training room (dog barking, wind, birds, traffic).
// - RandomRepeats: plays its clip again and again with a random pause in
//   between, so it does not sound like an obvious loop (e.g. a dog barking).
// - Loop: plays a seamless loop (e.g. wind in the trees, birds).
// - External: another script decides when to play (e.g. PassingVehicleSound);
//   this component only does the muffling below.
// - Unity does not block sound with walls, so while the listener is inside the
//   room the sound is muffled (low-pass) and quieter, as if heard through the
//   wall. Outside the room it is clear and gets louder as the player walks
//   towards it (normal 3D distance rolloff).
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class OutdoorAmbientSound : MonoBehaviour
{
    public enum PlayMode { RandomRepeats, Loop, External }

    public PlayMode playMode = PlayMode.RandomRepeats;

    [Header("Random repeats")]
    [Min(0f)] public float minPause = 4f;
    [Min(0f)] public float maxPause = 12f;
    [Range(0f, 0.5f)] public float pitchVariation = 0.06f;

    [Header("Heard through the wall")]
    [Tooltip("The room's inside space (set by Tools > Pavithra > Setup Spatial Audio).")]
    public Bounds roomBounds;

    [Tooltip("Low-pass cutoff while the listener is inside the room (Hz).")]
    public float insideCutoff = 700f;

    [Range(0f, 1f)]
    [Tooltip("Volume multiplier while the listener is inside the room.")]
    public float insideVolume = 0.4f;

    [Min(0.01f)]
    [Tooltip("Seconds to fade between inside and outside sound.")]
    public float fadeTime = 0.5f;

    private const float OutsideCutoff = 22000f;

    private AudioSource source;
    private AudioLowPassFilter lowPass;
    private AudioListener listener;
    private float baseVolume;
    private float nextPlayTime;
    private float blend = 1f; // 1 = inside (muffled), 0 = outside (clear)

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        lowPass = GetComponent<AudioLowPassFilter>();
        baseVolume = source.volume;
        source.playOnAwake = false;
        nextPlayTime = Time.time + Random.Range(0f, maxPause);

        if (playMode == PlayMode.Loop && source.clip != null)
        {
            source.loop = true;
            source.time = Random.Range(0f, source.clip.length); // loops don't start in sync
            source.Play();
        }
        else if (playMode == PlayMode.RandomRepeats)
        {
            source.loop = false;
        }
    }

    private void Update()
    {
        if (listener == null || !listener.isActiveAndEnabled)
            listener = FindObjectOfType<AudioListener>();

        // ---------- muffle while inside the room ----------
        bool inside = listener != null && roomBounds.Contains(listener.transform.position);
        blend = Mathf.MoveTowards(blend, inside ? 1f : 0f, Time.deltaTime / fadeTime);

        // Interpolate the cutoff on a log scale so the fade sounds even
        lowPass.cutoffFrequency = Mathf.Exp(Mathf.Lerp(
            Mathf.Log(OutsideCutoff), Mathf.Log(insideCutoff), blend));
        source.volume = baseVolume * Mathf.Lerp(1f, insideVolume, blend);

        // ---------- random repeats ----------
        if (playMode != PlayMode.RandomRepeats)
            return;

        if (source.clip == null || source.isPlaying || Time.time < nextPlayTime)
            return;

        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.Play();
        nextPlayTime = Time.time + source.clip.length / source.pitch +
                       Random.Range(minPause, Mathf.Max(minPause, maxPause));
    }
}
