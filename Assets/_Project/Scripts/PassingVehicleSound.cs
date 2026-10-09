using System.Collections;
using UnityEngine;

// A vehicle (e.g. a three-wheeler) driving past the training room now and then.
// The AudioSource plays an engine loop while this object moves along the road
// from roadStart to roadEnd (or the other way). Unity's Doppler effect makes the
// pitch rise as it comes closer and drop as it drives away. Put an
// OutdoorAmbientSound (play mode External) on the same object so it is muffled
// while the player is inside the room.
[RequireComponent(typeof(AudioSource))]
public class PassingVehicleSound : MonoBehaviour
{
    [Header("Road (set by Tools > Pavithra > Setup Spatial Audio)")]
    public Vector3 roadStart;
    public Vector3 roadEnd;

    [Header("Driving")]
    [Tooltip("Speed in metres per second (8 m/s is about 30 km/h).")]
    [Min(0.5f)] public float speed = 8f;

    [Tooltip("Random pause between two vehicles (seconds).")]
    [Min(0f)] public float minPause = 40f;
    [Min(0f)] public float maxPause = 90f;

    [Tooltip("Pause before the first vehicle (seconds).")]
    [Min(0f)] public float firstPause = 15f;

    [Range(0f, 0.3f)] public float pitchVariation = 0.08f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
    }

    private void OnEnable()
    {
        StartCoroutine(DriveForever());
    }

    private IEnumerator DriveForever()
    {
        yield return new WaitForSeconds(firstPause);

        while (true)
        {
            // Random direction each time
            bool reverse = Random.value < 0.5f;
            Vector3 from = reverse ? roadEnd : roadStart;
            Vector3 to = reverse ? roadStart : roadEnd;

            transform.position = from;
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

            if (source.clip != null)
            {
                source.time = Random.Range(0f, source.clip.length);
                source.Play();
            }

            float duration = Vector3.Distance(from, to) / speed;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                transform.position = Vector3.Lerp(from, to, t);
                yield return null;
            }

            source.Stop();
            yield return new WaitForSeconds(Random.Range(minPause, Mathf.Max(minPause, maxPause)));
        }
    }
}
