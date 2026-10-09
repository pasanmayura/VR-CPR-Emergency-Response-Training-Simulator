// SetupVRComfort.cs
// Place this file in Assets/_Project/Scripts/Editor/
// Open a VR scene (e.g. CPR_VR), then use: Tools > Pavithra > Setup VR Comfort
//
// What it does (only in the scene that is open):
// 1. Sets the XR Origin's Tracking Origin Mode to Floor (standing design, real head height,
//    so kneeling beside the patient works).
// 2. Adds the XRI "TunnelingVignette" sample prefab as a child of the XR camera
//    (or reuses it if it is already there).
// 3. Connects every locomotion provider on the XR Origin (move, snap turn, teleport, ...)
//    to the vignette, so the view narrows only while the player is moving or turning.
// Nothing outside the open scene is changed (no prefab assets, no ProjectSettings).
// Everything can be undone with Ctrl+Z.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

public static class SetupVRComfort
{
    const string VignetteName = "TunnelingVignette";

    [MenuItem("Tools/Pavithra/Setup VR Comfort")]
    public static void Setup()
    {
        // ---------- Find the XR Origin ----------
        XROrigin origin = Object.FindObjectOfType<XROrigin>();
        if (origin == null)
        {
            EditorUtility.DisplayDialog("XR Origin not found",
                "Could not find an XR Origin in this scene. Make sure the 'Complete XR Origin Set Up' " +
                "is in this scene and is switched on.", "OK");
            return;
        }

        Camera xrCamera = origin.Camera;
        if (xrCamera == null)
        {
            EditorUtility.DisplayDialog("XR camera not found",
                "The XR Origin has no Camera assigned.", "OK");
            return;
        }

        // ---------- 1. Tracking Origin Mode = Floor ----------
        var originSO = new SerializedObject(origin);
        var mode = originSO.FindProperty("m_RequestedTrackingOriginMode");
        string oldMode = mode.enumDisplayNames[mode.enumValueIndex];
        mode.enumValueIndex = (int)XROrigin.TrackingOriginMode.Floor;
        originSO.ApplyModifiedProperties(); // records Undo by itself

        // ---------- 2. Add (or reuse) the Tunneling Vignette under the camera ----------
        bool created = false;
        TunnelingVignetteController vignette = xrCamera.GetComponentInChildren<TunnelingVignetteController>(true);
        if (vignette == null)
        {
            GameObject prefab = FindVignettePrefab();
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Tunneling Vignette not found",
                    "Could not find the 'TunnelingVignette' prefab. Import it from " +
                    "Package Manager > XR Interaction Toolkit > Samples > Tunneling Vignette.", "OK");
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, xrCamera.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(go, "Add Tunneling Vignette");
            go.transform.SetParent(xrCamera.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            vignette = go.GetComponent<TunnelingVignetteController>();
            created = true;
        }

        // ---------- 3. Connect the locomotion providers to the vignette ----------
        var providers = new List<LocomotionVignetteProvider>();
        var names = new List<string>();
        foreach (var lp in origin.GetComponentsInChildren<LocomotionProvider>(true))
        {
            providers.Add(new LocomotionVignetteProvider { locomotionProvider = lp, enabled = true });
            names.Add(lp.GetType().Name + " (on " + lp.gameObject.name + ")");
        }

        Undo.RecordObject(vignette, "Connect Tunneling Vignette");
        vignette.locomotionVignetteProviders = providers;
        PrefabUtility.RecordPrefabInstancePropertyModifications(vignette);

        EditorSceneManager.MarkSceneDirty(origin.gameObject.scene);

        string report =
            "XR Origin: " + origin.name + "\n" +
            "Tracking Origin Mode: " + oldMode + " -> Floor\n\n" +
            "Tunneling Vignette: " + (created ? "added" : "already there, reused") +
            " under " + xrCamera.name + "\n\n" +
            "Connected locomotion providers (" + names.Count + "):\n- " + string.Join("\n- ", names) +
            "\n\nRemember to save the scene (Ctrl+S).";

        Debug.Log("Setup VR Comfort:\n" + report);
        EditorUtility.DisplayDialog("VR comfort set up", report, "OK");
    }

    static GameObject FindVignettePrefab()
    {
        foreach (string guid in AssetDatabase.FindAssets(VignetteName + " t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == VignetteName)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }
}
