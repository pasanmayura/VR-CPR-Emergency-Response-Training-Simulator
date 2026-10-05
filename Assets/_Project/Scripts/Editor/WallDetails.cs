// WallDetails.cs
// Place this file in the same "Editor" folder as the other scripts.
// Put Poster_CPR.png, Poster_Emergency.png, Sign_AED.png and Sign_Name.png in Assets/Art/Environment/Textures/
// Menu: Tools > Pavithra > Add Wall Details
// Adds a name sign, a whiteboard, CPR poster, emergency numbers poster, AED sign, first aid cabinet,
// clock and fire extinguisher directly inside the TrainingRoomEnvironment prefab.
// Safe to run again: it removes the old details and rebuilds them.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public static class WallDetails
{
    const string PrefabPath = "Assets/_Project/Prefabs/TrainingRoomEnvironment.prefab";
    const string MatFolder  = "Assets/Art/Environment/Materials";
    const string TexFolder  = "Assets/Art/Environment/Textures";
    const float  Half = 4f; // inner wall faces are at x = ±4 and z = ±4

    static string missing = "";

    [MenuItem("Tools/Pavithra/Add Wall Details")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            EditorUtility.DisplayDialog("Prefab not found", "Could not find " + PrefabPath, "OK");
            return;
        }
        missing = "";

        // ---------- Materials ----------
        var white      = GetMat("M_DetailWhite", new Color(0.95f, 0.95f, 0.95f), 0.3f);
        var whiteboard = GetMat("M_Whiteboard",  new Color(0.98f, 0.98f, 0.98f), 0.9f);
        var aluminium  = GetMat("M_Aluminium",   new Color(0.75f, 0.76f, 0.78f), 0.7f);
        var black      = GetMat("M_DetailBlack", new Color(0.05f, 0.05f, 0.05f), 0.4f);
        var red        = GetMat("M_ExtinguisherRed", new Color(0.75f, 0.05f, 0.05f), 0.75f);
        var green      = GetMat("M_SignGreen",   new Color(0.0f, 0.55f, 0.29f), 0.3f);
        var cprPoster  = GetTexMat("M_Poster_CPR",       "Poster_CPR.png");
        var emgPoster  = GetTexMat("M_Poster_Emergency", "Poster_Emergency.png");
        var aedSign    = GetTexMat("M_Sign_AED",         "Sign_AED.png");
        var nameSign   = GetTexMat("M_Sign_Name",        "Sign_Name.png");

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var old = root.transform.Find("WallDetails");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var details = new GameObject("WallDetails").transform;
            details.SetParent(root.transform, false);

            // ---------- Whiteboard (north wall) ----------
            var wb = Group("Whiteboard", details);
            Box("Frame", wb, new Vector3(0.6f, 2.1f, Half - 0.015f), new Vector3(1.9f, 1.0f, 0.03f), aluminium);
            Quad("Board", wb, new Vector3(0.6f, 2.1f, Half - 0.031f), 0f, new Vector2(1.84f, 0.94f), whiteboard);
            Box("MarkerTray", wb, new Vector3(0.6f, 1.585f, Half - 0.045f), new Vector3(1.0f, 0.03f, 0.08f), aluminium);

            // ---------- Training centre name sign (north wall, above the desk) ----------
            var ns = Group("Sign_Name", details);
            Box("Backing", ns, new Vector3(-2.2f, 2.3f, Half - 0.01f), new Vector3(1.44f, 0.39f, 0.02f), white);
            Quad("Sign", ns, new Vector3(-2.2f, 2.3f, Half - 0.021f), 0f, new Vector2(1.4f, 0.35f), nameSign);

            // ---------- CPR poster (west wall) ----------
            var cpr = Group("Poster_CPR", details);
            Box("Backing", cpr, new Vector3(-Half + 0.01f, 2.05f, 1.6f), new Vector3(0.02f, 0.84f, 0.64f), white);
            Quad("Poster", cpr, new Vector3(-Half + 0.021f, 2.05f, 1.6f), -90f, new Vector2(0.6f, 0.8f), cprPoster);

            // ---------- Emergency numbers poster (west wall, above phone table) ----------
            var emg = Group("Poster_Emergency", details);
            Box("Backing", emg, new Vector3(-Half + 0.01f, 1.95f, -2.5f), new Vector3(0.02f, 0.6f, 0.46f), white);
            Quad("Poster", emg, new Vector3(-Half + 0.021f, 1.95f, -2.5f), -90f, new Vector2(0.42f, 0.56f), emgPoster);

            // ---------- First aid cabinet (west wall) ----------
            var fa = Group("FirstAidCabinet", details);
            Box("Body", fa, new Vector3(-Half + 0.08f, 1.95f, 0.0f), new Vector3(0.16f, 0.5f, 0.42f), white);
            Box("GreenPlate", fa, new Vector3(-Half + 0.1625f, 1.98f, 0.0f), new Vector3(0.005f, 0.26f, 0.26f), green);
            Box("Cross_V", fa, new Vector3(-Half + 0.166f, 1.98f, 0.0f), new Vector3(0.005f, 0.18f, 0.06f), white);
            Box("Cross_H", fa, new Vector3(-Half + 0.166f, 1.98f, 0.0f), new Vector3(0.005f, 0.06f, 0.18f), white);

            // ---------- AED sign (east wall, above the AED table) ----------
            var aed = Group("Sign_AED", details);
            Box("Backing", aed, new Vector3(Half - 0.01f, 1.95f, 1.5f), new Vector3(0.02f, 0.37f, 0.37f), white);
            Quad("Sign", aed, new Vector3(Half - 0.021f, 1.95f, 1.5f), 90f, new Vector2(0.35f, 0.35f), aedSign);

            // ---------- Clock (south wall, above the door) ----------
            var clock = Group("WallClock", details);
            Disc("Rim",  clock, new Vector3(0, 2.6f, -Half + 0.02f),  0.34f, 0.04f, black);
            Disc("Face", clock, new Vector3(0, 2.6f, -Half + 0.045f), 0.30f, 0.01f, white);
            Hand("HourHand",   clock, 0.08f, -60f, 0.016f, black); // points to 10
            Hand("MinuteHand", clock, 0.12f,  60f, 0.010f, black); // points to 2

            // ---------- Fire extinguisher (south wall, right of the door) ----------
            var fe = Group("FireExtinguisher", details);
            Box("Bracket", fe, new Vector3(1.1f, 0.95f, -Half + 0.035f), new Vector3(0.06f, 0.1f, 0.03f), black);
            Cyl("Body",    fe, new Vector3(1.1f, 0.7f, -Half + 0.1f),  new Vector3(0.15f, 0.25f, 0.15f), red);
            Cyl("Valve",   fe, new Vector3(1.1f, 0.98f, -Half + 0.1f), new Vector3(0.06f, 0.03f, 0.06f), black);
            Box("Handle",  fe, new Vector3(1.1f, 1.02f, -Half + 0.1f), new Vector3(0.1f, 0.02f, 0.03f), black);
            Box("Hose",    fe, new Vector3(1.19f, 0.8f, -Half + 0.1f), new Vector3(0.02f, 0.3f, 0.02f), black);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        if (missing.Length > 0)
            EditorUtility.DisplayDialog("Some images were not found",
                "These images were not found in " + TexFolder + ":\n" + missing +
                "\nThe details were still added (plain white). Add the images and run the tool again.", "OK");
        Debug.Log("Wall details added to " + PrefabPath);
    }

    // ---------- Helpers ----------

    static Transform Group(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Transform Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Setup(go, name, parent, pos, Quaternion.identity, size, mat);
        return go.transform;
    }

    static Transform Cyl(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Setup(go, name, parent, pos, Quaternion.identity, scale, mat);
        return go.transform;
    }

    // Flat disc facing into the room from the south wall (+Z direction)
    static void Disc(string name, Transform parent, Vector3 pos, float diameter, float thickness, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Setup(go, name, parent, pos, Quaternion.Euler(90f, 0f, 0f),
              new Vector3(diameter, thickness / 2f, diameter), mat);
    }

    // Clock hand on the south-wall clock; angle 0 = pointing up
    static void Hand(string name, Transform parent, float length, float angle, float width, Material mat)
    {
        var centre = new Vector3(0, 2.6f, -Half + 0.055f);
        float rad = angle * Mathf.Deg2Rad;
        var dir = new Vector3(-Mathf.Sin(rad), Mathf.Cos(rad), 0f);
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Setup(go, name, parent, centre + dir * (length / 2f), Quaternion.Euler(0f, 0f, angle),
              new Vector3(width, length, 0.005f), mat);
    }

    // Quad facing into the room. yRotation: 0 = north wall, 90 = east wall, -90 = west wall, 180 = south wall
    static void Quad(string name, Transform parent, Vector3 pos, float yRotation, Vector2 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Setup(go, name, parent, pos, Quaternion.Euler(0f, yRotation, 0f), new Vector3(size.x, size.y, 1f), mat);
    }

    static void Setup(GameObject go, string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
    {
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static Material GetMat(string name, Color color, float smoothness)
    {
        string path = MatFolder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(DefaultShader());
            AssetDatabase.CreateAsset(mat, path);
        }
        SetColor(mat, color);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness); // Built-in
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness); // URP
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material GetTexMat(string name, string textureFile)
    {
        var mat = GetMat(name, Color.white, 0.2f);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexFolder + "/" + textureFile);
        if (tex == null)
        {
            missing += "- " + textureFile + "\n";
            return mat;
        }
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex); // Built-in
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex); // URP
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void SetColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
    }

    static Shader DefaultShader()
    {
        var rp = GraphicsSettings.renderPipelineAsset;
        if (rp != null && rp.defaultMaterial != null) return rp.defaultMaterial.shader;
        return Shader.Find("Standard");
    }
}
