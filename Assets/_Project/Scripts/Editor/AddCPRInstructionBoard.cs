// AddCPRInstructionBoard.cs
// Place this file in Assets/_Project/Scripts/Editor/
// Open a VR scene (e.g. CPR_VR), then use: Tools > Pavithra > Add CPR Instruction Board
//
// What it does (only in the scene that is open):
// 1. Finds the training room whiteboard ("Whiteboard/Board") and hides the whiteboard
//    in this scene (the prefab itself is not changed).
// 2. Puts a wall-mounted LED screen in its place: a black bezel and a world-space Canvas
//    with a dark screen, a green header bar and a TextMeshPro instruction text.
// 3. Adds Enuka's CPRInstructionDisplay to it, so CPRInteraction.ShowInstruction(...)
//    writes every CPR step's instructions onto the screen instead of a flat OnGUI box.
// If the screen already exists it is removed and created again. Ctrl+Z undoes everything.

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class AddCPRInstructionBoard
{
    const string ScreenName = "CPRInstructionScreen";
    const string OldBoardName = "CPRInstructionBoard"; // first version of this tool
    const string MatFolder = "Assets/Art/Environment/Materials";
    const float PixelsPerMetre = 1000f; // canvas scale 0.001 -> 1 unit = 1 mm
    const float Bezel = 0.04f;          // black border around the screen, metres
    const float HeaderHeight = 120f;    // canvas units (mm)

    [MenuItem("Tools/Pavithra/Add CPR Instruction Board")]
    public static void Add()
    {
        // ---------- Find the whiteboard ----------
        Transform board = null;
        foreach (var t in Object.FindObjectsOfType<Transform>(true))
        {
            if (t.name == "Board" && t.parent != null && t.parent.name == "Whiteboard")
            {
                board = t;
                break;
            }
        }

        if (board == null)
        {
            EditorUtility.DisplayDialog("Whiteboard not found",
                "Could not find 'Whiteboard/Board' in this scene. Make sure the " +
                "TrainingRoomEnvironment is in the scene and switched on.", "OK");
            return;
        }

        // ---------- Remove an older screen (so the tool can be run again) ----------
        foreach (string name in new[] { ScreenName, OldBoardName })
        {
            var old = GameObject.Find(name);
            if (old != null)
                Undo.DestroyObjectImmediate(old);
        }

        // ---------- Hide the whiteboard in this scene ----------
        GameObject whiteboard = board.parent.gameObject;
        Undo.RecordObject(whiteboard, "Hide Whiteboard");
        whiteboard.SetActive(false);

        Vector2 size = new Vector2(board.lossyScale.x, board.lossyScale.y); // metres

        // ---------- Screen root, placed where the board was ----------
        var root = new GameObject(ScreenName);
        Undo.RegisterCreatedObjectUndo(root, "Add CPR Instruction Screen");
        root.transform.SetPositionAndRotation(board.position, board.rotation);

        // ---------- Black bezel (back towards the wall, +Z) ----------
        var bezel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bezel.name = "Bezel";
        bezel.transform.SetParent(root.transform, false);
        bezel.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        bezel.transform.localScale = new Vector3(size.x + 2f * Bezel, size.y + 2f * Bezel, 0.04f);
        bezel.GetComponent<Renderer>().sharedMaterial =
            GetMat("M_ScreenBezel", new Color(0.03f, 0.03f, 0.035f), 0.85f);

        // ---------- World-space canvas just in front of the bezel ----------
        var canvasGO = new GameObject("Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(TrackedDeviceGraphicRaycaster));
        canvasGO.layer = LayerMask.NameToLayer("UI");
        canvasGO.transform.SetParent(root.transform, false);

        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        var rect = canvasGO.GetComponent<RectTransform>();
        rect.localPosition = new Vector3(0f, 0f, -0.012f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one / PixelsPerMetre;
        rect.sizeDelta = size * PixelsPerMetre;

        // ---------- Dark screen ----------
        var screen = CreateUI("Screen", canvasGO.transform, typeof(Image));
        Stretch(screen, 0f, 0f, 0f, 0f);
        screen.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 1f);

        // ---------- Green header bar ----------
        var header = CreateUI("Header", screen, typeof(Image));
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.offsetMin = new Vector2(0f, -HeaderHeight);
        header.offsetMax = Vector2.zero;
        header.GetComponent<Image>().color = new Color(0.0f, 0.55f, 0.29f, 1f);

        var headerText = CreateUI("HeaderText", header, typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        Stretch(headerText.rectTransform, 40f, 40f, 0f, 0f);
        headerText.text = "CPR TRAINING  |  FOLLOW THESE STEPS";
        headerText.fontSize = 56f;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = Color.white;
        headerText.alignment = TextAlignmentOptions.MidlineLeft;
        headerText.raycastTarget = false;

        // ---------- Instruction text (written by CPRInteraction) ----------
        var text = CreateUI("InstructionText", screen, typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        Stretch(text.rectTransform, 50f, 50f, 40f, HeaderHeight + 40f);
        text.text = "APPROACH THE CASUALTY\n\nWalk to the casualty to begin.";
        text.enableAutoSizing = true;
        text.fontSizeMin = 36f;
        text.fontSizeMax = 64f;
        text.color = new Color(0.85f, 0.95f, 1f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        // ---------- Enuka's display script ----------
        var display = canvasGO.AddComponent<CPRInstructionDisplay>();
        display.instructionText = text;

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;

        string report =
            "Created '" + ScreenName + "' in place of " + GetPath(board.parent) + "\n" +
            "Screen size: " + size.x.ToString("0.00") + " m x " + size.y.ToString("0.00") + " m\n" +
            "The whiteboard is hidden in this scene only.\n\n" +
            "Remember to save the scene (Ctrl+S).";

        Debug.Log("Add CPR Instruction Board:\n" + report);
        EditorUtility.DisplayDialog("CPR instruction screen added", report, "OK");
    }

    static RectTransform CreateUI(string name, Transform parent, System.Type component)
    {
        var go = new GameObject(name, typeof(RectTransform), component);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt, float left, float right, float bottom, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static Material GetMat(string name, Color color, float smoothness)
    {
        string path = MatFolder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
