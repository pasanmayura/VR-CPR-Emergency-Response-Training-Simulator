// AddCallButtonByPhone.cs
// Place this file in Assets/_Project/Scripts/Editor/
// Open a VR scene (e.g. CPR_VR), then use: Tools > Pavithra > Add 1990 Call Button by Phone
//
// What it does (only in the scene that is open):
// 1. Finds the phone ("Phone_Placeholder") in the training room.
// 2. Creates a small world-space Canvas "CallButton1990" just above the phone, turned to face
//    the player's start position, with a green "CALL 1990" button.
//    The canvas has a TrackedDeviceGraphicRaycaster, so the XR controller ray can press it.
// 3. Assigns that button to EmergencyAssistance.worldCallButton. The script hides the button
//    at the start and shows it only after the player touches the phone.
// If the button already exists it is removed and created again. Ctrl+Z undoes everything.

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class AddCallButtonByPhone
{
    const string CanvasName = "CallButton1990";
    const float PixelsPerMetre = 1000f;               // canvas scale 0.001 -> 1 unit = 1 mm
    static readonly Vector2 ButtonSize = new Vector2(300f, 120f); // 30 cm x 12 cm
    const float HeightAbovePhone = 0.2f;              // metres

    [MenuItem("Tools/Pavithra/Add 1990 Call Button by Phone")]
    public static void Add()
    {
        // ---------- Find the phone and the EmergencyAssistance script ----------
        Collider phone = null;
        foreach (var c in Object.FindObjectsOfType<Collider>(true))
        {
            if (c.gameObject.name == "Phone_Placeholder")
            {
                phone = c;
                break;
            }
        }

        var assistance = Object.FindObjectOfType<EmergencyAssistance>(true);

        if (phone == null || assistance == null)
        {
            EditorUtility.DisplayDialog("Not found",
                "Could not find " +
                (phone == null ? "the 'Phone_Placeholder' collider" : "the EmergencyAssistance script") +
                " in this scene.", "OK");
            return;
        }

        // ---------- Remove an older button (so the tool can be run again) ----------
        var old = GameObject.Find(CanvasName);
        if (old != null)
            Undo.DestroyObjectImmediate(old);

        // ---------- Position: above the phone, facing the player's start position ----------
        Vector3 position = phone.bounds.center + Vector3.up * (phone.bounds.extents.y + HeightAbovePhone);

        Vector3 viewer = position + Vector3.back;
        var origin = Object.FindObjectOfType<XROrigin>();
        if (origin != null && origin.Camera != null)
            viewer = origin.Camera.transform.position;

        Vector3 lookDirection = position - viewer; // canvas +Z points away from the viewer
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude < 0.0001f)
            lookDirection = Vector3.forward;

        // ---------- World-space canvas ----------
        var canvasGO = new GameObject(CanvasName,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(TrackedDeviceGraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Add 1990 Call Button");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        var rect = canvasGO.GetComponent<RectTransform>();
        rect.SetPositionAndRotation(position, Quaternion.LookRotation(lookDirection.normalized, Vector3.up));
        rect.localScale = Vector3.one / PixelsPerMetre;
        rect.sizeDelta = ButtonSize;

        // ---------- Green button ----------
        var buttonGO = new GameObject("CallButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.layer = canvasGO.layer;
        buttonGO.transform.SetParent(canvasGO.transform, false);
        var buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        var image = buttonGO.GetComponent<Image>();
        image.color = Color.white; // tinted by the button colours below

        var button = buttonGO.GetComponent<Button>();
        var colours = button.colors;
        colours.normalColor = new Color(0.2f, 0.75f, 0.35f);
        colours.highlightedColor = new Color(0.35f, 0.9f, 0.5f);
        colours.pressedColor = new Color(0.1f, 0.5f, 0.2f);
        colours.selectedColor = colours.highlightedColor;
        button.colors = colours;

        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        label.gameObject.layer = canvasGO.layer;
        label.transform.SetParent(buttonGO.transform, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        label.text = "CALL 1990";
        label.fontSize = 56f;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        // ---------- Connect it to Enuka's EmergencyAssistance ----------
        var so = new SerializedObject(assistance);
        so.FindProperty("worldCallButton").objectReferenceValue = button;
        so.ApplyModifiedProperties(); // records Undo by itself

        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        Selection.activeGameObject = canvasGO;

        string report =
            "Created '" + CanvasName + "' above " + phone.name + "\n" +
            "Connected to EmergencyAssistance (on " + assistance.gameObject.name + ")\n\n" +
            "The button is hidden until the phone is touched.\n" +
            "Remember to save the scene (Ctrl+S).";

        Debug.Log("Add 1990 Call Button by Phone:\n" + report);
        EditorUtility.DisplayDialog("1990 call button added", report, "OK");
    }
}
