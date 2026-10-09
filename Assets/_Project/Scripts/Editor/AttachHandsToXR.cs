// AttachHandsToXR.cs
// Place this file in Assets/_Project/Scripts/Editor/
// Open a VR scene (e.g. CPR_VR), then use: Tools > Pavithra > Attach Hands to XR Controllers
//
// What it does (only in the scene that is open):
// 1. Takes the hand bones and palm contacts from Enuka's desktop HandController
//    (J_Left / J_Right, LeftPalmContact / RightPalmContact).
// 2. Moves the "Free Pack - VR Hands" model out of the old desktop camera, switches it on and
//    adds XRHandFollower, so each hand bone follows its XR controller (fingers forward,
//    palm down; tuned rotation offsets are kept when the tool is run again). It also gives the
//    hands a Standard material (the PolyOne one is URP) and scales them to real hand size.
// 3. Hides the XR controller models, so only the hands are visible.
// 4. Points every CPR script's leftPalmContact / rightPalmContact at the hand palms.
// 5. Switches off the desktop HandController in this scene only (its keys 1, 2, R, Shift, Esc
//    clash with the XR Device Simulator). The script itself is not changed.
// Everything can be undone with Ctrl+Z.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public static class AttachHandsToXR
{
    [MenuItem("Tools/Pavithra/Attach Hands to XR Controllers")]
    public static void Attach()
    {
        // ---------- Desktop HandController: source of bones and palm contacts ----------
        var handController = Object.FindObjectOfType<HandController>(true);
        if (handController == null ||
            handController.leftHand == null || handController.rightHand == null ||
            handController.leftPalmContact == null || handController.rightPalmContact == null)
        {
            EditorUtility.DisplayDialog("Hands not found",
                "Could not find a HandController with both hand bones and palm contacts " +
                "assigned in this scene.", "OK");
            return;
        }

        Transform leftBone = handController.leftHand;
        Transform rightBone = handController.rightHand;
        Transform leftPalm = handController.leftPalmContact;
        Transform rightPalm = handController.rightPalmContact;

        // The hands prefab root ("Free Pack - VR Hands ( Rigged )")
        GameObject handsPrefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(leftBone.gameObject);
        Transform handsModel = handsPrefabRoot != null ? handsPrefabRoot.transform : leftBone.root;

        // ---------- XR controllers ----------
        Transform leftController = null, rightController = null;
        var controllers = Object.FindObjectsOfType<ActionBasedController>();
        foreach (var c in controllers)
        {
            string n = c.gameObject.name.ToLower();
            if (leftController == null && n.Contains("left")) leftController = c.transform;
            else if (rightController == null && n.Contains("right")) rightController = c.transform;
        }

        if (leftController == null || rightController == null)
        {
            EditorUtility.DisplayDialog("Controllers not found",
                "Could not find both XR controllers. Make sure the 'Complete XR Origin Set Up' " +
                "is in this scene and is switched on.", "OK");
            return;
        }

        // ---------- Rotation offsets: keep tuned values on a later run ----------
        // (0,0,0 = fingers forward, palm down; XRHandFollower measures the hand itself)
        var follower = handsModel.GetComponent<XRHandFollower>();
        Vector3 leftRotation = follower != null ? follower.left.rotationOffset : Vector3.zero;
        Vector3 rightRotation = follower != null ? follower.right.rotationOffset : Vector3.zero;

        // ---------- Move the hands model to the scene root ----------
        if (handsModel.parent != null)
            Undo.SetTransformParent(handsModel, null, "Move hands to scene root");

        Undo.RecordObject(handsModel.gameObject, "Enable hands");
        handsModel.gameObject.SetActive(true);

        // The PolyOne material uses a URP shader (pink in this Built-in project),
        // so the hands get our own Standard material with the same texture.
        Material handMaterial = GetHandMaterial();
        foreach (var smr in handsModel.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Undo.RecordObject(smr, "Set up hand renderer");
            smr.updateWhenOffscreen = true; // bones move far from the original bounds

            var materials = smr.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                materials[i] = handMaterial;
            smr.sharedMaterials = materials;
        }

        // ---------- Real-world hand size ----------
        string sizeReport = ScaleToRealHand(handsModel, leftBone);

        // ---------- XRHandFollower ----------
        if (follower == null)
            follower = Undo.AddComponent<XRHandFollower>(handsModel.gameObject);

        Undo.RecordObject(follower, "Set up XRHandFollower");
        Setup(follower.left, leftController, leftBone, leftPalm, leftRotation);
        Setup(follower.right, rightController, rightBone, rightPalm, rightRotation);
        PrefabUtility.RecordPrefabInstancePropertyModifications(follower);

        // ---------- Hide the controller models ----------
        // (XRI 2.3.2 does not save "hideControllerModel", so the model prefab is cleared instead;
        //  then no controller model is created at runtime.)
        foreach (var c in controllers)
        {
            var so = new SerializedObject(c);
            so.FindProperty("m_ModelPrefab").objectReferenceValue = null;
            so.ApplyModifiedProperties();

            if (c.model != null)
            {
                Undo.RecordObject(c.model.gameObject, "Hide controller model");
                c.model.gameObject.SetActive(false);
            }
        }

        // ---------- Point the CPR scripts at the hand palms ----------
        var changed = new List<string>();
        foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>(true))
        {
            if (mb == null || mb is HandController) continue;

            var so = new SerializedObject(mb);
            var l = so.FindProperty("leftPalmContact");
            var r = so.FindProperty("rightPalmContact");
            if (l == null || r == null ||
                l.propertyType != SerializedPropertyType.ObjectReference ||
                r.propertyType != SerializedPropertyType.ObjectReference)
                continue;

            l.objectReferenceValue = leftPalm;
            r.objectReferenceValue = rightPalm;
            so.ApplyModifiedProperties();
            changed.Add(mb.GetType().Name + " (on " + mb.gameObject.name + ")");
        }

        // ---------- Switch off the desktop HandController in this scene ----------
        Undo.RecordObject(handController, "Disable desktop HandController");
        handController.enabled = false;

        EditorSceneManager.MarkSceneDirty(handsModel.gameObject.scene);
        Selection.activeGameObject = handsModel.gameObject;

        string report =
            "Hands: " + handsModel.name + " (moved to scene root, switched on)\n" +
            "Left: " + leftBone.name + " -> " + leftController.name + "\n" +
            "Right: " + rightBone.name + " -> " + rightController.name + "\n" +
            "Controller models hidden: " + controllers.Length + "\n" +
            "Material: " + handMaterial.name + "\n" +
            sizeReport + "\n" +
            "Desktop HandController switched off (on " + handController.gameObject.name + ")\n\n" +
            "Palm contacts updated (" + changed.Count + "):\n- " + string.Join("\n- ", changed) +
            "\n\nRemember to save the scene (Ctrl+S).";

        Debug.Log("Attach Hands to XR Controllers:\n" + report);
        EditorUtility.DisplayDialog("Hands attached to XR controllers", report, "OK");
    }

    // Resets both rotation offsets to 0,0,0 = fingers forward, palm down.
    // (XRHandFollower measures the hand from its finger bones while playing.)
    [MenuItem("Tools/Pavithra/Reset Hand Rotation (palms down, fingers forward)")]
    public static void ResetRotation()
    {
        var follower = Object.FindObjectOfType<XRHandFollower>(true);
        if (follower == null)
        {
            EditorUtility.DisplayDialog("Hands not attached",
                "Run Tools > Pavithra > Attach Hands to XR Controllers first.", "OK");
            return;
        }

        Undo.RecordObject(follower, "Reset hand rotation");
        follower.left.rotationOffset = Vector3.zero;
        follower.right.rotationOffset = Vector3.zero;
        PrefabUtility.RecordPrefabInstancePropertyModifications(follower);
        EditorSceneManager.MarkSceneDirty(follower.gameObject.scene);

        Debug.Log("Reset Hand Rotation: both rotation offsets set to (0, 0, 0).");
        EditorUtility.DisplayDialog("Hand rotation reset",
            "Both rotation offsets set to (0, 0, 0).\n\nRemember to save the scene (Ctrl+S).", "OK");
    }

    const string HandMaterialPath = "Assets/_Project/Materials/M_VRHands.mat";
    const string HandTexturePath = "Assets/PolyOne/Free VR Hands/Texture/T_VRHands_Base_color.png";
    const float RealHandLength = 0.19f; // wrist to middle fingertip, metres

    static Material GetHandMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(HandMaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(HandTexturePath);
            mat.SetFloat("_Glossiness", 0.3f);
            AssetDatabase.CreateAsset(mat, HandMaterialPath);
        }
        return mat;
    }

    // Scales the hands model so the left hand (wrist -> middle fingertip) is RealHandLength long.
    static string ScaleToRealHand(Transform handsModel, Transform leftBone)
    {
        Transform wrist = null, tip = null;
        foreach (var t in leftBone.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "J_Left_Hand") wrist = t;
            else if (t.name == "J_Left_HandMiddle4") tip = t;
        }

        if (wrist == null || tip == null)
            return "Size: not changed (finger bones not found)";

        float length = Vector3.Distance(wrist.position, tip.position);
        if (length < 0.001f)
            return "Size: not changed";

        float factor = RealHandLength / length;
        if (Mathf.Abs(factor - 1f) < 0.02f)
            return "Size: already real size (" + length.ToString("0.000") + " m)";

        Undo.RecordObject(handsModel, "Scale hands to real size");
        handsModel.localScale *= factor;
        return "Size: hand was " + length.ToString("0.000") + " m long, scaled x" + factor.ToString("0.00") +
               " to " + RealHandLength + " m";
    }

    static void Setup(XRHandFollower.Hand hand, Transform controller, Transform bone,
                      Transform palm, Vector3 rotation)
    {
        hand.controller = controller;
        hand.bone = bone;
        hand.palmContact = palm;
        hand.rotationOffset = rotation;
    }
}
