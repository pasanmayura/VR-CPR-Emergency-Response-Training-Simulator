// WindowAndDoor.cs
// Place this file in the same "Editor" folder as the other scripts.
// Menu: Tools > Pavithra > Add Window and Door Frame
// Cuts a window into the south wall (right of the door), adds glass, an aluminium
// window frame with a sill, and a frame (casing) around the door opening.
// Works directly inside the TrainingRoomEnvironment prefab. Safe to run again.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public static class WindowAndDoor
{
    const string PrefabPath = "Assets/_Project/Prefabs/TrainingRoomEnvironment.prefab";
    const string MatFolder  = "Assets/Art/Environment/Materials";

    // Room / wall measurements (must match TrainingRoomBuilder)
    const float Half = 4f, H = 3f, T = 0.15f;
    const float DoorHalf = 0.6f, DoorH = 2.2f;

    // Window opening in the south wall (right side of the door)
    const float WinX0 = 1.9f, WinX1 = 3.5f;   // left / right edge
    const float WinY0 = 1.6f, WinY1 = 2.6f;   // bottom / top edge
    const float Bar = 0.05f;                  // frame bar thickness

    [MenuItem("Tools/Pavithra/Add Window and Door Frame")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            EditorUtility.DisplayDialog("Prefab not found", "Could not find " + PrefabPath, "OK");
            return;
        }

        var wallMat  = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/M_Wall.mat");
        if (wallMat == null) wallMat = GetMat("M_Wall", new Color(0.92f, 0.92f, 0.88f), 0.2f);
        var aluminium = GetMat("M_Aluminium", new Color(0.75f, 0.76f, 0.78f), 0.7f);
        var trimMat   = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/M_Trim.mat");
        if (trimMat == null) trimMat = GetMat("M_Trim", new Color(0.15f, 0.15f, 0.16f), 0.6f);
        var glass     = GetGlassMat();

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var structure = root.transform.Find("Structure");
            Transform parent = structure != null ? structure : root.transform;

            var old = parent.Find("WindowAndDoor");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // Remove the solid right part of the south wall (rebuilt below with a window hole)
            var solid = parent.Find("Wall_South_Right");
            if (solid != null) Object.DestroyImmediate(solid.gameObject);

            var group = new GameObject("WindowAndDoor").transform;
            group.SetParent(parent, false);

            float wallZ = -(Half + T / 2f);      // centre of the south wall
            float rightEnd = Half + T;            // outer corner of the room

            // ---------- South wall (right part) with window opening ----------
            var wall = Group("Wall_South_Right_WithWindow", group);
            Piece("Left",  wall, DoorHalf, WinX0, 0f, H, wallZ, wallMat);
            Piece("Right", wall, WinX1, rightEnd, 0f, H, wallZ, wallMat);
            Piece("Below", wall, WinX0, WinX1, 0f, WinY0, wallZ, wallMat);
            Piece("Above", wall, WinX0, WinX1, WinY1, H, wallZ, wallMat);

            // ---------- Window ----------
            var win = Group("Window", group);
            float wcx = (WinX0 + WinX1) / 2f, wcy = (WinY0 + WinY1) / 2f;
            float ww = WinX1 - WinX0, wh = WinY1 - WinY0;
            Box("Glass", win, new Vector3(wcx, wcy, wallZ), new Vector3(ww, wh, 0.01f), glass);
            Box("Frame_Top",    win, new Vector3(wcx, WinY1 - Bar / 2f, wallZ), new Vector3(ww, Bar, T), aluminium);
            Box("Frame_Bottom", win, new Vector3(wcx, WinY0 + Bar / 2f, wallZ), new Vector3(ww, Bar, T), aluminium);
            Box("Frame_Left",   win, new Vector3(WinX0 + Bar / 2f, wcy, wallZ), new Vector3(Bar, wh, T), aluminium);
            Box("Frame_Right",  win, new Vector3(WinX1 - Bar / 2f, wcy, wallZ), new Vector3(Bar, wh, T), aluminium);
            Box("Frame_Middle", win, new Vector3(wcx, wcy, wallZ), new Vector3(0.04f, wh - 2 * Bar, 0.06f), aluminium);
            Box("Sill", win, new Vector3(wcx, WinY0 - 0.015f, -Half + 0.05f), new Vector3(ww + 0.1f, 0.03f, 0.12f), trimMat);

            // ---------- Door frame (casing on both sides of the wall) ----------
            var door = Group("DoorFrame", group);
            float cw = 0.08f, cd = 0.05f;               // casing width / depth
            float casingH = DoorH + cw;
            float inZ = -Half + cd / 2f;                // inside face
            float outZ = -(Half + T) - cd / 2f;         // outside face
            foreach (var side in new[] { ("Inside", inZ), ("Outside", outZ) })
            {
                var s = Group(side.Item1, door);
                Box("Jamb_Left",  s, new Vector3(-(DoorHalf + cw / 2f), casingH / 2f, side.Item2), new Vector3(cw, casingH, cd), trimMat);
                Box("Jamb_Right", s, new Vector3(DoorHalf + cw / 2f, casingH / 2f, side.Item2),    new Vector3(cw, casingH, cd), trimMat);
                Box("Head",       s, new Vector3(0, DoorH + cw / 2f, side.Item2), new Vector3(2 * DoorHalf + 2 * cw, cw, cd), trimMat);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Window and door frame added to " + PrefabPath);
    }

    // A wall block between x0..x1 and y0..y1 on the south wall
    static void Piece(string name, Transform parent, float x0, float x1, float y0, float y1, float z, Material mat)
    {
        Box(name, parent, new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, z), new Vector3(x1 - x0, y1 - y0, T), mat);
    }

    static Transform Group(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Transform Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    static Material GetMat(string name, Color color, float smoothness)
    {
        string path = MatFolder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(DefaultShader());
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    // Transparent, slightly blue glass that works in the Built-in pipeline and URP
    static Material GetGlassMat()
    {
        string path = MatFolder + "/M_Glass.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(DefaultShader());
            AssetDatabase.CreateAsset(mat, path);
        }
        var color = new Color(0.80f, 0.90f, 1.0f, 0.15f);
        if (mat.HasProperty("_Surface"))
        {
            // URP Lit
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetColor("_BaseColor", color);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.95f);
        }
        else
        {
            // Built-in Standard, "Transparent" rendering mode
            mat.SetFloat("_Mode", 3f);
            mat.SetColor("_Color", color);
            mat.SetInt("_SrcBlend", (int)BlendMode.One);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.95f);
        }
        mat.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Shader DefaultShader()
    {
        var rp = GraphicsSettings.renderPipelineAsset;
        if (rp != null && rp.defaultMaterial != null) return rp.defaultMaterial.shader;
        return Shader.Find("Standard");
    }
}
