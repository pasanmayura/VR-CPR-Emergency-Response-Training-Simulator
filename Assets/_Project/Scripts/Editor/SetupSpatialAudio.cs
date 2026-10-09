// SetupSpatialAudio.cs
// Place this file in Assets/_Project/Scripts/Editor/
// Open the scene, then use: Tools > Pavithra > Setup Spatial Audio
//
// What it does (only in the scene that is open):
// 1. Creates a "SpatialAudio" object (an older one is removed first, so the tool can be re-run).
// 2. Adds an Audio Reverb Zone ("Room" preset) covering the training room.
// 3. Adds looping 3D ambience where it really comes from: room hum (ceiling), traffic outside
//    the window (muffled with a low-pass filter), wall clock ticking and a ceiling light buzz,
//    plus a dog barking behind the room (OutdoorAmbientSound: muffled while you are inside).
// 4. Adds CPRSoundEffects with all its clips, plus the ambulance siren start / stop points
//    outside the door.
// Sound files are found by name in Assets/_Project/Audio/ (see SOUND_LIST.txt there).
// Missing files are listed in the report and are simply skipped while playing.
// Nothing outside the open scene is changed. Ctrl+Z undoes everything.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SetupSpatialAudio
{
    const string AudioFolder = "Assets/_Project/Audio";
    const string RootName = "SpatialAudio";

    static readonly List<string> found = new List<string>();
    static readonly List<string> missing = new List<string>();

    [MenuItem("Tools/Pavithra/Setup Spatial Audio")]
    public static void Setup()
    {
        found.Clear();
        missing.Clear();

        // ---------- Find the training room ----------
        Transform floor = FindByName("Floor");
        if (floor == null)
        {
            EditorUtility.DisplayDialog("Training room not found",
                "Could not find the room 'Floor'. Make sure the TrainingRoomEnvironment " +
                "is in this scene and switched on.", "OK");
            return;
        }

        Bounds room = BoundsOf(floor);
        Vector3 centre = new Vector3(room.center.x, room.max.y + 1.5f, room.center.z);
        float ceilingY = room.max.y + 2.8f;

        Transform ceiling = FindByName("Ceiling");
        if (ceiling != null)
            ceilingY = BoundsOf(ceiling).center.y;

        // ---------- Fresh root ----------
        var old = GameObject.Find(RootName);
        if (old != null)
            Undo.DestroyObjectImmediate(old);

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Setup Spatial Audio");

        // ---------- Reverb zone ----------
        var reverbGO = Child(root, "RoomReverb", centre);
        var reverb = reverbGO.AddComponent<AudioReverbZone>();
        reverb.reverbPreset = AudioReverbPreset.Room;
        float halfSize = Mathf.Max(room.extents.x, room.extents.z);
        reverb.minDistance = halfSize;          // full reverb inside the room
        reverb.maxDistance = halfSize + 2f;     // fades out just outside it

        // ---------- Ambience loops ----------
        Vector3 ceilingCentre = new Vector3(centre.x, ceilingY - 0.05f, centre.z);
        Ambience(root, "Amb_RoomHum", ceilingCentre, "ambience_room_hum", 0.25f, 1f, 12f);

        Transform window = FindByName("Window");
        if (window != null)
        {
            Vector3 windowPos = BoundsOf(window).center;
            Vector3 outward = Flat(windowPos - centre).normalized;
            var traffic = Ambience(root, "Amb_TrafficOutside", windowPos + outward * 0.5f,
                "ambience_traffic_outside", 0.35f, 1f, 15f);
            if (traffic != null)
            {
                var lowPass = traffic.gameObject.AddComponent<AudioLowPassFilter>();
                lowPass.cutoffFrequency = 1500f; // heard through the glass
            }
        }

        Transform clock = FindByName("WallClock");
        if (clock != null)
            Ambience(root, "Amb_WallClock", BoundsOf(clock).center, "ambience_wall_clock", 0.3f, 0.3f, 6f);

        Transform light = FindByName("CeilingLight_1");
        if (light != null)
            Ambience(root, "Amb_LightBuzz", BoundsOf(light).center, "ambience_light_buzz", 0.12f, 0.3f, 4f);

        // ---------- Ambulance points (outside the door) ----------
        Transform door = FindByName("DoorFrame");
        Vector3 doorPos = door != null ? BoundsOf(door).center : centre + Vector3.back * room.extents.z;
        doorPos.y = room.max.y + 1f;
        Vector3 outDoor = Flat(doorPos - centre).normalized;
        if (outDoor == Vector3.zero) outDoor = Vector3.back;
        Vector3 side = Vector3.Cross(Vector3.up, outDoor);

        // ---------- Dog barking behind the room (opposite the door) ----------
        Bounds inside = new Bounds(
            new Vector3(room.center.x, (room.max.y + ceilingY) * 0.5f, room.center.z),
            new Vector3(room.size.x, Mathf.Max(0.5f, ceilingY - room.max.y), room.size.z));
        float wallDistance = Mathf.Abs(Vector3.Dot(room.extents, outDoor));
        Vector3 behind = centre - outDoor * (wallDistance + 8f);
        behind.y = room.max.y + 0.4f; // dog height
        Outdoor(root, "Amb_DogBarking", behind, "ambience_dog_barking", 0.8f, 2f, 40f,
            OutdoorAmbientSound.PlayMode.RandomRepeats, inside);

        // ---------- Wind in the trees and birds (loops) ----------
        // The street side is the window side (falls back to the door side).
        Vector3 street = outDoor;
        if (window != null)
        {
            Vector3 w = Flat(BoundsOf(window).center - centre).normalized;
            if (w != Vector3.zero) street = w;
        }
        Vector3 streetSide = Vector3.Cross(Vector3.up, street);
        float streetWall = Mathf.Abs(Vector3.Dot(room.extents, street));
        float treeY = room.max.y + 4f;

        // Wind from both sides, so it surrounds the room
        Outdoor(root, "Amb_WindLeaves_Street", centre + street * (streetWall + 6f) + Vector3.up * 2f,
            "ambience_wind_leaves", 0.35f, 6f, 40f, OutdoorAmbientSound.PlayMode.Loop, inside);
        Outdoor(root, "Amb_WindLeaves_Back", centre - street * (streetWall + 6f) + Vector3.up * 2f,
            "ambience_wind_leaves", 0.35f, 6f, 40f, OutdoorAmbientSound.PlayMode.Loop, inside);

        // Birds in a tree outside the window
        Vector3 tree = centre + street * (streetWall + 5f) + streetSide * 3f;
        tree.y = treeY;
        Outdoor(root, "Amb_Birds", tree, "ambience_birds", 0.5f, 3f, 35f,
            OutdoorAmbientSound.PlayMode.Loop, inside);

        // ---------- Three-wheeler driving past on the street ----------
        Vector3 road = centre + street * (streetWall + 15f);
        road.y = room.max.y + 0.8f;
        var tuk = Outdoor(root, "Vehicle_ThreeWheeler", road - streetSide * 80f,
            "vehicle_three_wheeler_engine", 0.9f, 3f, 80f,
            OutdoorAmbientSound.PlayMode.External, inside);
        if (tuk != null)
        {
            tuk.dopplerLevel = 1f; // pitch rises as it comes closer, drops as it drives away
            var vehicle = tuk.gameObject.AddComponent<PassingVehicleSound>();
            vehicle.roadStart = road - streetSide * 80f;
            vehicle.roadEnd = road + streetSide * 80f;
        }

        Transform doorPoint = Child(root, "DoorPoint", doorPos).transform;
        Transform stopPoint = Child(root, "AmbulanceStopPoint", doorPos + outDoor * 12f).transform;
        Transform sirenStart = Child(root, "SirenStartPoint", doorPos + outDoor * 25f + side * 150f).transform;

        // ---------- Player footsteps ----------
        AudioClip[] steps = Clips("footstep_floor_01", "footstep_floor_02",
                                  "footstep_floor_03", "footstep_floor_04");
        if (steps.Length > 0)
            Child(root, "PlayerFootsteps", centre).AddComponent<PlayerFootsteps>().footsteps = steps;

        // ---------- Event sound effects ----------
        var sfx = Child(root, "SoundEffects", centre).AddComponent<CPRSoundEffects>();
        sfx.shoulderTap = Clip("touch_shoulder_tap");
        sfx.skinTouch = Clip("touch_skin");
        sfx.compressionThuds = Clips("cpr_compression_01", "cpr_compression_02", "cpr_compression_03");
        sfx.padPeel = Clip("aed_pad_peel");
        sfx.padPlace = Clip("aed_pad_place");
        sfx.aedCharge = Clip("aed_charge");
        sfx.aedShock = Clip("aed_shock");
        sfx.sirenLoop = Clip("ambulance_siren_loop");
        sfx.ambulanceBrake = Clip("ambulance_brake");
        sfx.ambulanceDoor = Clip("ambulance_door");
        sfx.footsteps = Clip("ambulance_footsteps");
        sfx.sirenStartPoint = sirenStart;
        sfx.ambulanceStopPoint = stopPoint;
        sfx.doorPoint = doorPoint;

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;

        string report =
            "Created '" + RootName + "' with a Room reverb zone, ambience and sound effects.\n\n" +
            "Sound files found (" + found.Count + "):\n- " + (found.Count > 0 ? string.Join("\n- ", found) : "none") +
            "\n\nMissing (" + missing.Count + "):\n- " + (missing.Count > 0 ? string.Join("\n- ", missing) : "none") +
            "\n\nAdd missing files to " + AudioFolder + " and run this tool again.\n" +
            "Remember to save the scene (Ctrl+S).";

        Debug.Log("Setup Spatial Audio:\n" + report);
        EditorUtility.DisplayDialog("Spatial audio set up", report, "OK");
    }

    // ---------- Helpers ----------

    static AudioSource Ambience(GameObject root, string name, Vector3 position, string clipName,
                               float volume, float minDistance, float maxDistance)
    {
        AudioClip clip = Clip(clipName);
        if (clip == null)
            return null;

        var source = Child(root, name, position).AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = true;
        source.volume = volume;
        source.spatialBlend = 1f; // fully 3D
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
        source.reverbZoneMix = 1f;
        return source;
    }

    // A 3D sound outside the room, muffled while the listener is inside (OutdoorAmbientSound).
    static AudioSource Outdoor(GameObject root, string name, Vector3 position, string clipName,
                              float volume, float minDistance, float maxDistance,
                              OutdoorAmbientSound.PlayMode mode, Bounds inside)
    {
        AudioClip clip = Clip(clipName);
        if (clip == null)
            return null;

        var go = Child(root, name, position);
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = false;
        source.volume = volume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
        source.reverbZoneMix = 0f; // outdoors: no room reverb

        go.AddComponent<AudioLowPassFilter>();
        var outdoor = go.AddComponent<OutdoorAmbientSound>();
        outdoor.playMode = mode;
        outdoor.roomBounds = inside;
        return source;
    }

    static AudioClip Clip(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:AudioClip", new[] { AudioFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
            {
                if (!found.Contains(name)) found.Add(name);
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }

        if (!missing.Contains(name)) missing.Add(name);
        return null;
    }

    static AudioClip[] Clips(params string[] names)
    {
        var clips = new List<AudioClip>();
        foreach (string name in names)
        {
            AudioClip clip = Clip(name);
            if (clip != null)
                clips.Add(clip);
        }
        return clips.ToArray();
    }

    static GameObject Child(GameObject parent, string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = position;
        return go;
    }

    static Transform FindByName(string name)
    {
        foreach (var t in Object.FindObjectsOfType<Transform>())
        {
            if (t.name == name)
                return t;
        }
        return null;
    }

    static Bounds BoundsOf(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(t.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
