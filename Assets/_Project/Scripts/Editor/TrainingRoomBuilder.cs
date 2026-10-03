// TrainingRoomBuilder.cs
// Place this file inside a folder named "Editor" (e.g. Assets/_Project/Scripts/Editor/).
// Then use the menu: Tools > Pavithra > Build Training Room Blockout
// Builds a simple 8 m x 8 m x 3 m training room blockout from basic shapes,
// creates simple colour materials, and saves everything as a prefab.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class TrainingRoomBuilder
{
    // Room size in metres (1 Unity unit = 1 metre)
    const float W = 8f;      // width  (X)
    const float D = 8f;      // depth  (Z)
    const float H = 3f;      // height (Y)
    const float T = 0.15f;   // wall thickness
    const float DoorW = 1.2f;
    const float DoorH = 2.2f;

    const string MatFolder = "Assets/Art/Environment/Materials";
    const string PrefabFolder = "Assets/_Project/Prefabs";
    const string RootName = "TrainingRoomEnvironment";

    [MenuItem("Tools/Pavithra/Build Training Room Blockout")]
    public static void Build()
    {
        // Replace an existing room if one is already in the scene
        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Training Room already exists",
                "A '" + RootName + "' object is already in this scene. Replace it with a new one?",
                "Replace", "Cancel");
            if (!replace) return;
            Undo.DestroyObjectImmediate(existing);
        }

        EnsureFolder("Assets/Art");
        EnsureFolder("Assets/Art/Environment");
        EnsureFolder(MatFolder);
        EnsureFolder("Assets/_Project");
        EnsureFolder(PrefabFolder);

        // ---------- Materials ----------
        var floorMat   = GetMat("M_Floor",    new Color(0.55f, 0.55f, 0.58f));
        var wallMat    = GetMat("M_Wall",     new Color(0.92f, 0.92f, 0.88f));
        var ceilingMat = GetMat("M_Ceiling",  new Color(0.97f, 0.97f, 0.97f));
        var woodMat    = GetMat("M_Wood",     new Color(0.55f, 0.38f, 0.22f));
        var metalMat   = GetMat("M_Metal",    new Color(0.35f, 0.35f, 0.38f));
        var matMat     = GetMat("M_CPRMat",   new Color(0.15f, 0.35f, 0.75f));
        var greenMat   = GetMat("M_AEDGreen", new Color(0.10f, 0.60f, 0.30f));
        var darkMat    = GetMat("M_Dark",     new Color(0.08f, 0.08f, 0.08f));

        // ---------- Hierarchy ----------
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Training Room");
        root.transform.position = Vector3.zero;

        var structure = Group("Structure", root.transform);
        var props     = Group("Props", root.transform);
        var zones     = Group("Zones", root.transform);
        var lighting  = Group("Lighting", root.transform);

        // ---------- Floor & ceiling ----------
        Box("Floor",   structure, new Vector3(0, -0.05f, 0),     new Vector3(W, 0.1f, D), floorMat);
        Box("Ceiling", structure, new Vector3(0, H + 0.05f, 0),  new Vector3(W, 0.1f, D), ceilingMat);

        // ---------- Walls ----------
        Box("Wall_North", structure, new Vector3(0, H / 2, D / 2 + T / 2),      new Vector3(W + 2 * T, H, T), wallMat);
        Box("Wall_East",  structure, new Vector3(W / 2 + T / 2, H / 2, 0),      new Vector3(T, H, D), wallMat);
        Box("Wall_West",  structure, new Vector3(-(W / 2 + T / 2), H / 2, 0),   new Vector3(T, H, D), wallMat);

        // South wall has a door opening in the middle (the door itself is an interactive object)
        float segW = (W / 2 + T) - DoorW / 2;
        float segX = DoorW / 2 + segW / 2;
        float southZ = -(D / 2 + T / 2);
        Box("Wall_South_Left",  structure, new Vector3(-segX, H / 2, southZ), new Vector3(segW, H, T), wallMat);
        Box("Wall_South_Right", structure, new Vector3(segX, H / 2, southZ),  new Vector3(segW, H, T), wallMat);
        Box("Wall_South_AboveDoor", structure,
            new Vector3(0, DoorH + (H - DoorH) / 2, southZ), new Vector3(DoorW, H - DoorH, T), wallMat);

        // ---------- Zones ----------
        // Casualty area: a CPR mat in the middle of the room (mannequin lies along Z)
        Box("CasualtyArea_Mat", zones, new Vector3(0, 0.015f, 0.5f), new Vector3(1.0f, 0.03f, 2.0f), matMat);

        // Where the VR player starts (just inside the door, facing the casualty)
        var spawn = Group("PlayerSpawnPoint", zones);
        spawn.localPosition = new Vector3(0, 0, -3f);

        // ---------- Props ----------
        // AED table on the east side
        var aedTable = Table("AEDTable", props, new Vector3(3.2f, 0, 1.5f), new Vector3(0.6f, 0.75f, 1.2f), metalMat);
        Box("AED_Placeholder", aedTable, new Vector3(0, 0.75f + 0.06f, 0), new Vector3(0.3f, 0.12f, 0.25f), greenMat);

        // Phone table near the west wall
        var phoneTable = Table("PhoneTable", props, new Vector3(-3.3f, 0, -2.5f), new Vector3(0.5f, 0.75f, 0.6f), woodMat);
        Box("Phone_Placeholder", phoneTable, new Vector3(0, 0.75f + 0.01f, 0), new Vector3(0.08f, 0.02f, 0.16f), darkMat);

        // Instructor table and chairs (north-west corner)
        Table("InstructorTable", props, new Vector3(-2.2f, 0, 3.2f), new Vector3(1.6f, 0.75f, 0.8f), woodMat);
        Chair("Chair_1", props, new Vector3(-2.6f, 0, 2.45f), woodMat, metalMat);
        Chair("Chair_2", props, new Vector3(-1.8f, 0, 2.45f), woodMat, metalMat);

        // Training equipment shelf against the east wall (north end)
        var shelf = Group("EquipmentShelf", props);
        shelf.localPosition = new Vector3(3.75f, 0, 3.2f);
        Box("Shelf_Body", shelf, new Vector3(0, 0.9f, 0), new Vector3(0.4f, 1.8f, 1.4f), metalMat);
        Box("Shelf_Box_1", shelf, new Vector3(-0.05f, 1.25f, -0.35f), new Vector3(0.3f, 0.2f, 0.4f), matMat);
        Box("Shelf_Box_2", shelf, new Vector3(-0.05f, 1.25f, 0.3f),   new Vector3(0.3f, 0.25f, 0.35f), greenMat);

        // ---------- Lighting ----------
        Vector3[] lightPositions =
        {
            new Vector3(-2, H - 0.3f, -2), new Vector3(2, H - 0.3f, -2),
            new Vector3(-2, H - 0.3f,  2), new Vector3(2, H - 0.3f,  2)
        };
        for (int i = 0; i < lightPositions.Length; i++)
        {
            var lightGo = new GameObject("CeilingLight_" + (i + 1));
            lightGo.transform.SetParent(lighting, false);
            lightGo.transform.localPosition = lightPositions[i];
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 6f;
            light.intensity = 1.0f;
            light.color = new Color(1f, 0.96f, 0.90f);
            light.shadows = LightShadows.Soft;
        }

        // ---------- Save as prefab ----------
        string prefabPath = PrefabFolder + "/" + RootName + ".prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.UserAction);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();

        Debug.Log("Training room blockout built and saved to " + prefabPath);
    }

    // ---------- Helpers ----------

    static Transform Group(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
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

    // size = (width X, height Y, depth Z); position is the floor point under the table centre
    static Transform Table(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        var t = Group(name, parent);
        t.localPosition = pos;
        float top = 0.05f, leg = 0.05f;
        Box("Top", t, new Vector3(0, size.y - top / 2, 0), new Vector3(size.x, top, size.z), mat);
        float lx = size.x / 2 - leg, lz = size.z / 2 - leg, lh = size.y - top;
        Box("Leg_1", t, new Vector3( lx, lh / 2,  lz), new Vector3(leg, lh, leg), mat);
        Box("Leg_2", t, new Vector3(-lx, lh / 2,  lz), new Vector3(leg, lh, leg), mat);
        Box("Leg_3", t, new Vector3( lx, lh / 2, -lz), new Vector3(leg, lh, leg), mat);
        Box("Leg_4", t, new Vector3(-lx, lh / 2, -lz), new Vector3(leg, lh, leg), mat);
        return t;
    }

    // Chair facing +Z (towards the instructor table)
    static Transform Chair(string name, Transform parent, Vector3 pos, Material seatMat, Material legMat)
    {
        var c = Group(name, parent);
        c.localPosition = pos;
        float s = 0.45f, seatH = 0.45f, leg = 0.04f;
        Box("Seat", c, new Vector3(0, seatH, 0), new Vector3(s, 0.05f, s), seatMat);
        Box("Back", c, new Vector3(0, seatH + 0.25f, -s / 2 + 0.025f), new Vector3(s, 0.45f, 0.05f), seatMat);
        float o = s / 2 - leg;
        Box("Leg_1", c, new Vector3( o, seatH / 2,  o), new Vector3(leg, seatH, leg), legMat);
        Box("Leg_2", c, new Vector3(-o, seatH / 2,  o), new Vector3(leg, seatH, leg), legMat);
        Box("Leg_3", c, new Vector3( o, seatH / 2, -o), new Vector3(leg, seatH, leg), legMat);
        Box("Leg_4", c, new Vector3(-o, seatH / 2, -o), new Vector3(leg, seatH, leg), legMat);
        return c;
    }

    // Creates (or reuses) a simple coloured material that works with the project's render pipeline
    static Material GetMat(string name, Color color)
    {
        string path = MatFolder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(DefaultShader());
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color); // URP
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);     // Built-in
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Shader DefaultShader()
    {
        var rp = GraphicsSettings.renderPipelineAsset;
        if (rp != null && rp.defaultMaterial != null) return rp.defaultMaterial.shader;
        return Shader.Find("Standard");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
