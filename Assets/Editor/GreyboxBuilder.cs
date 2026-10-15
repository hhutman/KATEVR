using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using KateVR;

/// <summary>
/// Menu: KateVR → Build Greybox Street.
/// Builds Kate's street: a sparse, colourless, stylized Berkeley (WorldKit), ground fog,
/// two or three hurrying figures, Kate's Craftsman bungalow, and her rig gliding to the door.
/// Safe to run again: it rebuilds the scene from scratch.
/// </summary>
public static class GreyboxBuilder
{
    const string ScenePath = "Assets/Scenes/Street_Greybox.unity";
    const string MaterialFolder = "Assets/Materials";

    // Look of the air. Tweak these, then re-run the menu item.
    static readonly Color FogColor = new Color(0.50f, 0.48f, 0.44f);
    const float FogStart = 3f;
    const float FogEnd = 45f;
    const float EyeHeight = 1.65f;

    [MenuItem("KateVR/Build Greybox Street")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        WorldKit.LoadMaterials();

        // ---------- Atmosphere ----------
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogStartDistance = FogStart;
        RenderSettings.fogEndDistance = FogEnd;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.53f, 0.50f);

        var sun = new GameObject("Hazy Sun");
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.85f, 0.80f, 0.70f);
        light.intensity = 0.5f;
        light.shadows = LightShadows.None; // cheap on Quest
        sun.transform.rotation = Quaternion.Euler(40f, 30f, 0f);

        // ---------- Ground & street ----------
        var world = new GameObject("World").transform;
        var rng = new System.Random(7); // fixed seed = same street every time
        WorldKit.Cube("Ground", world, new Vector3(0, -0.05f, 40), new Vector3(140, 0.1f, 170), WorldKit.Dirt);
        WorldKit.Cube("Street", world, new Vector3(0, 0.0f, 40), new Vector3(8, 0.02f, 170), WorldKit.Asphalt);
        WorldKit.Cube("Sidewalk L", world, new Vector3(-5, 0.08f, 40), new Vector3(2, 0.16f, 170), WorldKit.Concrete);
        WorldKit.Cube("Sidewalk R", world, new Vector3(5, 0.08f, 40), new Vector3(2, 0.16f, 170), WorldKit.Concrete);
        for (float z = -40f; z < 120f; z += 4.5f)          // faded, broken centre line
            if (rng.NextDouble() < 0.65)
                WorldKit.Cube("Centre Line", world, new Vector3(0, 0.012f, z), new Vector3(0.12f, 0.004f, WorldKit.Range(rng, 0.8f, 2.2f)), WorldKit.Marking);
        WorldKit.Cracks(world, rng, -3.8f, 3.8f, -15f, 80f, 0.012f, 55);
        WorldKit.Cracks(world, rng, -5.9f, -4.1f, -15f, 80f, 0.162f, 18);
        WorldKit.Cracks(world, rng, 4.1f, 5.9f, -15f, 80f, 0.162f, 18);
        float[,] puddles = { { 2.4f, 6f }, { -1.5f, 12f }, { 0.8f, 19f }, { 2.8f, 27f }, { -2.6f, 31f }, { 3.2f, 36.5f }, { 0.2f, 44f }, { -1f, 52f }, { 2f, 60f } };
        for (int i = 0; i < puddles.GetLength(0); i++)
            WorldKit.PuddleAt(world, new Vector3(puddles[i, 0], 0.012f, puddles[i, 1]), WorldKit.Range(rng, 1.2f, 2.8f), WorldKit.Range(rng, 0.8f, 1.8f), WorldKit.Range(rng, 0, 180));
        for (int i = 0; i < 45; i++)                       // weeds along curbs and building edges
        {
            float side = rng.NextDouble() < 0.5 ? -1f : 1f;
            float x = side * (rng.NextDouble() < 0.6 ? 4.05f : WorldKit.Range(rng, 5.6f, 6.2f));
            WorldKit.WeedTuft(world, rng, new Vector3(x, x * side > 4.5f ? 0.16f : 0f, WorldKit.Range(rng, -12f, 75f)));
        }

        // ---------- Buildings: sparse, a few left standing ----------
        // Right side faces the street with rotY 90; left side with rotY -90.
        var town = new GameObject("Buildings").transform;
        WorldKit.Shop ("Shop",       town, new Vector3( 6.2f, 0, -9f),  90f, 10f, 14f, 2, 11, false, false);
        WorldKit.Shop ("Shop",       town, new Vector3( 6.2f, 0,  7f),  90f,  9f, 12f, 1, 12, true,  false);
        WorldKit.Lot  ("Empty Lot",  town, new Vector3( 6.2f, 0, 18f),  90f,  9f, 12f, 13, true);
        WorldKit.House("Bungalow",   town, new Vector3( 9.5f, 0, 27f),  90f,  9f, 11f, 1, 14, true);
        WorldKit.House("Shingle House", town, new Vector3( 9.5f, 0, 56f), 90f, 10f, 12f, 2, 15, true);
        WorldKit.Shop ("Shop",       town, new Vector3( 6.2f, 0, 72f),  90f, 12f, 14f, 2, 16, true,  true);
        WorldKit.Lot  ("Empty Lot",  town, new Vector3( 6.2f, 0, 86f),  90f, 10f, 12f, 17, false);
        WorldKit.House("Bungalow",   town, new Vector3(-9.5f, 0, -6f), -90f,  9f, 11f, 1, 21, true);
        WorldKit.Shop ("Shop",       town, new Vector3(-6.2f, 0, 10f), -90f, 13f, 14f, 1, 22, true,  false);
        WorldKit.Lot  ("Empty Lot",  town, new Vector3(-6.2f, 0, 24f), -90f, 10f, 12f, 23, false);
        WorldKit.House("Shingle House", town, new Vector3(-9.5f, 0, 38f), -90f, 10f, 12f, 2, 24, true);
        WorldKit.Shop ("Shop",       town, new Vector3(-6.2f, 0, 55f), -90f, 10f, 12f, 2, 25, true,  true);
        WorldKit.Lot  ("Empty Lot",  town, new Vector3(-6.2f, 0, 68f), -90f, 10f, 12f, 26, true);
        WorldKit.Shop ("Shop",       town, new Vector3(-6.2f, 0, 82f), -90f, 12f, 14f, 1, 27, false, false);

        // ---------- Kate's house: a Craftsman bungalow, door facing the street ----------
        var kate = WorldKit.House("Kate's House", town, new Vector3(13f, 0, 41f), 90f, 10f, 12f, 1, 31, false);
        WorldKit.Cube("Front Yard", kate, new Vector3(0, 0.015f, -3.6f), new Vector3(10f, 0.03f, 7f), WorldKit.Dirt);
        WorldKit.Cube("Walkway", town, new Vector3(7.85f, 0.04f, 41f), new Vector3(3.7f, 0.08f, 1.3f), WorldKit.Concrete);
        for (int i = 0; i < 6; i++)                          // what's left of a picket fence
            if (i != 2 && i != 3)
                WorldKit.Cube("Fence Post", town, new Vector3(6.4f, 0.45f, 37f + i * 1.6f), new Vector3(0.08f, 0.9f, 0.08f), WorldKit.Board,
                              new Vector3(WorldKit.Range(rng, -12, 12), 0, WorldKit.Range(rng, -12, 12)));

        // ---------- Utility poles & wires ----------
        var props = new GameObject("Street Furniture").transform;
        float[] rightPoles = { -14f, 8f, 30f, 52f, 74f };
        float[] leftPoles = { -4f, 18f, 40f, 62f, 84f };
        for (int i = 0; i < rightPoles.Length; i++)
        {
            var a = WorldKit.Pole(props, new Vector3(5.8f, 0, rightPoles[i]), i == 2);
            if (i > 0) WorldKit.Wires(props, new Vector3(5.8f, 0, rightPoles[i - 1]), a, i == 2);
        }
        for (int i = 0; i < leftPoles.Length; i++)
        {
            var a = WorldKit.Pole(props, new Vector3(-5.8f, 0, leftPoles[i]), i == 3);
            if (i > 0) WorldKit.Wires(props, new Vector3(-5.8f, 0, leftPoles[i - 1]), a, false);
        }
        WorldKit.SignPost(props, new Vector3(5.35f, 0.16f, 33.5f), 9f);
        WorldKit.SignPost(props, new Vector3(-5.35f, 0.16f, 15f), -4f);

        // ---------- Ground fog: slow puffs hugging the street ----------
        WorldKit.GroundFog(world, new Vector3(3f, 0.7f, 32f), new Vector3(30f, 1.2f, 90f));

        // ---------- Other people: two or three, all in a hurry, at different speeds ----------
        var walkers = new GameObject("Other Walkers").transform;
        WorldKit.Figure(walkers, 0, new Vector3(-4.8f, 0.16f, 45f), 180f, 1.9f); // toward us on the left, passes ~16 s in
        WorldKit.Figure(walkers, 1, new Vector3(4.8f, 0.16f, -14f), 0f, 1.8f);   // overtakes Kate on the right ~14 s in
        WorldKit.Figure(walkers, 2, new Vector3(-24f, 0f, 49f), 90f, 1.4f);      // crosses the street far ahead in the haze

        // ---------- Kate's path: down the street, up the walkway, up the porch steps ----------
        var path = new GameObject("Kate Path").transform;
        var points = new List<Transform>
        {
            Waypoint(path, "P0 Start",        new Vector3(1.5f, 0, 0f)),
            Waypoint(path, "P1 Street",       new Vector3(1.5f, 0, 25f)),
            Waypoint(path, "P2 Begin turn",   new Vector3(2.2f, 0, 34f)),
            Waypoint(path, "P3 Corner",       new Vector3(4.8f, 0, 39.2f)),
            Waypoint(path, "P4 Walkway",      new Vector3(8.0f, 0.08f, 41f)),
            Waypoint(path, "P5 Foot of steps",new Vector3(9.3f, 0.08f, 41f)),
            Waypoint(path, "P6 Porch",        new Vector3(10.8f, 0.6f, 41f)),
            Waypoint(path, "P7 At door",      new Vector3(12.0f, 0.6f, 41f)),
        };

        // ---------- Kate's rig (the viewer's head) ----------
        var rig = new GameObject("Kate Rig");
        rig.transform.position = points[0].position;
        var glide = rig.AddComponent<GlidePath>();
        glide.waypoints = points;
        rig.AddComponent<SeatedOrigin>();

        var eyes = new GameObject("Eye Height").transform;
        eyes.SetParent(rig.transform, false);
        eyes.localPosition = new Vector3(0, EyeHeight, 0);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(eyes, false);
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = FogColor;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = FogEnd + 10f; // nothing past the fog needs drawing
        cam.fieldOfView = 90f;           // only affects the flat Game-view preview; the headset sets its own
        camGO.AddComponent<AudioListener>();

        // Hazmat visor: head-locked, so the rim stays put at the edge of vision wherever you look.
        BuildVisor(camGO.transform);

        var tracker = camGO.AddComponent<TrackedPoseDriver>();
        tracker.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tracker.positionInput = new InputActionProperty(new InputAction("Head Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
        tracker.rotationInput = new InputActionProperty(new InputAction("Head Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));

        // ---------- Save & add to build ----------
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuild(ScenePath);
        Selection.activeGameObject = rig;
        Debug.Log("KateVR: Street built and saved to " + ScenePath);
    }

    // ---------- Visor ----------

    // Visor shape, in degrees from the centre of view. Smaller = more enclosed.
    const float VisorHalfWidth = 46f;
    const float VisorHalfHeightTop = 34f;
    const float VisorHalfHeightBottom = 42f;
    const float VisorDistance = 0.35f; // metres in front of the eyes

    static void BuildVisor(Transform camera)
    {
        const float quadW = 1.6f, quadH = 1.4f;
        var tex = MakeVisorTexture(quadW, quadH);

        var mat = WorldKit.MakeMaterial("Visor", Color.white, 0f, "Universal Render Pipeline/Unlit");
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        // Transparent, drawn on top of the world.
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent + 100;
        EditorUtility.SetDirty(mat);

        var visor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visor.name = "Hazmat Visor";
        Object.DestroyImmediate(visor.GetComponent<Collider>());
        visor.transform.SetParent(camera, false);
        visor.transform.localPosition = new Vector3(0, 0, VisorDistance);
        visor.transform.localScale = new Vector3(quadW, quadH, 1f);
        var r = visor.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    static Texture2D MakeVisorTexture(float quadW, float quadH)
    {
        const int w = 1024, h = 896;
        var px = new Color[w * h];
        var rim = new Color(0.06f, 0.06f, 0.055f);       // rubber seal / hood
        var tint = new Color(0.82f, 0.90f, 0.84f);       // faint greenish plastic
        const float tintAlpha = 0.06f;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            // pixel -> metres on the quad -> angle from the eye
            float mx = ((x + 0.5f) / w - 0.5f) * quadW;
            float my = ((y + 0.5f) / h - 0.5f) * quadH;
            float ax = Mathf.Atan2(mx, VisorDistance) * Mathf.Rad2Deg;
            float ay = Mathf.Atan2(my, VisorDistance) * Mathf.Rad2Deg;
            float halfH = ay >= 0 ? VisorHalfHeightTop : VisorHalfHeightBottom;
            float e = Mathf.Sqrt((ax / VisorHalfWidth) * (ax / VisorHalfWidth) + (ay / halfH) * (ay / halfH));

            float rimA = Mathf.SmoothStep(0f, 0.97f, Mathf.InverseLerp(0.80f, 1.02f, e));
            float a = 1f - (1f - tintAlpha) * (1f - rimA);
            Color c = Color.Lerp(tint, rim, rimA / Mathf.Max(a, 0.0001f));
            c.a = a;
            px[y * w + x] = c;
        }

        // A few faint scratches on the plastic.
        var rng = new System.Random(11);
        for (int s = 0; s < 22; s++)
        {
            float x0 = (float)rng.NextDouble() * w, y0 = (float)rng.NextDouble() * h;
            float ang = (float)rng.NextDouble() * Mathf.PI;
            float len = 30f + (float)rng.NextDouble() * 120f;
            for (int i = 0; i < len; i++)
            {
                int xi = (int)(x0 + Mathf.Cos(ang) * i), yi = (int)(y0 + Mathf.Sin(ang) * i);
                if (xi < 0 || yi < 0 || xi >= w || yi >= h) continue;
                var p = px[yi * w + xi];
                px[yi * w + xi] = new Color(Mathf.Lerp(p.r, 0.9f, 0.5f), Mathf.Lerp(p.g, 0.9f, 0.5f), Mathf.Lerp(p.b, 0.9f, 0.5f), Mathf.Max(p.a, 0.12f));
            }
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels(px);
        tex.Apply();
        System.IO.Directory.CreateDirectory(MaterialFolder);
        string path = $"{MaterialFolder}/Visor.png";
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = true;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ---------- helpers ----------

    static Transform Waypoint(Transform parent, string name, Vector3 pos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.position = pos;
        return t;
    }

    static void AddToBuild(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == path);
        foreach (var s in scenes) s.enabled = false; // only the street goes in the build for now
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
}
