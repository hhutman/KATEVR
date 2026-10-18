using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using KateVR;

/// <summary>
/// The ochre art direction (Hope's concept image, 2026-10-17): a monochrome amber world,
/// golden fog that dissolves the horizon, smooth drifted-ash dunes instead of a street,
/// soft silhouettes of gabled houses and wooden poles, sagging wires with tattered strips.
/// </summary>
public static class OchreWorld
{
    // ---------- Palette (#C88A3B → #F5D799 and the darker browns of silhouettes) ----------
    public static readonly Color Fog        = new Color(0.93f, 0.80f, 0.55f);   // buttercream-gold haze
    public static readonly Color Glow       = new Color(1.00f, 0.90f, 0.66f);   // the bright centre-distance
    public static readonly Color Ambient    = new Color(0.80f, 0.60f, 0.36f);
    public static readonly Color Sun        = new Color(1.00f, 0.84f, 0.58f);
    public static readonly Color Silhouette = new Color(0.36f, 0.24f, 0.13f);   // houses
    public static readonly Color DarkWood   = new Color(0.22f, 0.14f, 0.07f);   // poles, figures
    public static readonly Color PorchWood  = new Color(0.45f, 0.30f, 0.15f);
    public static readonly Color Concrete   = new Color(0.62f, 0.48f, 0.30f);
    public static readonly Color Cloth      = new Color(0.66f, 0.50f, 0.28f);
    public static readonly Color Tape       = new Color(0.78f, 0.60f, 0.32f);

    public const float FogStart = 1f;
    public const float FogEnd = 34f;

    public static Material Mat(string name, Color c, float smooth = 0.05f) => WorldKit.MakeMaterial(name, c, smooth);

    // ---------- Light & air ----------

    public static void Atmosphere()
    {
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = Fog;
        RenderSettings.fogStartDistance = FogStart;
        RenderSettings.fogEndDistance = FogEnd;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Ambient;

        // A low sun behind the haze, straight down the street: everything facing Kate falls into silhouette.
        var sun = new GameObject("Hazy Sun (ahead)");
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Sun;
        light.intensity = 0.9f;
        light.shadows = LightShadows.None;
        sun.transform.rotation = Quaternion.Euler(14f, 190f, 0f);
    }

    public static void HorizonGlow(Transform parent, Transform rig)
    {
        var shader = Shader.Find("KateVR/HorizonGlow");
        string path = WorldKit.MaterialFolder + "/Ochre_HorizonGlow.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = shader;
        mat.SetColor("_Color", Glow);
        EditorUtility.SetDirty(mat);

        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Horizon Glow";
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(parent, false);
        q.transform.localScale = new Vector3(80f, 34f, 1f);
        var r = q.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        q.AddComponent<KateVR.HorizonGlow>().follow = rig;
    }

    // ---------- Ash dunes ----------

    /// <summary>
    /// A smooth heightfield of drifted ash. Low lanes are kept where Kate walks and where the walker wades,
    /// the porch area is nearly clear, and ash piles up toward the buildings on both sides.
    /// </summary>
    public static void Dunes(Transform parent, IList<Vector3> katePath, float walkerX)
    {
        var shader = Shader.Find("KateVR/AshDune");
        string matPath = WorldKit.MaterialFolder + "/Ochre_AshDunes.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matPath); }
        mat.shader = shader;
        mat.SetColor("_MistColor", Fog);
        EditorUtility.SetDirty(mat);

        // Densely sample Kate's path so we can measure distance to it.
        var lane = new List<Vector2>();
        for (int i = 0; i < katePath.Count - 1; i++)
            for (int s = 0; s < 10; s++)
            {
                Vector3 p = Vector3.Lerp(katePath[i], katePath[i + 1], s / 10f);
                lane.Add(new Vector2(p.x, p.z));
            }
        lane.Add(new Vector2(katePath[katePath.Count - 1].x, katePath[katePath.Count - 1].z));

        const float x0 = -45f, x1 = 45f, z0 = -25f, z1 = 115f, step = 0.75f;
        int nx = Mathf.RoundToInt((x1 - x0) / step) + 1, nz = Mathf.RoundToInt((z1 - z0) / step) + 1;
        var verts = new Vector3[nx * nz];
        for (int j = 0; j < nz; j++)
        for (int i = 0; i < nx; i++)
        {
            float x = x0 + i * step, z = z0 + j * step;
            verts[j * nx + i] = new Vector3(x, Height(x, z, lane, walkerX), z);
        }
        var tris = new int[(nx - 1) * (nz - 1) * 6];
        int t = 0;
        for (int j = 0; j < nz - 1; j++)
        for (int i = 0; i < nx - 1; i++)
        {
            int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
            tris[t++] = a; tris[t++] = c; tris[t++] = b;
            tris[t++] = b; tris[t++] = c; tris[t++] = d;
        }
        var mesh = new Mesh { name = "Ash Dunes", indexFormat = IndexFormat.UInt32, vertices = verts, triangles = tris };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        System.IO.Directory.CreateDirectory("Assets/Meshes");
        string meshPath = "Assets/Meshes/AshDunes.asset";
        AssetDatabase.DeleteAsset(meshPath);
        AssetDatabase.CreateAsset(mesh, meshPath);

        var go = new GameObject("Ash Dunes");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        go.isStatic = true;
    }

    static float Height(float x, float z, List<Vector2> lane, float walkerX)
    {
        // Long, sinuous drifts sweeping diagonally across the street.
        float a = Mathf.Sin(0.17f * x + 0.075f * z + 0.8f * Mathf.Sin(0.045f * z + 0.3f));
        float b = Mathf.Sin(0.10f * z - 0.085f * x + 1.3f);
        float c = Mathf.Sin(0.26f * x + 0.19f * z + 2.1f);
        float crest = Mathf.Pow(a * 0.5f + 0.5f, 1.6f);           // softer troughs, rounder crests
        float h = 0.15f + 0.55f * crest + 0.22f * (b * 0.5f + 0.5f) + 0.10f * (c * 0.5f + 0.5f);

        // Ash piles up toward the walls on both sides of the old street.
        float side = Mathf.Abs(x);
        h += 1.1f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f, 10f, side));
        h += 0.6f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(12f, 22f, side));

        // A shallow, trodden lane where Kate walks…
        float dk = float.MaxValue;
        var p = new Vector2(x, z);
        for (int i = 0; i < lane.Count; i++) dk = Mathf.Min(dk, (lane[i] - p).sqrMagnitude);
        dk = Mathf.Sqrt(dk);
        h *= Mathf.Lerp(0.12f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 4.5f, dk)));

        // …and where the walker wades (ankle-deep).
        if (z > -5f && z < 70f)
            h *= Mathf.Lerp(0.3f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 2f, Mathf.Abs(x - walkerX))));

        // Nearly clear at the porch and the walkway edge, so they peek out of the drifts.
        float porch = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.5f, 7.5f, x)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(13f, 14f, x)))
                    * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(35.5f, 37.5f, z)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(44.5f, 46.5f, z)));
        h *= Mathf.Lerp(1f, 0.08f, porch);

        return Mathf.Max(0.02f, h);
    }

    // ---------- Silhouette houses ----------

    public static void GabledHouse(Transform parent, Vector3 frontCentre, float rotY, float w, float d, float wallH, float roofRise, Material mat)
    {
        var root = WorldKit.Root("House", parent, frontCentre, rotY);
        WorldKit.Cube("Walls", root, new Vector3(0, wallH * 0.5f, d * 0.5f), new Vector3(w, wallH, d), mat);
        WorldKit.Prism("Gable", root, new Vector3(0, wallH, d * 0.5f), new Vector3(w + 0.6f, roofRise, d + 0.6f), mat);
    }

    // ---------- Poles, wires and tattered strips ----------

    public static void PolesAndWires(Transform parent, float x, float[] zs, Material wood, Material wire, Material cloth, Material tape, System.Random rng)
    {
        var tops = new List<Vector3>();
        foreach (float z in zs)
        {
            var root = WorldKit.Root("Utility Pole", parent, new Vector3(x, 0f, z), WorldKit.Range(rng, -4f, 4f));
            root.localRotation *= Quaternion.Euler(WorldKit.Range(rng, -3f, 3f), 0f, WorldKit.Range(rng, -4f, 4f)); // leaning a little
            WorldKit.Part(PrimitiveType.Cylinder, "Pole", root, new Vector3(0, 4.5f, 0), new Vector3(0.26f, 4.5f, 0.26f), wood);
            WorldKit.Cube("Crossarm", root, new Vector3(0, 8.3f, 0), new Vector3(2.2f, 0.14f, 0.14f), wood);
            WorldKit.Cube("Crossarm", root, new Vector3(0, 7.5f, 0), new Vector3(1.4f, 0.12f, 0.12f), wood);
            tops.Add(new Vector3(x, 0f, z));
        }
        float[] offsets = { -0.9f, 0.9f, 0f };
        float[] heights = { 8.4f, 8.4f, 7.6f };
        float[] sags = { 1.1f, 1.3f, 1.6f };
        for (int i = 1; i < tops.Count; i++)
            for (int k = 0; k < offsets.Length; k++)
            {
                Vector3 a = tops[i - 1] + new Vector3(offsets[k], heights[k], 0), b = tops[i] + new Vector3(offsets[k], heights[k], 0);
                const int segs = 8;
                Vector3 prev = a;
                for (int s = 1; s <= segs; s++)
                {
                    float u = s / (float)segs;
                    Vector3 p = Vector3.Lerp(a, b, u) + Vector3.down * sags[k] * 4f * u * (1f - u);
                    WorldKit.Span("Wire", parent, prev, p, 0.03f, 0.03f, wire);
                    prev = p;
                }
                // Tattered strips tied along the wire.
                int strips = rng.Next(1, 4);
                for (int n = 0; n < strips; n++)
                {
                    float u = WorldKit.Range(rng, 0.15f, 0.85f);
                    Vector3 p = Vector3.Lerp(a, b, u) + Vector3.down * sags[k] * 4f * u * (1f - u);
                    Strip(parent, p, rng.NextDouble() < 0.35 ? tape : cloth, rng);
                }
            }
    }

    static void Strip(Transform parent, Vector3 top, Material mat, System.Random rng)
    {
        var pivot = new GameObject("Tattered Strip").transform;
        pivot.SetParent(parent, false);
        pivot.position = top;
        pivot.rotation = Quaternion.Euler(0f, WorldKit.Range(rng, 0f, 360f), 0f);
        float len = WorldKit.Range(rng, 0.35f, 0.9f), wid = WorldKit.Range(rng, 0.07f, 0.16f);
        var q = WorldKit.Part(PrimitiveType.Quad, "Cloth", pivot, new Vector3(0f, -len * 0.5f, 0f), new Vector3(wid, len, 1f), mat, default, false);
        // A second, shorter tail, like a torn end.
        if (rng.NextDouble() < 0.5)
            WorldKit.Part(PrimitiveType.Quad, "Cloth Tail", pivot, new Vector3(wid * 0.4f, -len * 0.3f, 0.01f), new Vector3(wid * 0.6f, len * 0.6f, 1f), mat, new Vector3(0, 0, 8f), false);
        var f = pivot.gameObject.AddComponent<Flutter>();
        f.speed = WorldKit.Range(rng, 0.8f, 1.4f);
    }

    public static Material DoubleSided(string name, Color c)
    {
        var m = Mat(name, c);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f); // render both faces of the thin strips
        m.doubleSidedGI = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------- Kate's porch (wooden, low, peeking out of the ash) ----------

    /// <summary>A low wooden porch in front of Kate's door (front wall at x = 11.5, door at z = 41).
    /// It sits low so Kate's walk is unchanged; the gap in the railing is where she passes.</summary>
    public static void Porch(Transform parent, Material wood, Material concrete)
    {
        var p = new GameObject("Porch").transform;
        p.SetParent(parent, false);
        WorldKit.Cube("Deck", p, new Vector3(10.55f, 0.06f, 41f), new Vector3(1.9f, 0.12f, 7f), wood);
        for (int i = 0; i < 9; i++) // plank lines
            WorldKit.Cube("Plank Gap", p, new Vector3(9.7f + i * 0.21f, 0.121f, 41f), new Vector3(0.012f, 0.002f, 7f), WorldKit.Wire);
        WorldKit.Cube("Step", p, new Vector3(9.4f, 0.03f, 41f), new Vector3(0.45f, 0.06f, 1.6f), wood);
        WorldKit.Cube("Walkway (edge)", p, new Vector3(7.6f, 0.03f, 41f), new Vector3(3.0f, 0.1f, 1.3f), concrete);
        foreach (float z in new[] { 37.7f, 44.3f })
            WorldKit.Cube("Post", p, new Vector3(9.75f, 1.38f, z), new Vector3(0.14f, 2.75f, 0.14f), wood);
        WorldKit.Cube("Porch Roof", p, new Vector3(10.5f, 2.82f, 41f), new Vector3(2.4f, 0.12f, 7.6f), wood, new Vector3(0, 0, -7f));
        // Railing with a gap where Kate walks through (z 40.2–41.8).
        foreach (var span in new[] { new Vector2(37.7f, 40.2f), new Vector2(41.8f, 44.3f) })
        {
            float mid = (span.x + span.y) * 0.5f, len = span.y - span.x;
            WorldKit.Cube("Rail", p, new Vector3(9.75f, 0.95f, mid), new Vector3(0.08f, 0.08f, len), wood);
            for (float z = span.x + 0.3f; z < span.y - 0.1f; z += 0.32f)
                WorldKit.Cube("Baluster", p, new Vector3(9.75f, 0.53f, z), new Vector3(0.05f, 0.82f, 0.05f), wood);
        }
    }
}
