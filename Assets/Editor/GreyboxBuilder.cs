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
/// Builds the comfort-test scene: a foggy street of block buildings, a few distant walkers,
/// a house at the end, and Kate's rig gliding along a path to its door.
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

        var matGround = MakeMaterial("Greybox_Ground", new Color(0.30f, 0.29f, 0.27f));
        var matStreet = MakeMaterial("Greybox_Street", new Color(0.22f, 0.22f, 0.22f));
        var matBuilding = MakeMaterial("Greybox_Building", new Color(0.42f, 0.41f, 0.39f));
        var matHouse = MakeMaterial("Greybox_House", new Color(0.36f, 0.33f, 0.30f));
        var matDoor = MakeMaterial("Greybox_Door", new Color(0.20f, 0.17f, 0.15f));
        var matFigure = MakeMaterial("Greybox_Figure", new Color(0.62f, 0.60f, 0.52f));

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
        Box("Ground", world, new Vector3(0, -0.05f, 40), new Vector3(140, 0.1f, 160), matGround);
        Box("Street", world, new Vector3(0, 0.0f, 40), new Vector3(8, 0.02f, 160), matStreet);
        Box("Sidewalk L", world, new Vector3(-5, 0.08f, 40), new Vector3(2, 0.16f, 160), matGround);
        Box("Sidewalk R", world, new Vector3(5, 0.08f, 40), new Vector3(2, 0.16f, 160), matGround);

        // ---------- Buildings (fog turns them into silhouettes) ----------
        var buildings = new GameObject("Buildings").transform;
        var rng = new System.Random(7); // fixed seed = same street every time
        for (int side = -1; side <= 1; side += 2)
        {
            float z = -20f;
            while (z < 110f)
            {
                float width = Range(rng, 6f, 12f);
                float depth = Range(rng, 8f, 14f);
                float height = Range(rng, 5f, 22f);
                bool gap = rng.NextDouble() < 0.18;               // occasional empty lot
                bool houseLot = side > 0 && z + width > 32f && z < 50f; // keep space for Kate's house
                if (!gap && !houseLot)
                {
                    float x = side * (7f + depth * 0.5f + Range(rng, 0f, 2f));
                    Box("Building", buildings, new Vector3(x, height * 0.5f, z + width * 0.5f), new Vector3(depth, height, width), matBuilding);
                }
                z += width + Range(rng, 0.5f, 3f);
            }
        }

        // ---------- Kate's house ----------
        var house = new GameObject("Kate's House").transform;
        Box("House", house, new Vector3(16f, 3f, 41f), new Vector3(9f, 6f, 9f), matHouse);
        Box("Roof", house, new Vector3(16f, 6.6f, 41f), new Vector3(9.6f, 1.2f, 9.6f), matHouse);
        Box("Walkway", house, new Vector3(8.5f, 0.05f, 41f), new Vector3(7f, 0.1f, 1.4f), matStreet);
        Box("Step", house, new Vector3(11.2f, 0.12f, 41f), new Vector3(0.6f, 0.24f, 1.6f), matHouse);
        Box("Door", house, new Vector3(11.47f, 1.05f, 41f), new Vector3(0.08f, 2.1f, 1.0f), matDoor);
        Box("Door Knob", house, new Vector3(11.40f, 1.0f, 41.35f), new Vector3(0.06f, 0.06f, 0.06f), matFigure);

        // ---------- Other walkers ----------
        var walkers = new GameObject("Other Walkers").transform;
        Walker(walkers, matFigure, new Vector3(-5f, 0f, 14f), 0f, 0.7f);    // ahead, walking away into the haze
        Walker(walkers, matFigure, new Vector3(5.5f, 0f, 70f), 180f, 0.5f); // emerges from the fog toward us
        Walker(walkers, matFigure, new Vector3(-20f, 0f, 58f), 90f, 0.45f); // crossing far down the street
        Walker(walkers, matFigure, new Vector3(-4.5f, 0f, 34f), 0f, 0.5f);

        // ---------- Kate's path ----------
        var path = new GameObject("Kate Path").transform;
        var points = new List<Transform>
        {
            Waypoint(path, "P0 Start",      new Vector3(1.5f, 0, 0f)),
            Waypoint(path, "P1 Street",     new Vector3(1.5f, 0, 25f)),
            Waypoint(path, "P2 Begin turn", new Vector3(2.2f, 0, 34f)),
            Waypoint(path, "P3 Corner",     new Vector3(4.8f, 0, 39.2f)),
            Waypoint(path, "P4 Walkway",    new Vector3(8.0f, 0, 41f)),
            Waypoint(path, "P5 At door",    new Vector3(10.4f, 0, 41f)),
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
        camGO.AddComponent<AudioListener>();

        var tracker = camGO.AddComponent<TrackedPoseDriver>();
        tracker.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tracker.positionInput = new InputActionProperty(new InputAction("Head Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
        tracker.rotationInput = new InputActionProperty(new InputAction("Head Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));

        // ---------- Save & add to build ----------
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuild(ScenePath);
        Selection.activeGameObject = rig;
        Debug.Log("KateVR: Greybox street built and saved to " + ScenePath);
    }

    // ---------- helpers ----------

    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.isStatic = true;
        return go;
    }

    static void Walker(Transform parent, Material mat, Vector3 feet, float heading, float speed)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Walker";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, true);
        go.transform.position = feet + Vector3.up * 0.9f;
        go.transform.rotation = Quaternion.Euler(0, heading, 0);
        go.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.AddComponent<Wanderer>().speed = speed;
    }

    static Transform Waypoint(Transform parent, string name, Vector3 pos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.position = pos;
        return t;
    }

    static Material MakeMaterial(string name, Color color)
    {
        System.IO.Directory.CreateDirectory(MaterialFolder);
        string path = $"{MaterialFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
        EditorUtility.SetDirty(mat);
        return mat;
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
