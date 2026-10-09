// ConnectCPRInputActions.cs
// Place this file in Assets/_Project/Scripts/Editor/
// Open a VR scene (e.g. CPR_VR), then use: Tools > Pavithra > Connect CPR Input Actions
//
// What it does (only in the scene that is open):
// Every CPR script that has an input action field (talkAction, compressAction, ...)
// is pointed at the matching action in Assets/_Project/Input/CPRInputActions.
// Those actions are bound to XR controller buttons with a keyboard fallback,
// so the old hard-coded keys (T, B, N, Space) are no longer used in this scene.
// Scenes where this tool is not run keep the old keys.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;

public static class ConnectCPRInputActions
{
    const string AssetPath = "Assets/_Project/Input/CPRInputActions.inputactions";

    // script field name -> action name in the "CPR" action map
    static readonly Dictionary<string, string> FieldToAction = new Dictionary<string, string>
    {
        { "startTrainingAction", "StartTraining" },
        { "talkAction", "Talk" },
        { "beginBreathingCheckAction", "BeginBreathingCheck" },
        { "noNormalBreathingAction", "NoNormalBreathing" },
        { "compressAction", "Compress" },
    };

    [MenuItem("Tools/Pavithra/Connect CPR Input Actions")]
    public static void Connect()
    {
        // ---------- Load the action references from the asset ----------
        var references = new Dictionary<string, InputActionReference>();
        foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(AssetPath))
        {
            var reference = sub as InputActionReference;
            if (reference != null && reference.action != null)
                references[reference.action.name] = reference;
        }

        if (references.Count == 0)
        {
            EditorUtility.DisplayDialog("Input actions not found",
                "Could not load the actions from " + AssetPath + ".", "OK");
            return;
        }

        // ---------- Assign them to every CPR script in the open scene ----------
        var changed = new List<string>();
        var missing = new List<string>();
        foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>(true))
        {
            if (mb == null) continue;

            var so = new SerializedObject(mb);
            bool touched = false;

            foreach (var pair in FieldToAction)
            {
                var prop = so.FindProperty(pair.Key);
                if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                InputActionReference reference;
                if (!references.TryGetValue(pair.Value, out reference))
                {
                    missing.Add(pair.Value);
                    continue;
                }

                prop.objectReferenceValue = reference;
                touched = true;
                changed.Add(mb.GetType().Name + "." + pair.Key + " -> " + pair.Value +
                            " (on " + mb.gameObject.name + ")");
            }

            if (touched)
                so.ApplyModifiedProperties(); // records Undo by itself
        }

        if (changed.Count > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        string report =
            "Connected fields (" + changed.Count + "):\n- " + string.Join("\n- ", changed) +
            (missing.Count > 0 ? "\n\nMissing actions: " + string.Join(", ", missing) : "") +
            "\n\nRemember to save the scene (Ctrl+S).";

        Debug.Log("Connect CPR Input Actions:\n" + report);
        EditorUtility.DisplayDialog("CPR input actions connected", report, "OK");
    }
}
