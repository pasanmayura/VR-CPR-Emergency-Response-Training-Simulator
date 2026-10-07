using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CPRMetronome : MonoBehaviour
{
    [Header("Beat settings")]
    public float beatsPerMinute = 110f;

    [Range(0f, 1f)]
    public float volume = 0.4f;

    private AudioSource audioSource;
    private AudioClip clickClip;

    private bool running;
    private double nextBeatTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        clickClip = CreateClickSound();
    }

    public void StartMetronome()
    {
        if (running || !isActiveAndEnabled)
            return;

        running = true;
        nextBeatTime = AudioSettings.dspTime;
    }

    public void StopMetronome()
    {
        running = false;

        if (audioSource != null)
            audioSource.Stop();
    }

    private void Update()
    {
        if (!running)
            return;

        double now = AudioSettings.dspTime;

        if (now < nextBeatTime)
            return;

        audioSource.PlayOneShot(clickClip, volume);

        double interval = 60.0 / Mathf.Max(1f, beatsPerMinute);

        // Keep the beat anchored to its original timing.
        // Skip missed beats instead of playing several at once.
        double intervalsPassed =
            System.Math.Floor((now - nextBeatTime) / interval) + 1;

        nextBeatTime += intervalsPassed * interval;
    }

    private AudioClip CreateClickSound()
    {
        const int sampleRate = 44100;
        const float duration = 0.04f;
        const float frequency = 1000f;

        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;

            float attack = Mathf.Clamp01(time / 0.002f);
            float decay = Mathf.Exp(-time * 120f);

            samples[i] =
                Mathf.Sin(2f * Mathf.PI * frequency * time) *
                attack * decay;
        }

        AudioClip clip = AudioClip.Create(
            "CPR Metronome Click",
            sampleCount,
            1,
            sampleRate,
            false);

        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDisable()
    {
        StopMetronome();
    }

    private void OnDestroy()
    {
        if (clickClip != null)
            Destroy(clickClip);
    }
}