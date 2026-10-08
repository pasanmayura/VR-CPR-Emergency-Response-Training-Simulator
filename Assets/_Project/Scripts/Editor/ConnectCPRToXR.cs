// ConnectCPRToXR.cs
// Place this file in Assets/_Project/Scripts/Editor/
// Open the CPR_VR scene, then use: Tools > Pavithra > Connect CPR to XR Controllers
//
// What it does (only in the scene that is open):
// 1. Finds the left and right XR controllers of the XR Origin.
// 2. Adds a "PalmContact" point to the front of each controller.
// 3. Every CPR script that has leftPalmContact / rightPalmContact fields
//    (ResponsivenessCheck, EmergencyAssistance, AirwayCheck, CPRHandPlacement,
//    CompressionSystem) is pointed at those controller points instead of the old desktop hands.
// 4. Every script with a playerCamera field is pointed at the XR camera.
// The old desktop HandController is skipped and left unchanged.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public static class ConnectCPRToXR
{
    const string ContactName = "PalmContact";
    static readonly Vector3 ContactOffset = new Vector3(0f, 0f, 0.06f); // a little in front of the controller

    [MenuItem("Tools/Pavithra/Connect CPR to XR Controllers")]
    public static void Connect()
    {
        // ---------- Find the XR controllers ----------
        Transform left = null, right = null;
        foreach (var c in Object.FindObjectsOfType<ActionBasedController>())
        {
            string n = c.gameObject.name.ToLower();
            if (left == null && n.Contains("left")) left = c.transform;
            else if (right == null && n.Contains("right")) right = c.transform;
        }

        if (left == null || right == null)
        {
            EditorUtility.DisplayDialog("Controllers not found",
                "Could not find both XR controllers. Make sure the 'Complete XR Origin Set Up' " +
                "is in this scene and is switched on.", "OK");
            return;
        }

        // ---------- Find the XR camera (active camera tagged MainCamera) ----------
        Camera xrCamera = null;
        foreach (var cam in Object.FindObjectsOfType<Camera>())
        {
            if (cam.CompareTag("MainCamera") && cam.transform.IsChildOf(left.root))
            {
                xrCamera = cam;
                break;
            }
        }

        // ---------- Create the palm contact points ----------
        Transform leftContact = GetOrCreateContact(left);
        Transform rightContact = GetOrCreateContact(right);

        // ---------- Point the CPR scripts at the controllers ----------
        var changed = new List<string>();
        foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            if (mb.GetType().Name == "HandController") continue; // old desktop hands, leave as is

            var so = new SerializedObject(mb);
            bool touched = false;

            var l = so.FindProperty("leftPalmContact");
            var r = so.FindProperty("rightPalmContact");
            if (l != null && r != null &&
                l.propertyType == SerializedPropertyType.ObjectReference &&
                r.propertyType == SerializedPropertyType.ObjectReference)
            {
                l.objectReferenceValue = leftContact;
                r.objectReferenceValue = rightContact;
                touched = true;
            }

            var pc = so.FindProperty("playerCamera");
            if (pc != null && xrCamera != null &&
                pc.propertyType == SerializedPropertyType.ObjectReference)
            {
                pc.objectReferenceValue = xrCamera.transform;
                touched = true;
            }

            if (touched)
            {
                Undo.RecordObject(mb, "Connect CPR to XR");
                so.ApplyModifiedProperties();
                changed.Add(mb.GetType().Name + " (on " + mb.gameObject.name + ")");
            }
        }

        EditorSceneManager.MarkSceneDirty(left.gameObject.scene);

        string report =
            "Left controller: " + left.name + "\n" +
            "Right controller: " + right.name + "\n" +
            "XR camera: " + (xrCamera != null ? xrCamera.name : "NOT FOUND") + "\n\n" +
            "Updated scripts (" + changed.Count + "):\n- " + string.Join("\n- ", changed);

        Debug.Log("Connect CPR to XR:\n" + report);
        EditorUtility.DisplayDialog("CPR connected to XR controllers", report, "OK");
    }

    static Transform GetOrCreateContact(Transform controller)
    {
        var existing = controller.Find(ContactName);
        if (existing != null) return existing;

        var go = new GameObject(ContactName);
        Undo.RegisterCreatedObjectUndo(go, "Create PalmContact");
        go.transform.SetParent(controller, false);
        go.transform.localPosition = ContactOffset;
        go.transform.localRotation = Quaternion.identity;
        return go.transform;
    }
}
