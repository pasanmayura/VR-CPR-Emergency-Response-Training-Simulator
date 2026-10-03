// MarbleWallPanels.cs
// Place this file in the same "Editor" folder as TrainingRoomBuilder.cs.
// Menu: Tools > Pavithra > Add Marble Wall Panels
// Adds marble slab panels to the lower half of every wall (with thin joints
// and a dark trim on top) directly inside the TrainingRoomEnvironment prefab.
// Safe to run again: it removes the old panels and rebuilds them.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public static class MarbleWallPanels
{
    const string PrefabPath = "Assets/_Project/Prefabs/TrainingRoomEnvironment.prefab";
    const string MatFolder  = "Assets/Art/Environment/Materials";

    const float Half       = 4f;     // half of the room size (room is 8 m x 8 m)
    const float DoorHalf   = 0.6f;   // half of the door opening width
    const float PanelH     = 1.5f;   // marble height (lower half of the 3 m wall)
    const float PT         = 0.02f;  // panel thickness
    const float Gap        = 0.004f; // joint between marble slabs
    const float TargetSlab = 1.2f;   // approximate width of one marble slab
    const float TrimH      = 0.04f;  // trim strip height
    const float TrimD      = 0.035f; // trim strip depth (how far it sticks out)

    [MenuItem("Tools/Pavithra/Add Marble Wall Panels")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            EditorUtility.DisplayDialog("Prefab not found",
                "Could not find " + PrefabPath + ". Build the training room first.", "OK");
            return;
        }

        var marble = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/M_Marble.mat");
        if (marble == null)
        {
            EditorUtility.DisplayDialog("M_Marble not found",
                "Create the M_Marble material in " + MatFolder + " first (with the marble textures).", "OK");
            return;
        }
        // Each slab shows the marble texture once
        SetTiling(marble, Vector2.one);

        var wallMat = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/M_Wall.mat");
        var trimMat = GetTrimMat();

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var structure = root.transform.Find("Structure");
            Transform parent = structure != null ? structure : root.transform;

            var old = parent.Find("WallPanels");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // Make sure every wall uses the plain wall material underneath
            if (wallMat != null && structure != null)
            {
                foreach (Transform child in structure)
                {
                    if (!child.name.StartsWith("Wall_")) continue;
                    var r = child.GetComponent<Renderer>();
                    if (r != null) r.sharedMaterial = wallMat;
                }
            }

            var panels = new GameObject("WallPanels").transform;
            panels.SetParent(parent, false);

            // North wall
            Run(panels, "North", new Vector3(-Half, 0, Half), new Vector3(Half, 0, Half), Vector3.back, marble, trimMat);
            // South wall (two parts, leaving the door opening free)
            Run(panels, "South_Left",  new Vector3(-Half, 0, -Half), new Vector3(-DoorHalf, 0, -Half), Vector3.forward, marble, trimMat);
            Run(panels, "South_Right", new Vector3(DoorHalf, 0, -Half), new Vector3(Half, 0, -Half), Vector3.forward, marble, trimMat);
            // East and west walls (shortened slightly so corners don't overlap)
            Run(panels, "East", new Vector3(Half, 0, -Half + PT), new Vector3(Half, 0, Half - PT), Vector3.left, marble, trimMat);
            Run(panels, "West", new Vector3(-Half, 0, -Half + PT), new Vector3(-Half, 0, Half - PT), Vector3.right, marble, trimMat);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Marble wall panels added to " + PrefabPath);
    }

    // Builds one wall's row of marble slabs plus a trim strip on top.
    // a and b are the two ends of the wall's inner face at floor level.
    static void Run(Transform parent, string name, Vector3 a, Vector3 b, Vector3 inward,
                    Material marble, Material trim)
    {
        var group = new GameObject("Panels_" + name).transform;
        group.SetParent(parent, false);

        float len = Vector3.Distance(a, b);
        Vector3 dir = (b - a).normalized;
        int n = Mathf.Max(1, Mathf.RoundToInt(len / TargetSlab));
        float w = len / n;
        bool alongX = Mathf.Abs(dir.x) > 0.5f;

        for (int i = 0; i < n; i++)
        {
            Vector3 c = a + dir * (w * (i + 0.5f)) + inward * (PT / 2f) + Vector3.up * (PanelH / 2f);
            Vector3 size = alongX ? new Vector3(w - Gap, PanelH, PT) : new Vector3(PT, PanelH, w - Gap);
            var slab = Box("Slab_" + (i + 1), group, c, size, marble);
            // Flip every second slab so the marble pattern doesn't look repeated
            if (i % 2 == 1) slab.localRotation = Quaternion.AngleAxis(180f, inward);
        }

        Vector3 tc = a + dir * (len / 2f) + inward * (TrimD / 2f) + Vector3.up * (PanelH + TrimH / 2f);
        Vector3 ts = alongX ? new Vector3(len, TrimH, TrimD) : new Vector3(TrimD, TrimH, len);
        Box("Trim", group, tc, ts, trim);
    }

    static Transform Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    static Material GetTrimMat()
    {
        string path = MatFolder + "/M_Trim.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(DefaultShader());
            var color = new Color(0.15f, 0.15f, 0.16f);
            if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor", color);  // URP
            if (mat.HasProperty("_Color"))      mat.SetColor("_Color", color);      // Built-in
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.6f);  // Built-in
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.6f);  // URP
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static void SetTiling(Material mat, Vector2 tiling)
    {
        if (mat.HasProperty("_MainTex")) mat.SetTextureScale("_MainTex", tiling); // Built-in
        if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", tiling); // URP
        EditorUtility.SetDirty(mat);
    }

    static Shader DefaultShader()
    {
        var rp = GraphicsSettings.renderPipelineAsset;
        if (rp != null && rp.defaultMaterial != null) return rp.defaultMaterial.shader;
        return Shader.Find("Standard");
    }
}
