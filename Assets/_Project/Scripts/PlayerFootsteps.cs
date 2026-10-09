using UnityEngine;
using Unity.XR.CoreUtils;

// The player's own footsteps.
// Measures how far the head (XR camera) moves along the floor - with the
// joystick (continuous move) or by really walking in room-scale - and plays one
// footstep every strideLength metres, from the floor under the player (3D).
// Turning on the spot makes no sound; a teleport plays a single landing step.
public class PlayerFootsteps : MonoBehaviour
{
    public AudioClip[] footsteps;

    [Range(0f, 1f)] public float volume = 0.5f;
    [Range(0f, 0.3f)] public float pitchVariation = 0.08f;

    [Tooltip("Metres walked per footstep.")]
    [Min(0.1f)] public float strideLength = 0.7f;

    [Tooltip("A jump larger than this in one frame is a teleport (metres).")]
    [Min(0.5f)] public float teleportDistance = 1f;

    private XROrigin origin;
    private Transform head;
    private AudioSource source;
    private Vector3 lastPosition;
    private float walked;
    private int lastClip = -1;

    private void Start()
    {
        origin = FindObjectOfType<XROrigin>();
        head = origin != null && origin.Camera != null
            ? origin.Camera.transform
            : (Camera.main != null ? Camera.main.transform : null);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f; // 3D, from the floor under the player
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 0.5f;
        source.maxDistance = 10f;
        source.dopplerLevel = 0f;
        source.reverbZoneMix = 1f;

        if (head != null)
            lastPosition = Flat(head.position);
    }

    private void Update()
    {
        if (head == null || footsteps == null || footsteps.Length == 0)
            return;

        Vector3 position = Flat(head.position);
        float moved = Vector3.Distance(position, lastPosition);
        lastPosition = position;

        if (moved > teleportDistance)
        {
            walked = 0f;
            Step(); // landing after a teleport
            return;
        }

        walked += moved;
        if (walked >= strideLength)
        {
            walked -= strideLength;
            Step();
        }
    }

    private void Step()
    {
        // Floor under the player: the XR Origin stands on the floor (Floor tracking mode)
        float floorY = origin != null ? origin.transform.position.y : head.position.y - 1.6f;
        transform.position = new Vector3(head.position.x, floorY, head.position.z);

        // Never the same clip twice in a row
        int index = Random.Range(0, footsteps.Length);
        if (footsteps.Length > 1 && index == lastClip)
            index = (index + 1) % footsteps.Length;
        lastClip = index;

        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.PlayOneShot(footsteps[index], volume);
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
