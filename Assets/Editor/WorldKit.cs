using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using KateVR;

/// <summary>
/// Stylized building blocks for Kate's street: Craftsman bungalows, shopfronts, empty lots,
/// utility poles, wires, cracks, puddles, weeds, ground fog and hurrying figures.
/// Everything is made from simple shapes so it runs comfortably on Quest.
///
/// Local frame for buildings: the front (facade) faces local -Z, the building extends toward +Z,
/// local X runs along the street. Root origin = front-centre at ground level.
/// </summary>
public static class WorldKit
{
    public const string MaterialFolder = "Assets/Materials";
    const string MeshFolder = "Assets/Meshes";

    // ---------------- Materials (no colour: near-greys with the faintest warm/cool tint) ----------------
    public static Material Asphalt, Puddle, Concrete, Crack, Marking, Dirt;
    public static Material Siding, Shingle, Stucco, Trim, Roof, Glass, Door, Brick, Board, Awning;
    public static Material Wood, Wire, Weed;
    public static Material ClothDark, ClothMid, ClothLight, MaskMat, Lens, Fog;

    public static void LoadMaterials()
    {
        Asphalt   = MakeMaterial("World_Asphalt",  new Color(0.17f, 0.17f, 0.17f), 0.55f); // wet sheen
        Puddle    = MakeMaterial("World_Puddle",   new Color(0.07f, 0.07f, 0.075f), 0.92f);
        Concrete  = MakeMaterial("World_Concrete", new Color(0.36f, 0.355f, 0.34f), 0.2f);
        Crack     = MakeMaterial("World_Crack",    new Color(0.05f, 0.05f, 0.05f), 0.0f);
        Marking   = MakeMaterial("World_Marking",  new Color(0.46f, 0.455f, 0.44f), 0.3f);
        Dirt      = MakeMaterial("World_Dirt",     new Color(0.22f, 0.21f, 0.2f), 0.05f);
        Siding    = MakeMaterial("World_Siding",   new Color(0.47f, 0.46f, 0.44f), 0.1f);
        Shingle   = MakeMaterial("World_Shingle",  new Color(0.30f, 0.29f, 0.28f), 0.05f);
        Stucco    = MakeMaterial("World_Stucco",   new Color(0.53f, 0.52f, 0.50f), 0.05f);
        Trim      = MakeMaterial("World_Trim",     new Color(0.62f, 0.62f, 0.60f), 0.15f);
        Roof      = MakeMaterial("World_Roof",     new Color(0.19f, 0.19f, 0.19f), 0.1f);
        Glass     = MakeMaterial("World_Glass",    new Color(0.06f, 0.065f, 0.07f), 0.85f);
        Door      = MakeMaterial("World_Door",     new Color(0.17f, 0.16f, 0.15f), 0.2f);
        Brick     = MakeMaterial("World_Brick",    new Color(0.27f, 0.26f, 0.25f), 0.05f);
        Board     = MakeMaterial("World_Board",    new Color(0.40f, 0.39f, 0.36f), 0.05f);
        Awning    = MakeMaterial("World_Awning",   new Color(0.25f, 0.25f, 0.25f), 0.1f);
        Wood      = MakeMaterial("World_PoleWood", new Color(0.21f, 0.20f, 0.19f), 0.05f);
        Wire      = MakeMaterial("World_Wire",     new Color(0.06f, 0.06f, 0.06f), 0.3f);
        Weed      = MakeMaterial("World_Weed",     new Color(0.31f, 0.31f, 0.29f), 0.05f);
        ClothDark = MakeMaterial("Figure_ClothDark",  new Color(0.16f, 0.16f, 0.16f), 0.15f);
        ClothMid  = MakeMaterial("Figure_ClothMid",   new Color(0.33f, 0.33f, 0.31f), 0.15f);
        ClothLight= MakeMaterial("Figure_ClothLight", new Color(0.52f, 0.51f, 0.48f), 0.2f);
        MaskMat   = MakeMaterial("Figure_Mask",       new Color(0.80f, 0.80f, 0.78f), 0.3f);
        Lens      = MakeMaterial("Figure_Lens",       new Color(0.05f, 0.05f, 0.05f), 0.9f);
    }

    public static Material MakeMaterial(string name, Color color, float smoothness = 0.05f, string shaderName = "Universal Render Pipeline/Lit")
    {
        System.IO.Directory.CreateDirectory(MaterialFolder);
        string path = $"{MaterialFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find(shaderName) ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------------- Primitive helpers ----------------

    public static Transform Root(string name, Transform parent, Vector3 pos, float rotY)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0, rotY, 0);
        return t;
    }

    public static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 lpos, Vector3 lscale, Material mat, Vector3 leuler = default, bool isStatic = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lpos;
        go.transform.localRotation = Quaternion.Euler(leuler);
        go.transform.localScale = lscale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        go.isStatic = isStatic;
        return go;
    }

    public static GameObject Cube(string name, Transform parent, Vector3 lpos, Vector3 lscale, Material mat, Vector3 leuler = default, bool isStatic = true)
        => Part(PrimitiveType.Cube, name, parent, lpos, lscale, mat, leuler, isStatic);

    /// <summary>A thin box stretched between two points (wires, cracks).</summary>
    public static GameObject Span(string name, Transform parent, Vector3 a, Vector3 b, float thickness, float height, Material mat)
    {
        Vector3 d = b - a;
        if (d.sqrMagnitude < 0.0001f) d = Vector3.forward * 0.01f;
        var go = Cube(name, parent, (a + b) * 0.5f, new Vector3(thickness, height, d.magnitude), mat);
        go.transform.localRotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        return go;
    }

    // Triangular prism (gable infill): base width 1 on X, apex height 1 on Y, depth 1 on Z.
    static Mesh _prism;
    static Mesh PrismMesh()
    {
        if (_prism != null) return _prism;
        System.IO.Directory.CreateDirectory(MeshFolder);
        string path = $"{MeshFolder}/Prism.asset";
        _prism = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (_prism != null) return _prism;

        Vector3 fl = new Vector3(-0.5f, 0, -0.5f), fr = new Vector3(0.5f, 0, -0.5f), ft = new Vector3(0, 1, -0.5f);
        Vector3 bl = new Vector3(-0.5f, 0, 0.5f), br = new Vector3(0.5f, 0, 0.5f), bt = new Vector3(0, 1, 0.5f);
        var v = new[]
        {
            ft, fr, fl,          // front (faces -Z)
            bt, bl, br,          // back (faces +Z)
            fl, bt, ft,  fl, bl, bt,   // left slope
            fr, ft, bt,  fr, bt, br,   // right slope
        };
        var tris = new int[v.Length];
        for (int i = 0; i < tris.Length; i++) tris[i] = i;
        _prism = new Mesh { name = "Prism", vertices = v, triangles = tris };
        _prism.RecalculateNormals();
        _prism.RecalculateBounds();
        AssetDatabase.CreateAsset(_prism, path);
        return _prism;
    }

    public static GameObject Prism(string name, Transform parent, Vector3 lpos, Vector3 lscale, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lpos;
        go.transform.localScale = lscale;
        go.AddComponent<MeshFilter>().sharedMesh = PrismMesh();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        go.isStatic = true;
        return go;
    }

    /// <summary>Front-facing gable roof: triangle infill + two roof slabs with overhanging eaves.</summary>
    public static void Gable(Transform parent, float centerZ, float width, float depth, float baseY, float pitchDeg, Material wallMat, float eave = 0.6f)
    {
        float half = width * 0.5f;
        float rise = half * Mathf.Tan(pitchDeg * Mathf.Deg2Rad);
        Prism("Gable", parent, new Vector3(0, baseY, centerZ), new Vector3(width, rise, depth), wallMat);

        float run = half + eave;
        float slope = run / Mathf.Cos(pitchDeg * Mathf.Deg2Rad) + 0.15f;
        const float thick = 0.18f;
        for (int s = -1; s <= 1; s += 2)
        {
            float xc = s * run * 0.5f;
            float yc = baseY + rise * (1f - Mathf.Abs(xc) / half) + thick * 0.5f;
            Cube("Roof", parent, new Vector3(xc, yc, centerZ), new Vector3(slope, thick, depth + 1.2f), Roof, new Vector3(0, 0, -s * pitchDeg));
        }
    }

    // ---------------- Windows ----------------

    /// <summary>Window on the facade (z = 0 plane, facing -Z).</summary>
    public static void FrontWindow(Transform p, float x, float y, float w, float h, bool boarded)
    {
        Cube("Window Trim", p, new Vector3(x, y, -0.03f), new Vector3(w + 0.22f, h + 0.22f, 0.06f), Trim);
        Cube("Window", p, new Vector3(x, y, -0.065f), new Vector3(w, h, 0.03f), Glass);
        if (boarded)
        {
            Cube("Board", p, new Vector3(x, y, -0.1f), new Vector3(w * 1.25f, 0.18f, 0.03f), Board, new Vector3(0, 0, 32));
            Cube("Board", p, new Vector3(x, y + 0.1f, -0.11f), new Vector3(w * 1.25f, 0.18f, 0.03f), Board, new Vector3(0, 0, -28));
        }
    }

    /// <summary>Window on a side wall (x = ±halfWidth plane).</summary>
    public static void SideWindow(Transform p, float side, float halfWidth, float z, float y, float w, float h)
    {
        float x = side * (halfWidth + 0.03f);
        Cube("Window Trim", p, new Vector3(x, y, z), new Vector3(0.06f, h + 0.22f, w + 0.22f), Trim);
        Cube("Window", p, new Vector3(x + side * 0.035f, y, z), new Vector3(0.03f, h, w), Glass);
    }

    // ---------------- Buildings ----------------

    /// <summary>A Craftsman bungalow (stories = 1) or a two-storey brown-shingle house (stories = 2).</summary>
    public static Transform House(string name, Transform parent, Vector3 pos, float rotY, float w, float d, int stories, int seed, bool decayed)
    {
        var rng = new System.Random(seed);
        var root = Root(name, parent, pos, rotY);
        float floorY = 0.6f;
        float wallH = stories == 1 ? 2.8f : 5.6f;
        float top = floorY + wallH;
        var wall = stories == 1 ? Siding : Shingle;
        float hw = w * 0.5f;

        Cube("Foundation", root, new Vector3(0, floorY * 0.5f, d * 0.5f), new Vector3(w + 0.15f, floorY, d + 0.15f), Brick);
        Cube("Body", root, new Vector3(0, floorY + wallH * 0.5f, d * 0.5f), new Vector3(w, wallH, d), wall);
        Cube("Belt Trim", root, new Vector3(0, floorY + 0.05f, -0.02f), new Vector3(w + 0.05f, 0.12f, 0.06f), Trim);
        Gable(root, d * 0.5f, w, d, top, stories == 1 ? 22f : 30f, wall);
        Cube("Gable Vent", root, new Vector3(0, top + 0.9f, -0.03f), new Vector3(0.9f, 0.5f, 0.05f), Glass);

        // Porch with its own small gable, tapered columns on piers, pony walls and steps.
        float porchD = 2.6f;
        Cube("Porch Deck", root, new Vector3(0, floorY * 0.5f, -porchD * 0.5f), new Vector3(w, floorY, porchD), Concrete);
        float pw = w * 0.62f;
        float porchTop = floorY + 2.4f;
        Cube("Porch Beam", root, new Vector3(0, porchTop - 0.15f, -porchD + 0.25f), new Vector3(pw + 0.4f, 0.3f, 0.3f), Trim);
        Gable(root, -porchD * 0.5f + 0.1f, pw + 0.4f, porchD + 0.2f, porchTop, 20f, wall, 0.4f);
        for (int s = -1; s <= 1; s += 2)
        {
            float cx = s * pw * 0.5f;
            Cube("Pier", root, new Vector3(cx, floorY + 0.5f, -porchD + 0.3f), new Vector3(0.65f, 1.0f, 0.65f), Brick);
            Cube("Column", root, new Vector3(cx, floorY + 1.0f + 0.35f, -porchD + 0.3f), new Vector3(0.42f, 0.7f, 0.42f), Trim);
            Cube("Column", root, new Vector3(cx, floorY + 1.7f + 0.35f, -porchD + 0.3f), new Vector3(0.32f, 0.7f, 0.32f), Trim);
            Cube("Pony Wall", root, new Vector3(s * (0.95f + (hw - 0.95f) * 0.5f), floorY + 0.4f, -porchD + 0.12f), new Vector3(hw - 0.95f, 0.8f, 0.22f), wall);
        }
        Cube("Step", root, new Vector3(0, 0.2f, -porchD - 0.175f), new Vector3(1.8f, 0.4f, 0.35f), Concrete);
        Cube("Step", root, new Vector3(0, 0.1f, -porchD - 0.525f), new Vector3(1.8f, 0.2f, 0.35f), Concrete);

        // Front door + windows.
        Cube("Door Frame", root, new Vector3(0, floorY + 1.1f, -0.03f), new Vector3(1.3f, 2.35f, 0.06f), Trim);
        Cube("Door", root, new Vector3(0, floorY + 1.05f, -0.07f), new Vector3(1.0f, 2.1f, 0.06f), Door);
        Cube("Door Knob", root, new Vector3(0.36f, floorY + 1.0f, -0.12f), new Vector3(0.07f, 0.07f, 0.07f), Trim);
        FrontWindow(root, -hw * 0.55f, floorY + 1.6f, 1.5f, 1.3f, decayed && rng.NextDouble() < 0.7);
        FrontWindow(root,  hw * 0.55f, floorY + 1.6f, 1.5f, 1.3f, decayed && rng.NextDouble() < 0.4);
        if (stories == 2)
        {
            FrontWindow(root, -hw * 0.45f, floorY + 4.3f, 1.2f, 1.2f, decayed);
            FrontWindow(root,  hw * 0.45f, floorY + 4.3f, 1.2f, 1.2f, false);
        }
        for (int s = -1; s <= 1; s += 2)
            for (float z = 2f; z < d - 1.5f; z += 3.2f)
                SideWindow(root, s, hw, z, floorY + 1.6f, 1.1f, 1.2f);

        // Chimney on one side.
        float chH = top + (stories == 1 ? 3.2f : 3.8f);
        Cube("Chimney", root, new Vector3(-(hw + 0.4f), chH * 0.5f, d * 0.62f), new Vector3(0.8f, chH, 0.9f), Brick);
        return root;
    }

    /// <summary>A shopfront: flat roof behind a false front, big dark display window, awning.</summary>
    public static Transform Shop(string name, Transform parent, Vector3 pos, float rotY, float w, float d, int stories, int seed, bool awning, bool tornAwning)
    {
        var rng = new System.Random(seed);
        var root = Root(name, parent, pos, rotY);
        float h = stories == 1 ? 4.6f : 7.8f;
        float hw = w * 0.5f;

        Cube("Body", root, new Vector3(0, h * 0.5f, d * 0.5f), new Vector3(w, h, d), Stucco);
        Cube("Cornice", root, new Vector3(0, h, -0.05f), new Vector3(w + 0.3f, 0.18f, 0.5f), Trim);
        Cube("Parapet", root, new Vector3(0, h + 0.35f, 0.1f), new Vector3(w + 0.1f, 0.7f, 0.3f), Stucco);
        Cube("False Front", root, new Vector3(0, h + 0.95f, 0.1f), new Vector3(w * 0.4f, 0.6f, 0.3f), Stucco);

        Cube("Bulkhead", root, new Vector3(-w * 0.1f, 0.25f, -0.05f), new Vector3(w * 0.7f, 0.5f, 0.12f), Trim);
        bool boarded = rng.NextDouble() < 0.5;
        Cube(boarded ? "Boarded Window" : "Display Window", root, new Vector3(-w * 0.1f, 0.5f + 1.3f, -0.03f), new Vector3(w * 0.7f, 2.6f, 0.08f), boarded ? Board : Glass);
        Cube("Door", root, new Vector3(hw - 1.0f, 1.25f, -0.03f), new Vector3(1.1f, 2.5f, 0.08f), Glass);
        Cube("Sign Band", root, new Vector3(0, 3.45f, -0.07f), new Vector3(w * 0.85f, 0.7f, 0.1f), Trim);

        if (awning)
        {
            var e = tornAwning ? new Vector3(-30, 0, 9) : new Vector3(-18, 0, 0);
            Cube("Awning", root, new Vector3(-w * 0.05f, 3.0f, -0.8f), new Vector3(w * 0.8f, 0.06f, 1.7f), Awning, e);
        }
        if (stories == 2)
            for (int i = -1; i <= 1; i++)
                FrontWindow(root, i * w * 0.3f, 5.9f, 1.2f, 1.6f, rng.NextDouble() < 0.35);
        return root;
    }

    /// <summary>Where a building used to be: a cracked slab, rubble, maybe a lone chimney.</summary>
    public static Transform Lot(string name, Transform parent, Vector3 pos, float rotY, float w, float d, int seed, bool chimney)
    {
        var rng = new System.Random(seed);
        var root = Root(name, parent, pos, rotY);
        Cube("Slab", root, new Vector3(0, 0.12f, d * 0.5f + 1f), new Vector3(w, 0.24f, d), Concrete);
        Cube("Wall Stub", root, new Vector3(-w * 0.2f, 0.24f + 0.6f, 1.1f), new Vector3(w * 0.45f, 1.2f, 0.25f), Brick, new Vector3(0, 0, 3));
        for (int i = 0; i < 7; i++)
        {
            float s = Range(rng, 0.3f, 1.1f);
            Cube("Rubble", root, new Vector3(Range(rng, -w * 0.4f, w * 0.4f), 0.24f + s * 0.3f, Range(rng, 2f, d)),
                 new Vector3(s, s * 0.6f, s * 0.8f), rng.NextDouble() < 0.5 ? Brick : Concrete,
                 new Vector3(Range(rng, -20, 20), Range(rng, 0, 360), Range(rng, -20, 20)));
        }
        if (chimney) Cube("Lone Chimney", root, new Vector3(w * 0.25f, 3f, d * 0.6f), new Vector3(0.9f, 6f, 1.0f), Brick);
        return root;
    }

    // ---------------- Street furniture ----------------

    public static Vector3 Pole(Transform parent, Vector3 pos, bool transformer)
    {
        var root = Root("Utility Pole", parent, pos, 0);
        Part(PrimitiveType.Cylinder, "Pole", root, new Vector3(0, 4.5f, 0), new Vector3(0.28f, 4.5f, 0.28f), Wood);
        Cube("Crossarm", root, new Vector3(0, 8.3f, 0), new Vector3(2.2f, 0.15f, 0.15f), Wood);
        Cube("Crossarm", root, new Vector3(0, 7.5f, 0), new Vector3(1.4f, 0.12f, 0.12f), Wood);
        if (transformer) Part(PrimitiveType.Cylinder, "Transformer", root, new Vector3(0.35f, 6.6f, 0), new Vector3(0.55f, 0.45f, 0.55f), Concrete);
        return pos;
    }

    public static void Wires(Transform parent, Vector3 poleA, Vector3 poleB, bool broken)
    {
        float[] xs = { -0.9f, 0.9f };
        foreach (float x in xs)
        {
            Vector3 a = poleA + new Vector3(x, 8.4f, 0), b = poleB + new Vector3(x, 8.4f, 0);
            Vector3 mid = (a + b) * 0.5f + Vector3.down * 0.7f;
            Span("Wire", parent, a, mid, 0.03f, 0.03f, Wire);
            Span("Wire", parent, mid, b, 0.03f, 0.03f, Wire);
        }
        Vector3 la = poleA + new Vector3(0.6f, 7.55f, 0), lb = poleB + new Vector3(0.6f, 7.55f, 0);
        if (broken)
        {
            // Snapped line hanging from one pole down to the pavement.
            Vector3 hang = la + new Vector3(0.4f, -4f, (lb - la).normalized.z * 2.5f);
            Span("Broken Wire", parent, la, hang, 0.03f, 0.03f, Wire);
            Span("Broken Wire", parent, hang, new Vector3(hang.x + 0.3f, 0.2f, hang.z + 1.5f), 0.03f, 0.03f, Wire);
        }
        else
        {
            Vector3 mid = (la + lb) * 0.5f + Vector3.down * 0.9f;
            Span("Wire", parent, la, mid, 0.03f, 0.03f, Wire);
            Span("Wire", parent, mid, lb, 0.03f, 0.03f, Wire);
        }
    }

    public static void SignPost(Transform parent, Vector3 pos, float lean)
    {
        var root = Root("Sign Post", parent, pos, Random.Range(0f, 30f));
        root.localRotation *= Quaternion.Euler(0, 0, lean);
        Part(PrimitiveType.Cylinder, "Post", root, new Vector3(0, 1.3f, 0), new Vector3(0.07f, 1.3f, 0.07f), Wood);
        Cube("Sign", root, new Vector3(0, 2.45f, -0.05f), new Vector3(0.6f, 0.6f, 0.03f), Trim, new Vector3(0, 0, 45));
    }

    // ---------------- Ground detail ----------------

    public static void Cracks(Transform parent, System.Random rng, float xMin, float xMax, float zMin, float zMax, float y, int chains)
    {
        for (int c = 0; c < chains; c++)
        {
            Vector3 p = new Vector3(Range(rng, xMin, xMax), y, Range(rng, zMin, zMax));
            float ang = Range(rng, 0, 360);
            int segs = rng.Next(3, 7);
            for (int s = 0; s < segs; s++)
            {
                ang += Range(rng, -50, 50);
                float len = Range(rng, 0.4f, 1.3f);
                Vector3 q = p + Quaternion.Euler(0, ang, 0) * Vector3.forward * len;
                q.x = Mathf.Clamp(q.x, xMin, xMax);
                Span("Crack", parent, p, q, Range(rng, 0.03f, 0.07f), 0.004f, Crack);
                p = q;
            }
        }
    }

    public static void PuddleAt(Transform parent, Vector3 pos, float sx, float sz, float rotY)
        => Part(PrimitiveType.Cylinder, "Puddle", parent, pos, new Vector3(sx, 0.002f, sz), Puddle, new Vector3(0, rotY, 0));

    public static void WeedTuft(Transform parent, System.Random rng, Vector3 pos)
    {
        var root = Root("Weeds", parent, pos, Range(rng, 0, 360));
        int blades = rng.Next(4, 7);
        for (int i = 0; i < blades; i++)
        {
            float h = Range(rng, 0.18f, 0.5f);
            Cube("Blade", root, new Vector3(Range(rng, -0.08f, 0.08f), h * 0.45f, Range(rng, -0.08f, 0.08f)),
                 new Vector3(0.025f, h, 0.01f), Weed, new Vector3(Range(rng, -28, 28), Range(rng, 0, 180), Range(rng, -28, 28)));
        }
    }

    // ---------------- Ground fog ----------------

    public static void GroundFog(Transform parent, Vector3 center, Vector3 size, Color? tint = null)
    {
        var tex = FogPuffTexture();
        var mat = MakeMaterial("World_GroundFog", Color.white, 0f, "Universal Render Pipeline/Particles/Unlit");
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", tint ?? new Color(0.66f, 0.64f, 0.60f, 0.5f));
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        // Fade puffs out as they reach the viewer's face, so no hard-edged sheets cut through the head.
        mat.SetFloat("_CameraFadingEnabled", 1f);
        mat.SetFloat("_CameraNearFadeDistance", 0.6f);
        mat.SetFloat("_CameraFarFadeDistance", 2.5f);
        mat.SetVector("_CameraFadeParams", new Vector4(0.6f, 1f / (2.5f - 0.6f), 0, 0));
        mat.EnableKeyword("_FADING_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);

        var go = new GameObject("Ground Fog");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 30f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(18f, 30f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(3.5f, 7.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.maxParticles = 170;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 170f / 24f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = size;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0.12f, 0.45f);
        vel.y = new ParticleSystem.MinMaxCurve(0f, 0.03f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.8f, 1, 1.25f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.alignment = ParticleSystemRenderSpace.Facing; // steadier than screen-aligned in VR
        r.sortMode = ParticleSystemSortMode.Distance;
        r.maxParticleSize = 5f;
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    public static Texture2D FogPuffTexture()
    {
        System.IO.Directory.CreateDirectory(MaterialFolder);
        string path = $"{MaterialFolder}/FogPuff.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
            float r = Mathf.Sqrt(u * u + v * v);
            float fall = Mathf.Clamp01(1f - r);
            fall = fall * fall * (3f - 2f * fall);
            float noise = 0.55f + 0.45f * Mathf.PerlinNoise(x * 0.06f + 3.1f, y * 0.06f + 7.7f);
            px[y * n + x] = new Color(1, 1, 1, fall * noise * 0.85f);
        }
        tex.SetPixels(px);
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ---------------- Ash ----------------

    /// <summary>Fine ash drifting in the air around Kate. Emitted around the rig, but left behind in the world as she walks.</summary>
    public static void Ash(Transform rig, Color? tint = null)
    {
        var mat = MakeMaterial("World_Ash", Color.white, 0f, "Universal Render Pipeline/Particles/Unlit");
        mat.SetTexture("_BaseMap", FogPuffTexture());
        mat.SetColor("_BaseColor", tint ?? new Color(0.78f, 0.76f, 0.72f, 0.7f));
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);

        var go = new GameObject("Ash");
        go.transform.SetParent(rig, false);
        go.transform.localPosition = new Vector3(0f, 1.8f, 3f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 10f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.035f);
        main.startColor = Color.white;
        main.maxParticles = 700;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 70f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(10f, 4f, 12f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0.08f, 0.3f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.1f, -0.02f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.1f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.alignment = ParticleSystemRenderSpace.Facing;
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.minParticleSize = 0.0015f; // keep distant specks from vanishing to nothing
    }

    // ---------------- People ----------------

    /// <summary>
    /// A hurrying figure made of simple shapes. Variant 0: long coat, hood, gas mask.
    /// Variant 1: hoodie, wrapped scarf, backpack. Variant 2: poncho, respirator, hat.
    /// Limb pivots are named LegL/LegR/ArmL/ArmR so Wanderer can swing them.
    /// </summary>
    public static void Figure(Transform parent, int variant, Vector3 feet, float heading, float speed)
    {
        var root = Root("Walker", parent, feet, heading);
        var body = new GameObject("Body").transform;
        body.SetParent(root, false);
        body.localRotation = Quaternion.Euler(7, 0, 0); // leaning forward, in a hurry

        Material coat = variant == 0 ? ClothDark : variant == 1 ? ClothMid : ClothLight;
        Material pants = variant == 2 ? ClothDark : ClothMid;

        for (int s = -1; s <= 1; s += 2)
        {
            var leg = new GameObject(s < 0 ? "LegL" : "LegR").transform;
            leg.SetParent(body, false);
            leg.localPosition = new Vector3(s * 0.11f, 0.92f, 0);
            Cube("Leg", leg, new Vector3(0, -0.45f, 0), new Vector3(0.17f, 0.9f, 0.19f), pants, default, false);
            Cube("Boot", leg, new Vector3(0, -0.87f, 0.05f), new Vector3(0.18f, 0.1f, 0.3f), ClothDark, default, false);

            var arm = new GameObject(s < 0 ? "ArmL" : "ArmR").transform;
            arm.SetParent(body, false);
            arm.localPosition = new Vector3(s * 0.29f, 1.5f, 0);
            Cube("Arm", arm, new Vector3(0, -0.3f, 0), new Vector3(0.13f, 0.62f, 0.14f), coat, default, false);
            Cube("Glove", arm, new Vector3(0, -0.65f, 0), new Vector3(0.11f, 0.12f, 0.11f), ClothDark, default, false);
        }
        Cube("Hips", body, new Vector3(0, 0.96f, 0), new Vector3(0.38f, 0.2f, 0.24f), pants, default, false);
        Cube("Torso", body, new Vector3(0, 1.25f, 0), new Vector3(0.46f, 0.6f, 0.27f), coat, default, false);
        Part(PrimitiveType.Sphere, "Head", body, new Vector3(0, 1.71f, 0), new Vector3(0.23f, 0.26f, 0.24f), ClothDark, default, false);

        switch (variant)
        {
            case 0: // long coat, hood, gas mask
                Cube("Coat Skirt", body, new Vector3(0, 0.8f, 0), new Vector3(0.5f, 0.58f, 0.31f), coat, default, false);
                Part(PrimitiveType.Sphere, "Hood", body, new Vector3(0, 1.73f, -0.02f), new Vector3(0.28f, 0.31f, 0.29f), coat, default, false);
                Cube("Mask", body, new Vector3(0, 1.66f, 0.12f), new Vector3(0.17f, 0.15f, 0.07f), MaskMat, default, false);
                Part(PrimitiveType.Cylinder, "Filter", body, new Vector3(0, 1.61f, 0.18f), new Vector3(0.09f, 0.04f, 0.09f), ClothDark, new Vector3(90, 0, 0), false);
                Cube("Goggles", body, new Vector3(0, 1.74f, 0.13f), new Vector3(0.19f, 0.05f, 0.04f), Lens, default, false);
                break;
            case 1: // hoodie, scarf over the face, backpack
                Part(PrimitiveType.Sphere, "Hood", body, new Vector3(0, 1.72f, -0.03f), new Vector3(0.27f, 0.3f, 0.28f), coat, default, false);
                Cube("Scarf", body, new Vector3(0, 1.6f, 0.03f), new Vector3(0.25f, 0.14f, 0.25f), ClothLight, default, false);
                Cube("Backpack", body, new Vector3(0, 1.28f, -0.21f), new Vector3(0.33f, 0.43f, 0.18f), ClothDark, default, false);
                break;
            default: // poncho, respirator, bucket hat
                Cube("Poncho", body, new Vector3(0, 1.12f, 0), new Vector3(0.64f, 0.8f, 0.35f), coat, default, false);
                Cube("Respirator", body, new Vector3(0, 1.65f, 0.12f), new Vector3(0.12f, 0.09f, 0.06f), MaskMat, default, false);
                Part(PrimitiveType.Cylinder, "Hat Brim", body, new Vector3(0, 1.84f, 0), new Vector3(0.38f, 0.008f, 0.38f), ClothDark, default, false);
                Part(PrimitiveType.Cylinder, "Hat", body, new Vector3(0, 1.89f, 0), new Vector3(0.23f, 0.06f, 0.23f), ClothDark, default, false);
                break;
        }

        var w = root.gameObject.AddComponent<Wanderer>();
        w.speed = speed;
    }

    public static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
}
