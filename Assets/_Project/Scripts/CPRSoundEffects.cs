using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// World (diegetic) sound effects for the CPR / AED scenario. All sounds are 3D:
// they come from where they happen in the room, get quieter with distance and
// pass through the room's Audio Reverb Zone.
//
// It only listens to the existing CPR scripts (public fields, UnityEvents and
// state) and never changes them:
// - touch sounds when a palm reaches the shoulder, forehead, chin or chest zone
// - a chest "thud" and a controller vibration for every compression
// - AED pad peel / pad place / charge / shock
// - an ambulance siren driving closer (with Doppler) after the shock, then
//   braking, a door and footsteps when the ambulance arrives
// Instruction / voice / phone sounds are NOT part of this script.
// Any clip left empty is simply skipped.
public class CPRSoundEffects : MonoBehaviour
{
    [Header("Touch")]
    public AudioClip shoulderTap;
    public AudioClip skinTouch;
    [Range(0f, 1f)] public float touchVolume = 0.6f;

    [Header("Compressions")]
    public AudioClip[] compressionThuds;
    [Range(0f, 1f)] public float compressionVolume = 0.9f;

    [Tooltip("Controller vibration for each compression (0 = off).")]
    [Range(0f, 1f)] public float compressionHaptics = 0.6f;

    [Header("AED")]
    public AudioClip padPeel;
    public AudioClip padPlace;
    public AudioClip aedCharge;
    public AudioClip aedShock;
    [Range(0f, 1f)] public float aedVolume = 0.9f;

    [Header("Ambulance")]
    public AudioClip sirenLoop;
    public AudioClip ambulanceBrake;
    public AudioClip ambulanceDoor;
    public AudioClip footsteps;
    [Range(0f, 1f)] public float ambulanceVolume = 1f;

    [Tooltip("Where the siren starts, far outside the room.")]
    public Transform sirenStartPoint;

    [Tooltip("Where the ambulance stops, just outside the door.")]
    public Transform ambulanceStopPoint;

    [Tooltip("Where the paramedics' footsteps are heard (the door).")]
    public Transform doorPoint;

    [Header("3D sound settings")]
    [Min(0.01f)] public float minDistance = 0.5f;
    [Min(0.1f)] public float maxDistance = 15f;

    [Range(0f, 0.5f)]
    [Tooltip("Random pitch change so repeated sounds are not identical.")]
    public float pitchVariation = 0.08f;

    // ---------- scene references (found automatically) ----------
    private ResponsivenessCheck responsiveness;
    private AirwayCheck airway;
    private CPRHandPlacement handPlacement;
    private CompressionSystem compressions;
    private AEDController aed;
    private AEDHandoverController handover;
    private XRBaseController[] controllers;

    private class TouchZone
    {
        public Collider zone;
        public Transform[] palms;
        public float tolerance;
        public AudioClip clip;
        public bool[] wasTouching;
    }

    private readonly List<TouchZone> touchZones = new List<TouchZone>();
    private float lastTouchSoundTime;
    private bool chargePlayed;
    private readonly HashSet<Object> peeledPads = new HashSet<Object>();
    private AudioSource siren;

    private void Start()
    {
        responsiveness = FindObjectOfType<ResponsivenessCheck>();
        airway = FindObjectOfType<AirwayCheck>();
        handPlacement = FindObjectOfType<CPRHandPlacement>();
        compressions = FindObjectOfType<CompressionSystem>();
        aed = FindObjectOfType<AEDController>();
        handover = FindObjectOfType<AEDHandoverController>();
        controllers = FindObjectsOfType<XRBaseController>();

        // ---------- touch zones ----------
        if (responsiveness != null)
            AddTouchZone(responsiveness.shoulderZone, shoulderTap,
                responsiveness.contactTolerance,
                responsiveness.leftPalmContact, responsiveness.rightPalmContact);

        if (airway != null)
        {
            AddTouchZone(airway.foreheadZone, skinTouch, airway.contactTolerance,
                airway.leftPalmContact, airway.rightPalmContact);
            AddTouchZone(airway.chinZone, skinTouch, airway.contactTolerance,
                airway.leftPalmContact, airway.rightPalmContact);
        }

        if (handPlacement != null)
        {
            AddTouchZone(handPlacement.chestZone, skinTouch, handPlacement.contactTolerance,
                handPlacement.leftPalmContact, handPlacement.rightPalmContact);
            AddTouchZone(handPlacement.upperHandZone, skinTouch, handPlacement.contactTolerance,
                handPlacement.leftPalmContact, handPlacement.rightPalmContact);
        }

        // ---------- compressions ----------
        if (compressions != null)
            compressions.onCompressionRecorded.AddListener(OnCompression);

        // ---------- AED ----------
        if (aed != null)
        {
            WatchPad(aed.pad1);
            WatchPad(aed.pad2);
            WatchSocket(aed.pad1Socket);
            WatchSocket(aed.pad2Socket);
            aed.onShockDelivered.AddListener(
                () => PlayAt(aedShock, aed.transform.position, aedVolume, 0f));
        }

        // ---------- ambulance ----------
        if (handover != null)
        {
            handover.onSimulatedRecovery.AddListener(StartSiren);
            handover.onAmbulanceArrived.AddListener(OnAmbulanceArrived);
        }
    }

    private void Update()
    {
        CheckTouchZones();

        // The AED charges as soon as it advises a shock.
        if (aed != null && !chargePlayed && aed.ShockAdvised)
        {
            chargePlayed = true;
            PlayAt(aedCharge, aed.transform.position, aedVolume, 0f);
        }
    }

    // ================= Touch =================

    private void AddTouchZone(Collider zone, AudioClip clip, float tolerance,
                              Transform leftPalm, Transform rightPalm)
    {
        if (zone == null || clip == null)
            return;

        touchZones.Add(new TouchZone
        {
            zone = zone,
            clip = clip,
            tolerance = tolerance,
            palms = new[] { leftPalm, rightPalm },
            wasTouching = new bool[2]
        });
    }

    private void CheckTouchZones()
    {
        foreach (TouchZone t in touchZones)
        {
            for (int i = 0; i < t.palms.Length; i++)
            {
                bool touching = CPRInteraction.Touching(t.palms[i], t.zone, t.tolerance);

                // Play only at the moment the palm arrives, not every frame.
                if (touching && !t.wasTouching[i] && Time.time - lastTouchSoundTime > 0.15f)
                {
                    lastTouchSoundTime = Time.time;
                    PlayAt(t.clip, t.palms[i].position, touchVolume, pitchVariation);
                }

                t.wasTouching[i] = touching;
            }
        }
    }

    // ================= Compressions =================

    private void OnCompression(CompressionRecord record)
    {
        if (compressionThuds != null && compressionThuds.Length > 0)
        {
            AudioClip clip = compressionThuds[Random.Range(0, compressionThuds.Length)];
            Vector3 chest = (record.leftPalmPosition + record.rightPalmPosition) * 0.5f;

            // A compression with the hands in the wrong place sounds weaker.
            float volume = record.correctPosition ? compressionVolume : compressionVolume * 0.5f;
            PlayAt(clip, chest, volume, pitchVariation);
        }

        if (compressionHaptics > 0f && controllers != null)
        {
            foreach (XRBaseController controller in controllers)
            {
                if (controller != null)
                    controller.SendHapticImpulse(compressionHaptics, 0.08f);
            }
        }
    }

    // ================= AED =================

    private void WatchPad(XRGrabInteractable pad)
    {
        if (pad == null)
            return;

        // Peeling sound the first time each pad is picked up.
        pad.selectEntered.AddListener(args =>
        {
            if (args.interactorObject is XRSocketInteractor || !peeledPads.Add(pad))
                return;

            PlayAt(padPeel, pad.transform.position, aedVolume, pitchVariation);
        });
    }

    private void WatchSocket(XRSocketInteractor socket)
    {
        if (socket == null)
            return;

        socket.selectEntered.AddListener(args =>
            PlayAt(padPlace, socket.transform.position, aedVolume, pitchVariation));
    }

    // ================= Ambulance =================

    private void StartSiren()
    {
        if (sirenLoop == null || sirenStartPoint == null || ambulanceStopPoint == null)
            return;

        siren = CreateSource("Siren", sirenStartPoint.position, sirenLoop, ambulanceVolume, 0f);
        siren.loop = true;
        siren.dopplerLevel = 1f;       // pitch changes as it drives towards the player
        siren.minDistance = 5f;        // a siren is loud: audible from far away
        siren.maxDistance = 200f;
        siren.Play();

        float travelTime = handover != null ? handover.ambulanceArrivalDelay : 15f;
        StartCoroutine(DriveSiren(travelTime));
    }

    private IEnumerator DriveSiren(float travelTime)
    {
        Vector3 from = sirenStartPoint.position;
        Vector3 to = ambulanceStopPoint.position;
        float t = 0f;

        while (siren != null && t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.1f, travelTime);

            // Ease out: fast on the road, slowing down as it arrives.
            float eased = 1f - (1f - t) * (1f - t);
            siren.transform.position = Vector3.Lerp(from, to, eased);
            yield return null;
        }
    }

    private void OnAmbulanceArrived()
    {
        StartCoroutine(ArrivalSequence());
    }

    private IEnumerator ArrivalSequence()
    {
        Vector3 stop = ambulanceStopPoint != null ? ambulanceStopPoint.position : transform.position;
        Vector3 door = doorPoint != null ? doorPoint.position : stop;

        PlayAt(ambulanceBrake, stop, ambulanceVolume, 0f);

        // Fade the siren out
        if (siren != null)
        {
            float startVolume = siren.volume;
            for (float f = 0f; f < 1f && siren != null; f += Time.deltaTime / 2f)
            {
                siren.volume = Mathf.Lerp(startVolume, 0f, f);
                yield return null;
            }

            if (siren != null)
                Destroy(siren.gameObject);
        }

        PlayAt(ambulanceDoor, stop, ambulanceVolume, 0f);
        yield return new WaitForSeconds(1f);
        PlayAt(footsteps, door, ambulanceVolume, 0f);
    }

    // ================= Helpers =================

    private void PlayAt(AudioClip clip, Vector3 position, float volume, float pitchRange)
    {
        if (clip == null)
            return;

        AudioSource source = CreateSource(clip.name, position, clip, volume, pitchRange);
        source.Play();
        Destroy(source.gameObject, clip.length / Mathf.Max(0.1f, source.pitch) + 0.1f);
    }

    private AudioSource CreateSource(string name, Vector3 position, AudioClip clip,
                                     float volume, float pitchRange)
    {
        var go = new GameObject("SFX_" + name);
        go.transform.SetParent(transform, true);
        go.transform.position = position;

        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = 1f + Random.Range(-pitchRange, pitchRange);
        source.playOnAwake = false;
        source.spatialBlend = 1f;      // fully 3D
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
        source.reverbZoneMix = 1f;
        return source;
    }
}
