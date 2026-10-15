using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using KateVR;

/// <summary>
/// Turns the images in Assets/Memories into point-cloud memories and places them in the scene.
/// To swap an image: replace the PNG (same name) and run KateVR → Build Greybox Street again.
/// </summary>
public static class MemoryBuilder
{
    const string Folder = "Assets/Memories";

    public struct Spec
    {
        public string image;       // file name in Assets/Memories
        public float startTime;    // seconds into the scene
        public float duration;     // seconds from forming to fully dispersed
        public Vector3 center;     // where it hangs in the world
        public float width;        // metres across
        public Vector3 lookFrom;   // roughly where Kate will be, so it faces her
        public int columns;        // point density across the image
        public float colourTrace;  // 0 = pure grey, 1 = original colour
    }

    public static readonly Spec[] Memories =
    {
        // 9 s: the candlelight march, ahead and up to the left, like a low cloud.
        new Spec { image = "Memory1_Candles.png",    startTime = 9f,  duration = 12f, center = new Vector3(-3.5f, 6.5f, 33f), width = 10f, lookFrom = new Vector3(1.5f, 1.65f, 18f), columns = 120, colourTrace = 0.15f },
        // 18 s: the fist, off to the right at the edge of view, closer and smaller.
        new Spec { image = "Memory2_Fist.png",       startTime = 18f, duration = 12f, center = new Vector3(7.5f, 3.4f, 31f),  width = 4.5f, lookFrom = new Vector3(1.7f, 1.65f, 28f), columns = 80,  colourTrace = 0.12f },
        // 28 s: Sproul Hall, large and overhead as Kate turns toward the door.
        new Spec { image = "Memory3_SproulHall.png", startTime = 28f, duration = 12f, center = new Vector3(14f, 9.5f, 47f),  width = 14f, lookFrom = new Vector3(4.3f, 1.65f, 36.8f), columns = 130, colourTrace = 0.1f },
    };

    public static void BuildAll(Transform parent)
    {
        var shader = Shader.Find("KateVR/MemoryPoints");
        if (shader == null) { Debug.LogError("KateVR: MemoryPoints shader not found."); return; }
        string matPath = $"{Folder}/MemoryPoints.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matPath); }
        mat.shader = shader;
        mat.renderQueue = (int)RenderQueue.Transparent + 50;
        mat.SetFloat("_Scatter", 3f); // a tighter cloud before forming, so it doesn't spill around Kate
        EditorUtility.SetDirty(mat);

        var root = new GameObject("Memories").transform;
        root.SetParent(parent, false);
        for (int i = 0; i < Memories.Length; i++)
        {
            var s = Memories[i];
            var mesh = BuildMesh(s, i, out float pointSize);
            if (mesh == null) continue;

            var go = new GameObject("Memory " + (i + 1) + " – " + System.IO.Path.GetFileNameWithoutExtension(s.image));
            go.transform.SetParent(root, false);
            go.transform.position = s.center;
            Vector3 toward = s.center - s.lookFrom;
            go.transform.rotation = Quaternion.LookRotation(toward.normalized, Vector3.up);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            var cloud = go.AddComponent<MemoryCloud>();
            cloud.startTime = s.startTime;
            cloud.duration = s.duration;
            cloud.pointSize = pointSize;
        }
    }

    static Mesh BuildMesh(Spec s, int index, out float pointSize)
    {
        pointSize = 0.1f;
        string imgPath = $"{Folder}/{s.image}";
        if (!System.IO.File.Exists(imgPath)) { Debug.LogWarning("KateVR: missing memory image " + imgPath); return null; }
        var tex = new Texture2D(2, 2);
        tex.LoadImage(System.IO.File.ReadAllBytes(imgPath));

        int cols = s.columns;
        int rows = Mathf.Max(1, Mathf.RoundToInt(cols * tex.height / (float)tex.width));
        float cell = s.width / cols;
        float height = cell * rows;
        pointSize = cell * 1.8f;
        var rng = new System.Random(100 + index);

        var verts = new List<Vector3>();
        var colors = new List<Color>();
        var uv0 = new List<Vector2>();
        var uv1 = new List<Vector4>();
        var tris = new List<int>();
        Vector2[] corners = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };

        for (int y = 0; y < rows; y++)
        for (int x = 0; x < cols; x++)
        {
            float u = (x + 0.5f + Rand(rng, -0.45f, 0.45f)) / cols;
            float v = (y + 0.5f + Rand(rng, -0.45f, 0.45f)) / rows;
            Color c = tex.GetPixelBilinear(u, v);
            float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            // Near-white paper drops away, so posters read as marks in the air, not as cards.
            float ink = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 0.95f, lum));
            if (ink < 0.05f || rng.NextDouble() > 0.35 + 0.65 * ink) continue;

            Color grey = new Color(lum, lum, lum);
            Color col = Color.Lerp(grey, c, s.colourTrace);
            col.a = ink;

            float edgeX = Mathf.Min(u, 1f - u), edgeY = Mathf.Min(v, 1f - v);
            float edge = 1f - Mathf.Clamp01(Mathf.Min(edgeX, edgeY) / 0.18f); // soft, cloud-like border
            Vector3 pos = new Vector3((u - 0.5f) * s.width, (v - 0.5f) * height, Rand(rng, -0.25f, 0.25f) * (1f + edge * 3f));
            var data = new Vector4((float)rng.NextDouble() * 100f, u, edge, Rand(rng, 0.6f, 1.4f));

            int b = verts.Count;
            for (int k = 0; k < 4; k++) { verts.Add(pos); colors.Add(col); uv0.Add(corners[k]); uv1.Add(data); }
            tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
        }
        Object.DestroyImmediate(tex);

        var mesh = new Mesh { name = "Memory " + (index + 1), indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uv0);
        mesh.SetUVs(1, uv1);
        mesh.SetTriangles(tris, 0);
        // Generous bounds: points start scattered and end far downwind, so never cull them early.
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(s.width + 30f, height + 30f, 30f));

        string meshPath = $"{Folder}/Memory{index + 1}_Points.asset";
        AssetDatabase.DeleteAsset(meshPath);
        AssetDatabase.CreateAsset(mesh, meshPath);
        return mesh;
    }

    static float Rand(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
}
